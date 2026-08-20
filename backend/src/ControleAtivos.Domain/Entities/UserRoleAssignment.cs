using ControleAtivos.Domain.Common;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Papel concedido a um usuario dentro do sistema, guardado localmente.
///
/// A autenticacao continua sendo do provedor corporativo: e ele quem diz
/// <em>quem</em> a pessoa e. Ja a autorizacao — <em>o que</em> ela pode fazer
/// aqui dentro — e decidida por este registro. A separacao existe porque os
/// usuarios vem federados do AD e nao podem ser editados no Keycloak, mas o
/// controle de acesso precisa ser administravel pelo proprio produto.
///
/// Papeis vindos do token continuam valendo e sao somados a estes; nenhum dos
/// dois lados revoga o outro.
/// </summary>
public sealed class UserRoleAssignment : TenantEntity
{
    private UserRoleAssignment()
    {
    }

    public UserRoleAssignment(
        Guid id,
        Guid tenantId,
        AppUser user,
        string role,
        Guid? grantedByUserId = null,
        string? notes = null)
        : base(id, tenantId)
    {
        DomainException.ThrowIf(
            user.TenantId != tenantId,
            "O usuario pertence a outro tenant.");

        UserId = user.Id;
        User = user;
        Role = Guard.MaxLength(Guard.NotEmpty(role, nameof(role)), 100, nameof(role));
        GrantedByUserId = grantedByUserId;
        Notes = Guard.Optional(notes);
        GrantedAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid UserId { get; private set; }

    public AppUser User { get; private set; } = null!;

    /// <summary>Nome do papel, conforme <c>Roles</c> na camada de aplicacao.</summary>
    public string Role { get; private set; } = string.Empty;

    public DateTimeOffset GrantedAt { get; private set; }

    /// <summary>
    /// Quem concedeu. Nulo quando a concessao veio do bootstrap por
    /// configuracao, caso em que nao ha usuario responsavel na aplicacao.
    /// </summary>
    public Guid? GrantedByUserId { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public void Revoke(Guid? revokedByUserId = null)
    {
        DomainException.ThrowIf(!IsActive, "O papel ja foi revogado.");

        IsActive = false;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByUserId = revokedByUserId;
        Touch(revokedByUserId);
    }

    /// <summary>
    /// Reativa uma concessao revogada, preservando o registro original em vez
    /// de criar uma linha nova — o historico de quem teve acesso e quando
    /// continua legivel numa auditoria.
    /// </summary>
    public void Restore(Guid? grantedByUserId = null)
    {
        DomainException.ThrowIf(IsActive, "O papel ja esta ativo.");

        IsActive = true;
        RevokedAt = null;
        RevokedByUserId = null;
        GrantedAt = DateTimeOffset.UtcNow;
        GrantedByUserId = grantedByUserId;
        Touch(grantedByUserId);
    }
}
