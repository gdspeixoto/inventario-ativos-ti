using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Lancamento de custo por competencia. Enquanto <c>Asset.MonthlyAmount</c>
/// representa o valor corrente, esta entidade guarda o que foi efetivamente
/// gasto em cada mes — o que permite reconstruir a serie historica mesmo depois
/// de varios reajustes.
/// </summary>
public sealed class CostEntry : TenantEntity
{
    private CostEntry()
    {
    }

    public CostEntry(
        Guid id,
        Guid tenantId,
        Guid organizationalUnitId,
        CostType type,
        CostCategory category,
        Money amount,
        DateOnly competenceMonth,
        string description)
        : base(id, tenantId)
    {
        OrganizationalUnitId = organizationalUnitId;
        Type = type;
        Category = category;
        Amount = amount;
        // Normaliza para o primeiro dia do mes: a competencia e mensal.
        CompetenceMonth = new DateOnly(competenceMonth.Year, competenceMonth.Month, 1);
        EffectiveDate = competenceMonth;
        Description = Guard.MaxLength(Guard.NotEmpty(description, nameof(description)), 500, nameof(description));
    }

    public Guid OrganizationalUnitId { get; private set; }

    public Guid? AssetId { get; private set; }

    public Asset? Asset { get; private set; }

    public Guid? ContractId { get; private set; }

    public Contract? Contract { get; private set; }

    public Guid? SupplierId { get; private set; }

    public Supplier? Supplier { get; private set; }

    public Guid? CostCenterId { get; private set; }

    public CostCenter? CostCenter { get; private set; }

    public CostType Type { get; private set; }

    public CostCategory Category { get; private set; }

    public Money Amount { get; private set; }

    /// <summary>Primeiro dia do mes de competencia.</summary>
    public DateOnly CompetenceMonth { get; private set; }

    public DateOnly EffectiveDate { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public void LinkToAsset(Asset asset)
    {
        DomainException.ThrowIf(asset.TenantId != TenantId, "O ativo pertence a outro tenant.");
        AssetId = asset.Id;
        Asset = asset;
        SupplierId = asset.SupplierId;
        ContractId = asset.ContractId;
        CostCenterId = asset.CostCenterId;
    }

    public void LinkToContract(Contract contract)
    {
        DomainException.ThrowIf(contract.TenantId != TenantId, "O contrato pertence a outro tenant.");
        ContractId = contract.Id;
        Contract = contract;
        SupplierId = contract.SupplierId;
    }
}
