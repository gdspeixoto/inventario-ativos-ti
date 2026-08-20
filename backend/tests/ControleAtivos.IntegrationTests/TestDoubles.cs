using ControleAtivos.Application.Abstractions;

namespace ControleAtivos.IntegrationTests;

/// <summary>Tenant fixo, no lugar do resolvido a partir do token.</summary>
public sealed class TestTenantContext(Guid tenantId, bool bypass = false) : ITenantContext
{
    private bool _bypassed = bypass;

    public Guid TenantId { get; } = tenantId;

    public string? TenantSlug => null;

    public bool HasTenant => TenantId != Guid.Empty;

    public bool IsTenantFilterBypassed => _bypassed;

    public IDisposable BypassTenantFilter()
    {
        _bypassed = true;
        return new Scope(this);
    }

    private sealed class Scope(TestTenantContext context) : IDisposable
    {
        public void Dispose() => context._bypassed = false;
    }
}

/// <summary>Usuario fixo, com permissoes declaradas no teste.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(
        Guid? userId = null,
        string? displayName = null,
        params string[] permissions)
    {
        UserId = userId;
        DisplayName = displayName;
        Permissions = permissions.ToHashSet();
        Roles = new HashSet<string>();
    }

    public Guid? UserId { get; }

    public string? ExternalSubject => UserId?.ToString();

    public string? DisplayName { get; }

    public string? Email => null;

    public bool IsAuthenticated => UserId is not null;

    public IReadOnlySet<string> Roles { get; }

    public IReadOnlySet<string> Permissions { get; }

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public bool HasRole(string role) => Roles.Contains(role);
}
