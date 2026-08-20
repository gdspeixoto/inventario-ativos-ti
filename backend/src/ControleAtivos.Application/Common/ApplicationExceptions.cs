namespace ControleAtivos.Application.Common;

/// <summary>
/// Recurso inexistente ou fora do escopo do usuario.
///
/// As duas situacoes usam a mesma excecao de proposito: responder 404 para um
/// item que existe mas pertence a outra unidade evita revelar sua existencia a
/// quem nao deveria enxerga-lo.
/// </summary>
public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} nao encontrado.")
{
    public string Resource { get; } = resource;

    public object Key { get; } = key;
}

/// <summary>Operacao negada por falta de permissao ou de escopo organizacional.</summary>
public sealed class ForbiddenException(string message) : Exception(message);

/// <summary>Conflito com o estado atual (codigo duplicado, versao divergente).</summary>
public sealed class ConflictException(string message) : Exception(message);
