using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Registro historico imutavel de uma mudanca de valor.
///
/// Criado exclusivamente pelos metodos <c>ApplyPriceAdjustment</c> de
/// <see cref="Asset"/> e <see cref="Contract"/> — nunca diretamente por um caso
/// de uso. Isso garante que o valor anterior seja sempre o valor real que
/// estava vigente, e nao um numero informado pelo cliente da API.
/// </summary>
public sealed class PriceAdjustment : TenantEntity
{
    private PriceAdjustment()
    {
    }

    private PriceAdjustment(
        Guid id,
        Guid tenantId,
        PriceAdjustmentTargetType targetType,
        Guid targetId,
        Guid organizationalUnitId,
        Money previousAmount,
        Money newAmount,
        DateOnly effectiveDate,
        string reason,
        AdjustmentIndex indexApplied,
        Guid createdByUserId,
        Guid? approvedByUserId)
        : base(id, tenantId)
    {
        TargetType = targetType;
        TargetId = Guard.NotEmpty(targetId, nameof(targetId));
        OrganizationalUnitId = organizationalUnitId;
        PreviousAmount = previousAmount;
        NewAmount = newAmount;
        EffectiveDate = effectiveDate;
        Reason = Guard.MaxLength(Guard.NotEmpty(reason, nameof(reason)), 1000, nameof(reason));
        IndexApplied = indexApplied;
        CreatedByUserId = createdByUserId;
        ApprovedByUserId = approvedByUserId;
    }

    public PriceAdjustmentTargetType TargetType { get; private set; }

    public Guid TargetId { get; private set; }

    /// <summary>Copiado do alvo para permitir filtrar reajustes por escopo sem join.</summary>
    public Guid OrganizationalUnitId { get; private set; }

    public Money PreviousAmount { get; private set; }

    public Money NewAmount { get; private set; }

    /// <summary>Diferenca em valor absoluto (positiva em aumento, negativa em reducao).</summary>
    public decimal AbsoluteDifference => NewAmount.Amount - PreviousAmount.Amount;

    public Percentage PercentageDifference => NewAmount.PercentageDifferenceFrom(PreviousAmount);

    /// <summary>Impacto anualizado da mudanca, usado nos relatorios orcamentarios.</summary>
    public decimal AnnualImpact => AbsoluteDifference * 12m;

    public DateOnly EffectiveDate { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public AdjustmentIndex IndexApplied { get; private set; }

    public new Guid CreatedByUserId { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public Guid? DocumentId { get; private set; }

    public bool IsIncrease => AbsoluteDifference > 0m;

    /// <summary>Indica se o reajuste ultrapassa o limite configurado para alerta.</summary>
    public bool ExceedsThreshold(decimal thresholdPercentage) =>
        PercentageDifference.Absolute > thresholdPercentage;

    internal static PriceAdjustment ForAsset(
        Guid id,
        Guid tenantId,
        Asset asset,
        Money previousAmount,
        Money newAmount,
        DateOnly effectiveDate,
        string reason,
        AdjustmentIndex indexApplied,
        Guid createdByUserId,
        Guid? approvedByUserId) =>
        new(
            id,
            tenantId,
            PriceAdjustmentTargetType.Asset,
            asset.Id,
            asset.OrganizationalUnitId,
            previousAmount,
            newAmount,
            effectiveDate,
            reason,
            indexApplied,
            createdByUserId,
            approvedByUserId);

    internal static PriceAdjustment ForContract(
        Guid id,
        Guid tenantId,
        Contract contract,
        Money previousAmount,
        Money newAmount,
        DateOnly effectiveDate,
        string reason,
        AdjustmentIndex indexApplied,
        Guid createdByUserId,
        Guid? approvedByUserId) =>
        new(
            id,
            tenantId,
            PriceAdjustmentTargetType.Contract,
            contract.Id,
            contract.OrganizationalUnitId,
            previousAmount,
            newAmount,
            effectiveDate,
            reason,
            indexApplied,
            createdByUserId,
            approvedByUserId);

    public void AttachDocument(Guid documentId)
    {
        DocumentId = Guard.NotEmpty(documentId, nameof(documentId));
    }

    /// <summary>
    /// Simula um reajuste sem persistir nada. Usado pelo endpoint de simulacao
    /// para que a interface mostre o impacto antes da decisao.
    /// </summary>
    public static (decimal AbsoluteDifference, Percentage Percentage, decimal AnnualImpact) Simulate(
        Money currentAmount,
        Money newAmount)
    {
        var difference = newAmount.Amount - currentAmount.Amount;
        return (difference, newAmount.PercentageDifferenceFrom(currentAmount), difference * 12m);
    }
}
