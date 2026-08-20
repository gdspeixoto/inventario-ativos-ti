using System.Security.Claims;
using ControleAtivos.Application.Abstractions;

namespace ControleAtivos.Api.Security;

/// <summary>
/// Le a identidade a partir das claims do principal ja validado pelo pipeline
/// de autenticacao. As permissoes sao derivadas dos papeis pelo mapa em
/// <see cref="Roles"/>, e nao lidas do token: assim uma configuracao incompleta
/// no realm nao concede acesso indevido nem bloqueia quem deveria ter.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    public const string UserIdClaim = "controle_ativos_user_id";

    private readonly IHttpContextAccessor _accessor;
    private readonly Lazy<HashSet<string>> _roles;
    private readonly Lazy<HashSet<string>> _permissions;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;

        _roles = new Lazy<HashSet<string>>(ResolveRoles);
        _permissions = new Lazy<HashSet<string>>(() =>
            _roles.Value.SelectMany(Application.Abstractions.Roles.PermissionsFor).ToHashSet());
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? ExternalSubject => Principal?.FindFirst("sub")?.Value;

    public string? DisplayName =>
        Principal?.FindFirst("name")?.Value
        ?? Principal?.FindFirst("preferred_username")?.Value;

    public string? Email => Principal?.FindFirst("email")?.Value?.ToLowerInvariant();

    /// <summary>
    /// Id local, injetado como claim pelo middleware de provisionamento apos
    /// espelhar o usuario do Keycloak na tabela local.
    /// </summary>
    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst(UserIdClaim)?.Value, out var id) ? id : null;

    public IReadOnlySet<string> Roles => _roles.Value;

    public IReadOnlySet<string> Permissions => _permissions.Value;

    public bool HasPermission(string permission) => _permissions.Value.Contains(permission);

    public bool HasRole(string role) => _roles.Value.Contains(role);

    /// <summary>
    /// Coleta papeis do formato do Keycloak (<c>realm_access.roles</c> e
    /// <c>resource_access.&lt;client&gt;.roles</c>, ja achatados em claims
    /// <c>role</c> pelo handler) e do claim padrao do ASP.NET Core.
    /// </summary>
    private HashSet<string> ResolveRoles()
    {
        if (Principal is null)
        {
            return [];
        }

        return Principal
            .FindAll(ClaimTypes.Role)
            .Concat(Principal.FindAll("role"))
            .Concat(Principal.FindAll("roles"))
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
