using ControleAtivos.Domain.Common;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Projecao local do usuario autenticado no Keycloak. O sistema nao guarda
/// senha nem faz autenticacao propria: apenas espelha identidade e vinculo
/// organizacional para permitir atribuicoes gerenciais, responsaveis e
/// auditoria com nome legivel.
/// </summary>
public sealed class AppUser : TenantEntity
{
    private readonly List<ManagementAssignment> _managementAssignments = [];

    private AppUser()
    {
    }

    public AppUser(Guid id, Guid tenantId, string externalSubject, string displayName, string email)
        : base(id, tenantId)
    {
        ExternalSubject = Guard.NotEmpty(externalSubject, nameof(externalSubject));
        DisplayName = Guard.MaxLength(Guard.NotEmpty(displayName, nameof(displayName)), 200, nameof(displayName));
        Email = NormalizeEmail(email);
        IsActive = true;
    }

    /// <summary>Claim <c>sub</c> do Keycloak. Chave estavel de correlacao.</summary>
    public string ExternalSubject { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? JobTitle { get; private set; }

    /// <summary>Unidade organizacional de lotacao (nao confundir com escopo de acesso).</summary>
    public Guid? PrimaryOrganizationalUnitId { get; private set; }

    public OrganizationalUnit? PrimaryOrganizationalUnit { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public IReadOnlyCollection<ManagementAssignment> ManagementAssignments =>
        _managementAssignments.AsReadOnly();

    public void SyncFromIdentityProvider(string displayName, string email, string? jobTitle)
    {
        DisplayName = Guard.MaxLength(Guard.NotEmpty(displayName, nameof(displayName)), 200, nameof(displayName));
        Email = NormalizeEmail(email);
        JobTitle = Guard.Optional(jobTitle);
        Touch();
    }

    public void AssignPrimaryUnit(OrganizationalUnit unit, Guid? userId = null)
    {
        DomainException.ThrowIf(unit.TenantId != TenantId, "A unidade pertence a outro tenant.");
        PrimaryOrganizationalUnitId = unit.Id;
        PrimaryOrganizationalUnit = unit;
        Touch(userId);
    }

    public void RegisterLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        Touch(userId);
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = Guard.NotEmpty(email, nameof(email)).Trim().ToLowerInvariant();
        DomainException.ThrowIf(!normalized.Contains('@'), "E-mail invalido.");
        return Guard.MaxLength(normalized, 320, nameof(email));
    }
}
