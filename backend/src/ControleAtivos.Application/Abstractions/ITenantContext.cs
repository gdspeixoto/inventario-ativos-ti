namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Tenant da requisicao atual. Resolvido a partir de claim do token ou de header
/// definido pelo BFF apos validar a sessao. Nunca a partir do corpo da
/// requisicao: aceitar o tenant do cliente anularia o isolamento.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }

    string? TenantSlug { get; }

    bool HasTenant { get; }

    /// <summary>
    /// Suspende o filtro global de tenant para operacoes de infraestrutura
    /// (migrations, provisionamento inicial, jobs administrativos). Deve ser
    /// usado apenas fora do fluxo de requisicao HTTP.
    /// </summary>
    IDisposable BypassTenantFilter();

    bool IsTenantFilterBypassed { get; }
}
