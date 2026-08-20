namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Identidade do chamador na requisicao atual, resolvida a partir do token
/// validado pelo Keycloak — nunca de dados enviados no corpo da requisicao.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Id local do usuario (tabela <c>users</c>), quando ja provisionado.</summary>
    Guid? UserId { get; }

    /// <summary>Claim <c>sub</c> do provedor de identidade.</summary>
    string? ExternalSubject { get; }

    string? DisplayName { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    IReadOnlySet<string> Roles { get; }

    IReadOnlySet<string> Permissions { get; }

    bool HasPermission(string permission);

    bool HasRole(string role);
}
