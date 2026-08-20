using ControleAtivos.Api.Configuration;
using ControleAtivos.Api.Security;
using ControleAtivos.Application.Abstractions;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControleAtivos.Api.Middleware;

/// <summary>
/// Resolve o tenant e provisiona o usuario local a partir do token validado.
///
/// A ordem de precedencia e deliberada: primeiro a claim do token, depois o
/// header interno — que so e aceito quando a requisicao vem do BFF, ja
/// autenticada. Em nenhum caso o tenant e lido do corpo da requisicao.
///
/// Quando nenhuma das fontes traz o tenant, entra o padrao configurado em
/// <see cref="AccessOptions.DefaultTenantSlug"/>. Esse fallback existe porque
/// usuarios federados do AD nao sao editaveis no Keycloak — nao ha onde
/// gravar a claim. Ele so e seguro em instalacao de organizacao unica, e por
/// isso e opcional e explicito, nunca automatico.
/// </summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger,
    IOptions<AccessOptions> accessOptions)
{
    public const string TenantHeader = "X-Tenant-Id";
    private const string TenantClaim = "tenant_id";
    private const string TenantSlugClaim = "tenant";

    private readonly AccessOptions _access = accessOptions.Value;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ControleAtivosDbContext dbContext)
    {
        // Rotas publicas (health, openapi) nao exigem tenant.
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await next(context);
            return;
        }

        var slug = context.User.FindFirst(TenantSlugClaim)?.Value
            ?? _access.DefaultTenantSlug;

        var tenantId = ResolveTenantId(context);

        if (tenantId is null && slug is not null)
        {
            using (tenantContext.BypassTenantFilter())
            {
                tenantId = await dbContext.Tenants
                    .AsNoTracking()
                    .Where(t => t.Slug == slug && t.IsActive)
                    .Select(t => (Guid?)t.Id)
                    .FirstOrDefaultAsync(context.RequestAborted);
            }
        }

        if (tenantId is null || tenantId == Guid.Empty)
        {
            logger.LogWarning(
                "Requisicao autenticada sem tenant resolvido. Subject: {Subject}",
                context.User.FindFirst("sub")?.Value);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Tenant nao identificado",
                detail = "O usuario autenticado nao esta associado a nenhuma organizacao ativa.",
                status = StatusCodes.Status403Forbidden,
            });
            return;
        }

        ((TenantContext)tenantContext).Initialize(tenantId.Value, slug);

        var userId = await ProvisionLocalUserAsync(context, dbContext, tenantId.Value);

        if (userId.HasValue)
        {
            await ApplyLocalRolesAsync(context, dbContext, tenantId.Value, userId.Value);
        }

        await next(context);
    }

    /// <summary>
    /// Soma aos papeis do token aqueles concedidos dentro do sistema.
    ///
    /// Os dois lados se somam, e nenhum revoga o outro: quem administra o
    /// Keycloak e quem administra o produto sao equipes diferentes, e o
    /// silencio de uma nao deve apagar a decisao da outra.
    ///
    /// A concessao de bootstrap (<see cref="AccessOptions.BootstrapAdmins"/>)
    /// e materializada como registro no banco, e nao apenas como claim na
    /// requisicao. Assim ela aparece nas telas de administracao e pode ser
    /// revogada normalmente depois que a lista sair da configuracao.
    /// </summary>
    private async Task ApplyLocalRolesAsync(
        HttpContext context,
        ControleAtivosDbContext dbContext,
        Guid tenantId,
        Guid userId)
    {
        await EnsureBootstrapAdminAsync(context, dbContext, tenantId, userId);

        var roles = await dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId && assignment.IsActive)
            .Select(assignment => assignment.Role)
            .ToListAsync(context.RequestAborted);

        if (roles.Count == 0)
        {
            return;
        }

        var existing = context.User
            .FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var identity = new System.Security.Claims.ClaimsIdentity(
            roles
                .Where(role => !existing.Contains(role))
                .Select(role => new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Role, role)));

        context.User.AddIdentity(identity);
    }

    private async Task EnsureBootstrapAdminAsync(
        HttpContext context,
        ControleAtivosDbContext dbContext,
        Guid tenantId,
        Guid userId)
    {
        if (_access.BootstrapAdmins.Length == 0)
        {
            return;
        }

        var email = context.User.FindFirst("email")?.Value;
        var subject = context.User.FindFirst("sub")?.Value;
        var username = context.User.FindFirst("preferred_username")?.Value;

        var matches = _access.BootstrapAdmins.Any(entry =>
            string.Equals(entry, email, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entry, subject, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entry, username, StringComparison.OrdinalIgnoreCase));

        if (!matches)
        {
            return;
        }

        var alreadyGranted = await dbContext.UserRoleAssignments.AnyAsync(
            assignment => assignment.UserId == userId && assignment.Role == Roles.TenantAdmin,
            context.RequestAborted);

        if (alreadyGranted)
        {
            return;
        }

        var user = await dbContext.Users.FirstAsync(u => u.Id == userId, context.RequestAborted);

        dbContext.UserRoleAssignments.Add(new UserRoleAssignment(
            Guid.NewGuid(),
            tenantId,
            user,
            Roles.TenantAdmin,
            grantedByUserId: null,
            notes: "Concedido automaticamente pela lista Access:BootstrapAdmins."));

        await dbContext.SaveChangesAsync(context.RequestAborted);

        logger.LogWarning(
            "Papel {Role} concedido por bootstrap ao usuario {UserId}. " +
            "Remova a entrada de Access:BootstrapAdmins apos configurar a equipe.",
            Roles.TenantAdmin,
            userId);
    }

    private static Guid? ResolveTenantId(HttpContext context)
    {
        if (Guid.TryParse(context.User.FindFirst(TenantClaim)?.Value, out var fromClaim))
        {
            return fromClaim;
        }

        /*
         * O header so e considerado porque, na topologia com BFF, ele e
         * definido pelo proprio backend depois de validar a sessao. Em um
         * deployment sem BFF, o proxy de borda deve remover este header das
         * requisicoes externas.
         */
        if (context.Request.Headers.TryGetValue(TenantHeader, out var headerValue) &&
            Guid.TryParse(headerValue.ToString(), out var fromHeader))
        {
            return fromHeader;
        }

        return null;
    }

    /// <summary>
    /// Espelha o usuario do Keycloak na tabela local e injeta o id resultante
    /// como claim, para que auditoria e timeline registrem um identificador
    /// estavel do sistema.
    /// </summary>
    private static async Task<Guid?> ProvisionLocalUserAsync(
        HttpContext context,
        ControleAtivosDbContext dbContext,
        Guid tenantId)
    {
        var subject = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalSubject == subject, context.RequestAborted);

        var displayName =
            context.User.FindFirst("name")?.Value
            ?? context.User.FindFirst("preferred_username")?.Value
            ?? subject;

        var email = context.User.FindFirst("email")?.Value ?? $"{subject}@sem-email.local";

        if (user is null)
        {
            user = new AppUser(Guid.NewGuid(), tenantId, subject, displayName, email);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(context.RequestAborted);
        }
        else if (!string.Equals(user.DisplayName, displayName, StringComparison.Ordinal))
        {
            user.SyncFromIdentityProvider(displayName, email, context.User.FindFirst("job_title")?.Value);
            await dbContext.SaveChangesAsync(context.RequestAborted);
        }

        context.User.AddIdentity(new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim(CurrentUser.UserIdClaim, user.Id.ToString()),
        ]));

        return user.Id;
    }
}
