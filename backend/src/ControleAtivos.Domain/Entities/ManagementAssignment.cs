using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Vinculo de gestao entre um usuario e um no organizacional.
///
/// Existe como entidade propria — e nao como campo <c>GerenteId</c> na unidade —
/// porque um gerente pode liderar varias areas, unidades ou nucleos ao mesmo
/// tempo, e uma mesma unidade pode ter papeis distintos (gerente, responsavel
/// tecnico, responsavel financeiro) atribuidos a pessoas diferentes.
///
/// O vinculo e temporal: <see cref="EndDate"/> encerra sem apagar o historico,
/// preservando a rastreabilidade de quem respondia pela unidade em cada periodo.
/// </summary>
public sealed class ManagementAssignment : TenantEntity
{
    private ManagementAssignment()
    {
    }

    public ManagementAssignment(
        Guid id,
        Guid tenantId,
        AppUser user,
        OrganizationalUnit organizationalUnit,
        ManagementRole role,
        DateOnly startDate,
        bool isPrimary = false)
        : base(id, tenantId)
    {
        DomainException.ThrowIf(user.TenantId != tenantId, "O usuario pertence a outro tenant.");
        DomainException.ThrowIf(
            organizationalUnit.TenantId != tenantId,
            "A unidade organizacional pertence a outro tenant.");

        UserId = user.Id;
        User = user;
        OrganizationalUnitId = organizationalUnit.Id;
        OrganizationalUnit = organizationalUnit;
        Role = role;
        StartDate = startDate;
        IsPrimary = isPrimary;

        /*
         * Herda o escopo por padrao: quem gerencia uma Area Tecnologica enxerga
         * as Unidades Operacionais e os Nucleos abaixo dela. Pode ser desligado
         * para vinculos pontuais (ex.: responsavel financeiro de um unico nucleo).
         */
        IncludesDescendants = true;
    }

    public Guid UserId { get; private set; }

    public AppUser User { get; private set; } = null!;

    public Guid OrganizationalUnitId { get; private set; }

    public OrganizationalUnit OrganizationalUnit { get; private set; } = null!;

    public ManagementRole Role { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    /// <summary>Vinculo principal do usuario, usado como padrao na interface.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Se verdadeiro, o escopo alcanca toda a sub-arvore da unidade.</summary>
    public bool IncludesDescendants { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActiveOn(DateOnly reference) =>
        StartDate <= reference && (EndDate is null || EndDate >= reference);

    public bool IsCurrentlyActive() => IsActiveOn(DateOnly.FromDateTime(DateTime.UtcNow));

    public void RestrictToOwnUnit(Guid? userId = null)
    {
        IncludesDescendants = false;
        Touch(userId);
    }

    public void ExtendToDescendants(Guid? userId = null)
    {
        IncludesDescendants = true;
        Touch(userId);
    }

    public void MarkAsPrimary(Guid? userId = null)
    {
        IsPrimary = true;
        Touch(userId);
    }

    public void Finish(DateOnly endDate, string? notes = null, Guid? userId = null)
    {
        DomainException.ThrowIf(
            endDate < StartDate,
            "A data de termino nao pode ser anterior a data de inicio.");

        EndDate = endDate;
        Notes = Guard.Optional(notes);
        IsPrimary = false;
        Touch(userId);
    }
}
