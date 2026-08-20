using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.Events;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Contrato firmado com um fornecedor. Um contrato agrega varios ativos
/// (licencas, servidores, servicos) e concentra as regras de reajuste e vigencia.
/// </summary>
public sealed class Contract : TenantEntity
{
    private readonly List<Asset> _assets = [];

    private Contract()
    {
    }

    public Contract(
        Guid id,
        Guid tenantId,
        Supplier supplier,
        OrganizationalUnit organizationalUnit,
        string number,
        string name,
        ContractCategory category,
        DateOnly startDate,
        DateOnly endDate,
        Money monthlyAmount)
        : base(id, tenantId)
    {
        DomainException.ThrowIf(supplier.TenantId != tenantId, "O fornecedor pertence a outro tenant.");
        DomainException.ThrowIf(
            organizationalUnit.TenantId != tenantId,
            "A unidade organizacional pertence a outro tenant.");
        DomainException.ThrowIf(endDate <= startDate, "A vigencia final deve ser posterior ao inicio.");

        SupplierId = supplier.Id;
        Supplier = supplier;
        OrganizationalUnitId = organizationalUnit.Id;
        OrganizationalUnit = organizationalUnit;
        Number = Guard.MaxLength(Guard.NotEmpty(number, nameof(number)), 60, nameof(number));
        Name = Guard.MaxLength(Guard.NotEmpty(name, nameof(name)), 200, nameof(name));
        Category = category;
        StartDate = startDate;
        EndDate = endDate;
        MonthlyAmount = monthlyAmount;
        Status = ContractStatus.Ativo;
        AdjustmentIndex = AdjustmentIndex.NegociacaoManual;
        AdjustmentPeriodicity = AdjustmentPeriodicity.Anual;

        Raise(new TimelineEntryRequested(
            TimelineEntityType.Contract,
            Id,
            TimelineEventType.Criacao,
            $"Contrato {Number} criado",
            $"Contrato '{Name}' com o fornecedor {supplier.Name}, vigente de {startDate:dd/MM/yyyy} a {endDate:dd/MM/yyyy}.",
            OrganizationalUnitId));
    }

    public Guid SupplierId { get; private set; }

    public Supplier Supplier { get; private set; } = null!;

    public Guid OrganizationalUnitId { get; private set; }

    public OrganizationalUnit OrganizationalUnit { get; private set; } = null!;

    public string Number { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public ContractCategory Category { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public Money MonthlyAmount { get; private set; }

    /// <summary>Projecao anual derivada do valor mensal.</summary>
    public Money AnnualAmount => MonthlyAmount.ToAnnual();

    public AdjustmentIndex AdjustmentIndex { get; private set; }

    public AdjustmentPeriodicity AdjustmentPeriodicity { get; private set; }

    public DateOnly? LastAdjustmentDate { get; private set; }

    public DateOnly? NextAdjustmentDate { get; private set; }

    public ContractStatus Status { get; private set; }

    public Guid? InternalResponsibleUserId { get; private set; }

    public AppUser? InternalResponsible { get; private set; }

    public IReadOnlyCollection<Asset> Assets => _assets.AsReadOnly();

    public int DaysUntilExpiration(DateOnly reference) => EndDate.DayNumber - reference.DayNumber;

    public bool IsExpiringWithin(int days, DateOnly reference) =>
        Status == ContractStatus.Ativo && DaysUntilExpiration(reference) is var d && d >= 0 && d <= days;

    public void DefineAdjustmentRules(
        AdjustmentIndex index,
        AdjustmentPeriodicity periodicity,
        DateOnly? nextAdjustmentDate,
        Guid? userId = null)
    {
        AdjustmentIndex = index;
        AdjustmentPeriodicity = periodicity;
        NextAdjustmentDate = nextAdjustmentDate;
        Touch(userId);
    }

    public void AssignResponsible(AppUser user, Guid? userId = null)
    {
        DomainException.ThrowIf(user.TenantId != TenantId, "O usuario pertence a outro tenant.");
        InternalResponsibleUserId = user.Id;
        InternalResponsible = user;
        Touch(userId);

        Raise(new TimelineEntryRequested(
            TimelineEntityType.Contract,
            Id,
            TimelineEventType.MudancaResponsavel,
            "Responsavel interno alterado",
            $"O contrato passou a ser acompanhado por {user.DisplayName}.",
            OrganizationalUnitId));
    }

    /// <summary>
    /// Prorroga a vigencia. Nao altera valores: um reajuste eventual e registrado
    /// separadamente por <see cref="PriceAdjustment"/>, mantendo os dois fatos
    /// distinguiveis na timeline.
    /// </summary>
    public void Renew(DateOnly newEndDate, string? reason = null, Guid? userId = null)
    {
        DomainException.ThrowIf(newEndDate <= EndDate, "A nova vigencia deve ser posterior a atual.");

        var previousEnd = EndDate;
        EndDate = newEndDate;
        Status = ContractStatus.Ativo;
        Touch(userId);

        Raise(new TimelineEntryRequested(
            TimelineEntityType.Contract,
            Id,
            TimelineEventType.Renovacao,
            "Contrato renovado",
            $"Vigencia prorrogada de {previousEnd:dd/MM/yyyy} para {newEndDate:dd/MM/yyyy}. {Guard.Optional(reason)}".Trim(),
            OrganizationalUnitId));
    }

    /// <summary>
    /// Aplica um novo valor mensal. Retorna o registro historico do reajuste,
    /// que o caso de uso persiste junto — o valor corrente e o historico mudam
    /// na mesma transacao, nunca isoladamente.
    /// </summary>
    public PriceAdjustment ApplyPriceAdjustment(
        Money newMonthlyAmount,
        DateOnly effectiveDate,
        string reason,
        AdjustmentIndex index,
        Guid createdByUserId,
        Guid? approvedByUserId = null)
    {
        DomainException.ThrowIf(
            Status is ContractStatus.Cancelado or ContractStatus.Encerrado,
            "Nao e possivel reajustar um contrato encerrado ou cancelado.");

        var previous = MonthlyAmount;
        DomainException.ThrowIf(
            newMonthlyAmount.Amount == previous.Amount,
            "O novo valor deve ser diferente do valor atual.");

        MonthlyAmount = newMonthlyAmount;
        LastAdjustmentDate = effectiveDate;
        NextAdjustmentDate = ProjectNextAdjustment(effectiveDate);
        AdjustmentIndex = index;
        Touch(createdByUserId);

        var adjustment = PriceAdjustment.ForContract(
            Guid.NewGuid(),
            TenantId,
            this,
            previous,
            newMonthlyAmount,
            effectiveDate,
            reason,
            index,
            createdByUserId,
            approvedByUserId);

        Raise(new TimelineEntryRequested(
            TimelineEntityType.Contract,
            Id,
            TimelineEventType.ReajusteAplicado,
            "Reajuste aplicado",
            $"Valor mensal alterado de {previous} para {newMonthlyAmount} " +
            $"({adjustment.PercentageDifference}). Motivo: {adjustment.Reason}",
            OrganizationalUnitId,
            previous.Amount,
            newMonthlyAmount.Amount));

        return adjustment;
    }

    public void Terminate(ContractStatus status, string reason, Guid? userId = null)
    {
        DomainException.ThrowIf(
            status is not (ContractStatus.Encerrado or ContractStatus.Cancelado or ContractStatus.Suspenso),
            "Status invalido para encerramento de contrato.");

        Status = status;
        Touch(userId);

        Raise(new TimelineEntryRequested(
            TimelineEntityType.Contract,
            Id,
            TimelineEventType.Cancelamento,
            $"Contrato {status.ToString().ToLowerInvariant()}",
            Guard.NotEmpty(reason, nameof(reason)),
            OrganizationalUnitId));
    }

    private DateOnly? ProjectNextAdjustment(DateOnly from) => AdjustmentPeriodicity switch
    {
        AdjustmentPeriodicity.Mensal => from.AddMonths(1),
        AdjustmentPeriodicity.Trimestral => from.AddMonths(3),
        AdjustmentPeriodicity.Semestral => from.AddMonths(6),
        AdjustmentPeriodicity.Anual => from.AddYears(1),
        _ => null,
    };
}
