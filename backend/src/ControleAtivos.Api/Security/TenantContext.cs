using ControleAtivos.Application.Abstractions;

namespace ControleAtivos.Api.Security;

/// <summary>
/// Tenant da requisicao. O valor e definido uma unica vez pelo
/// <c>TenantResolutionMiddleware</c>, a partir de claim do token — nunca do
/// corpo ou da query string, que sao controlados pelo cliente.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private Guid _tenantId;
    private bool _bypassed;

    public Guid TenantId => _tenantId;

    public string? TenantSlug { get; private set; }

    public bool HasTenant => _tenantId != Guid.Empty;

    public bool IsTenantFilterBypassed => _bypassed;

    internal void Initialize(Guid tenantId, string? slug)
    {
        if (HasTenant && _tenantId != tenantId)
        {
            throw new InvalidOperationException(
                "O tenant da requisicao ja foi definido e nao pode ser alterado.");
        }

        _tenantId = tenantId;
        TenantSlug = slug;
    }

    public IDisposable BypassTenantFilter()
    {
        _bypassed = true;
        return new BypassScope(this);
    }

    private sealed class BypassScope(TenantContext context) : IDisposable
    {
        public void Dispose() => context._bypassed = false;
    }
}
