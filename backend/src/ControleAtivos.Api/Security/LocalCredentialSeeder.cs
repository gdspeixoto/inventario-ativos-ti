using ControleAtivos.Api.Configuration;
using ControleAtivos.Application.Abstractions;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Api.Security;

/// <summary>
/// Provisiona as credenciais locais declaradas em configuracao.
///
/// A conta de emergencia precisa existir <em>antes</em> da emergencia: criar
/// pela interface depende de conseguir entrar, que e exatamente o que falta
/// quando o SSO cai.
///
/// O seeder e idempotente e nunca sobrescreve a senha de uma credencial ja
/// existente — do contrario, uma senha trocada pelo administrador voltaria ao
/// valor da configuracao no proximo deploy.
/// </summary>
public static class LocalCredentialSeeder
{
    public static async Task SeedAsync(
        ControleAtivosDbContext dbContext,
        IPasswordHasher passwordHasher,
        AccessOptions access,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (access.LocalLogin.SeedCredentials.Length == 0)
        {
            return;
        }

        var tenant = await ResolveTenantAsync(dbContext, access.DefaultTenantSlug, cancellationToken);

        if (tenant is null)
        {
            logger.LogWarning(
                "Credenciais locais nao provisionadas: nenhum tenant ativo encontrado.");
            return;
        }

        foreach (var seed in access.LocalLogin.SeedCredentials)
        {
            await SeedOneAsync(dbContext, passwordHasher, tenant.Value, seed, logger, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedOneAsync(
        ControleAtivosDbContext dbContext,
        IPasswordHasher passwordHasher,
        Guid tenantId,
        SeedCredential seed,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(seed.Username) || string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogWarning("Credencial local ignorada: usuario ou senha em branco.");
            return;
        }

        var username = seed.Username.Trim().ToLowerInvariant();

        var exists = await dbContext.LocalCredentials
            .AnyAsync(c => c.Username == username, cancellationToken);

        if (exists)
        {
            return;
        }

        var subject = $"local:{username}";

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalSubject == subject, cancellationToken);

        if (user is null)
        {
            user = new AppUser(
                Guid.NewGuid(),
                tenantId,
                subject,
                string.IsNullOrWhiteSpace(seed.DisplayName) ? username : seed.DisplayName,
                string.IsNullOrWhiteSpace(seed.Email) ? $"{username}@local.invalid" : seed.Email);

            dbContext.Users.Add(user);
        }

        dbContext.LocalCredentials.Add(new LocalCredential(
            Guid.NewGuid(),
            tenantId,
            user,
            username,
            passwordHasher.Hash(seed.Password),
            seed.ExpiresInDays > 0
                ? DateTimeOffset.UtcNow.AddDays(seed.ExpiresInDays)
                : null));

        var alreadyAdmin = await dbContext.UserRoleAssignments
            .AnyAsync(a => a.UserId == user.Id && a.Role == Roles.TenantAdmin, cancellationToken);

        if (!alreadyAdmin)
        {
            dbContext.UserRoleAssignments.Add(new UserRoleAssignment(
                Guid.NewGuid(),
                tenantId,
                user,
                Roles.TenantAdmin,
                grantedByUserId: null,
                notes: "Credencial local de emergencia provisionada por configuracao."));
        }

        logger.LogWarning(
            "Credencial local '{Username}' provisionada com papel {Role}. " +
            "Troque a senha inicial e mantenha o acesso restrito.",
            username,
            Roles.TenantAdmin);
    }

    private static async Task<Guid?> ResolveTenantAsync(
        ControleAtivosDbContext dbContext,
        string? slug,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(slug))
        {
            var bySlug = await dbContext.Tenants
                .Where(t => t.Slug == slug && t.IsActive)
                .Select(t => (Guid?)t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (bySlug is not null)
            {
                return bySlug;
            }
        }

        /*
         * Sem slug configurado, so ha escolha obvia se existir exatamente um
         * tenant. Com varios, adivinhar colocaria a conta de emergencia na
         * organizacao errada.
         */
        var tenants = await dbContext.Tenants
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        return tenants.Count == 1 ? tenants[0] : null;
    }
}
