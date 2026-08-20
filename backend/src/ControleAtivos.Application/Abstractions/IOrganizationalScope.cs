namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Escopo organizacional efetivo do usuario: o conjunto de unidades cujos dados
/// ele pode enxergar.
///
/// Calculado a partir das atribuicoes gerenciais ativas (um gerente pode
/// responder por varias areas e nucleos) somadas as unidades herdadas por
/// descendencia. Quem tem escopo global — administrador do tenant, auditor —
/// recebe <see cref="HasGlobalScope"/> verdadeiro.
/// </summary>
public interface IOrganizationalScope
{
    bool HasGlobalScope { get; }

    /// <summary>Ids das unidades acessiveis, ja expandidos por descendencia.</summary>
    IReadOnlySet<Guid> UnitIds { get; }

    /// <summary>
    /// Prefixos de <c>Path</c> das sub-arvores acessiveis. Usados em consultas
    /// do tipo <c>Path LIKE prefixo%</c> para alcancar descendentes sem
    /// materializar a lista completa de ids.
    /// </summary>
    IReadOnlyCollection<string> PathPrefixes { get; }

    /// <summary>Verdadeiro quando o usuario nao alcanca nenhuma unidade.</summary>
    bool IsEmpty { get; }

    bool CanAccess(Guid organizationalUnitId);

    bool CanAccessPath(string organizationalUnitPath);
}

/// <summary>
/// Resolve o escopo da requisicao atual. A implementacao consulta as
/// atribuicoes gerenciais e mantem o resultado em cache pelo tempo de vida da
/// requisicao, evitando repetir a consulta a cada endpoint.
/// </summary>
public interface IOrganizationalScopeResolver
{
    Task<IOrganizationalScope> GetAsync(CancellationToken cancellationToken = default);
}
