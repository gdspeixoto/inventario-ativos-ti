using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.Events;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Ativo controlado pelo setor. Classe base de <see cref="LicenseAsset"/> e
/// <see cref="ServerAsset"/>, mapeada como <em>table-per-hierarchy</em>: o que e
/// comum (dono, unidade, fornecedor, contrato, custo mensal, status) vive aqui;
/// o que e especifico vive na especializacao.
/// </summary>
public abstract class Asset : TenantEntity
{
    private readonly List<CostEntry> _costEntries = [];

    protected Asset()
    {
    }

    protected Asset(
        Guid id,
        Guid tenantId,
        AssetKind kind,
        OrganizationalUnit organizationalUnit,
        string name,
        string code,
        Money monthlyAmount)
        : base(id, tenantId)
    {
        DomainException.ThrowIf(
            organizationalUnit.TenantId != tenantId,
            "A unidade organizacional pertence a outro tenant.");

        Kind = kind;
        OrganizationalUnitId = organizationalUnit.Id;
        OrganizationalUnit = organizationalUnit;
        Name = Guard.MaxLength(Guard.NotEmpty(name, nameof(name)), 200, nameof(name));
        Code = Guard.MaxLength(Guard.NotEmpty(code, nameof(code)).ToUpperInvariant(), 60, nameof(code));
        MonthlyAmount = monthlyAmount;
        Status = AssetStatus.Ativo;
        BillingType = BillingType.Mensal;
    }

    public AssetKind Kind { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public AssetStatus Status { get; private set; }

    public BillingType BillingType { get; protected set; }

    public Guid OrganizationalUnitId { get; private set; }

    public OrganizationalUnit OrganizationalUnit { get; private set; } = null!;

    public Guid? SupplierId { get; private set; }

    public Supplier? Supplier { get; private set; }

    public Guid? ContractId { get; private set; }

    public Contract? Contract { get; private set; }

    public Guid? CostCenterId { get; private set; }

    public CostCenter? CostCenter { get; private set; }

    public Guid? TechnicalResponsibleUserId { get; private set; }

    public AppUser? TechnicalResponsible { get; private set; }

    public Guid? FinancialResponsibleUserId { get; private set; }

    public AppUser? FinancialResponsible { get; private set; }

    /// <summary>Custo mensal corrente do ativo. Alterado somente via reajuste.</summary>
    public Money MonthlyAmount { get; protected set; }

    public Money AnnualAmount => MonthlyAmount.ToAnnual();

    public DateOnly? StartDate { get; protected set; }

    public DateOnly? RenewalDate { get; protected set; }

    public DateOnly? LastAdjustmentDate { get; protected set; }

    public DateOnly? NextAdjustmentDate { get; protected set; }

    public IReadOnlyCollection<CostEntry> CostEntries => _costEntries.AsReadOnly();

    /// <summary>Tipo usado nos eventos de timeline desta especializacao.</summary>
    protected abstract TimelineEntityType TimelineType { get; }

    /// <summary>
    /// Corrige os dados cadastrais.
    ///
    /// Correcao e coisa distinta de mudanca de estado: o ativo continua sendo o
    /// mesmo, e o que muda e o registro que estava errado — um nome digitado
    /// incorretamente, um codigo trocado. Por isso gera evento de
    /// <see cref="TimelineEventType.EdicaoCadastral"/> e nao de cancelamento,
    /// e por isso a timeline guarda o antes e o depois: quem auditar precisa
    /// saber que o valor mudou e qual era o anterior.
    /// </summary>
    public void Rename(string name, string code, Guid? userId = null)
    {
        var newName = Guard.MaxLength(Guard.NotEmpty(name, nameof(name)), 200, nameof(name));
        var newCode = Guard.MaxLength(Guard.NotEmpty(code, nameof(code)), 60, nameof(code)).ToUpperInvariant();

        if (newName == Name && newCode == Code)
        {
            return;
        }

        var previousName = Name;
        var previousCode = Code;

        Name = newName;
        Code = newCode;
        Touch(userId);

        Raise(new TimelineEntryRequested(
            TimelineType,
            Id,
            TimelineEventType.EdicaoCadastral,
            "Dados cadastrais corrigidos",
            $"Nome: '{previousName}' para '{Name}'. Codigo: '{previousCode}' para '{Code}'.",
            OrganizationalUnitId));
    }

    /// <summary>
    /// Se o ativo pode ser removido em definitivo.
    ///
    /// A exclusao existe para desfazer um cadastro que nao deveria ter sido
    /// feito — nao para encerrar algo que existiu. Um ativo que ja acumulou
    /// custos ou reajustes tem historia financeira, e apaga-lo deixaria
    /// relatorios sem explicacao; nesse caso o caminho correto e desativar.
    /// </summary>
    public bool CanBeDeleted => _costEntries.Count == 0 && LastAdjustmentDate is null;

    public void Describe(string? description, Guid? userId = null)
    {
        Description = Guard.Optional(description);
        Touch(userId);
    }

    public void DefineValidity(DateOnly? startDate, DateOnly? renewalDate, Guid? userId = null)
    {
        DomainException.ThrowIf(
            startDate is not null && renewalDate is not null && renewalDate <= startDate,
            "A data de renovacao deve ser posterior ao inicio da vigencia.");

        StartDate = startDate;
        RenewalDate = renewalDate;
        Touch(userId);
    }

    public void LinkToSupplier(Supplier supplier, Guid? userId = null)
    {
        DomainException.ThrowIf(supplier.TenantId != TenantId, "O fornecedor pertence a outro tenant.");

        var previous = Supplier?.Name;
        SupplierId = supplier.Id;
        Supplier = supplier;
        Touch(userId);

        Raise(new TimelineEntryRequested(
            TimelineType,
            Id,
            TimelineEventType.MudancaFornecedor,
            "Fornecedor alterado",
            previous is null
                ? $"Fornecedor definido como {supplier.Name}."
                : $"Fornecedor alterado de {previous} para {supplier.Name}.",
            OrganizationalUnitId));
    }

    public void LinkToContract(Contract contract, Guid? userId = null)
    {
        DomainException.ThrowIf(contract.TenantId != TenantId, "O contrato pertence a outro tenant.");
        ContractId = contract.Id;
        Contract = contract;
        Touch(userId);
    }

    public void AssignCostCenter(CostCenter costCenter, Guid? userId = null)
    {
        DomainException.ThrowIf(costCenter.TenantId != TenantId, "O centro de custo pertence a outro tenant.");
        CostCenterId = costCenter.Id;
        CostCenter = costCenter;
        Touch(userId);
    }

    public void AssignResponsibles(AppUser? technical, AppUser? financial, Guid? userId = null)
    {
        if (technical is not null)
        {
            DomainException.ThrowIf(technical.TenantId != TenantId, "O usuario pertence a outro tenant.");
            TechnicalResponsibleUserId = technical.Id;
            TechnicalResponsible = technical;
        }

        if (financial is not null)
        {
            DomainException.ThrowIf(financial.TenantId != TenantId, "O usuario pertence a outro tenant.");
            FinancialResponsibleUserId = financial.Id;
            FinancialResponsible = financial;
        }

        Touch(userId);

        Raise(new TimelineEntryRequested(
            TimelineType,
            Id,
            TimelineEventType.MudancaResponsavel,
            "Responsaveis atualizados",
            $"Tecnico: {technical?.DisplayName ?? "sem alteracao"}. " +
            $"Financeiro: {financial?.DisplayName ?? "sem alteracao"}.",
            OrganizationalUnitId));
    }

    public void ChangeStatus(AssetStatus status, string reason, Guid? userId = null)
    {
        DomainException.ThrowIf(
            status is AssetStatus.Cancelado or AssetStatus.Descontinuado && string.IsNullOrWhiteSpace(reason),
            "Cancelamento e descontinuacao exigem justificativa.");

        var previous = Status;
        Status = status;
        Touch(userId);

        Raise(new TimelineEntryRequested(
            TimelineType,
            Id,
            status is AssetStatus.Cancelado or AssetStatus.Descontinuado
                ? TimelineEventType.Cancelamento
                : TimelineEventType.EdicaoCadastral,
            $"Status alterado para {status}",
            $"Status alterado de {previous} para {status}. Motivo: {Guard.NotEmpty(reason, nameof(reason))}",
            OrganizationalUnitId));
    }

    /// <summary>
    /// Altera o valor mensal e devolve o registro historico correspondente.
    /// O percentual e a diferenca sao calculados aqui — nunca aceitos do cliente.
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
            Status is AssetStatus.Cancelado or AssetStatus.Descontinuado,
            "Nao e possivel reajustar um ativo cancelado ou descontinuado.");

        var previous = MonthlyAmount;
        DomainException.ThrowIf(
            newMonthlyAmount.Amount == previous.Amount,
            "O novo valor deve ser diferente do valor atual.");

        MonthlyAmount = newMonthlyAmount;
        LastAdjustmentDate = effectiveDate;
        Touch(createdByUserId);
        OnPriceAdjusted(newMonthlyAmount);

        var adjustment = PriceAdjustment.ForAsset(
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
            TimelineType,
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

    /// <summary>Gancho para a especializacao recalcular derivados apos o reajuste.</summary>
    protected virtual void OnPriceAdjusted(Money newMonthlyAmount)
    {
    }

    protected void RaiseTimeline(
        TimelineEventType eventType,
        string title,
        string description,
        decimal? previousAmount = null,
        decimal? newAmount = null) =>
        Raise(new TimelineEntryRequested(
            TimelineType,
            Id,
            eventType,
            title,
            description,
            OrganizationalUnitId,
            previousAmount,
            newAmount));
}
