using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Licenca de software (Microsoft 365, Power BI, SQL Server, etc.).
///
/// O valor mensal e derivado: <c>quantidade contratada x valor unitario</c>.
/// Por isso a alteracao de quantidade e a alteracao de preco unitario sao
/// operacoes distintas, ambas refletidas no custo mensal e na timeline.
/// </summary>
public sealed class LicenseAsset : Asset
{
    private LicenseAsset()
    {
    }

    public LicenseAsset(
        Guid id,
        Guid tenantId,
        OrganizationalUnit organizationalUnit,
        string name,
        string code,
        string manufacturer,
        string product,
        int contractedQuantity,
        Money unitPrice)
        : base(
            id,
            tenantId,
            AssetKind.License,
            organizationalUnit,
            name,
            code,
            unitPrice.Multiply(Guard.NotNegative(contractedQuantity, nameof(contractedQuantity))))
    {
        DomainException.ThrowIf(contractedQuantity <= 0, "A quantidade contratada deve ser maior que zero.");

        Manufacturer = Guard.MaxLength(Guard.NotEmpty(manufacturer, nameof(manufacturer)), 120, nameof(manufacturer));
        Product = Guard.MaxLength(Guard.NotEmpty(product, nameof(product)), 200, nameof(product));
        ContractedQuantity = contractedQuantity;
        UsedQuantity = 0;
        UnitPrice = unitPrice;

        RaiseTimeline(
            TimelineEventType.Criacao,
            $"Licenca {name} cadastrada",
            $"{contractedQuantity} licenca(s) de {product} a {unitPrice} por unidade. " +
            $"Custo mensal: {MonthlyAmount}.");
    }

    protected override TimelineEntityType TimelineType => TimelineEntityType.License;

    public string Manufacturer { get; private set; } = string.Empty;

    public string Product { get; private set; } = string.Empty;

    public string? Plan { get; private set; }

    public int ContractedQuantity { get; private set; }

    public int UsedQuantity { get; private set; }

    /// <summary>Saldo disponivel. Derivado, nunca armazenado de forma independente.</summary>
    public int AvailableQuantity => ContractedQuantity - UsedQuantity;

    public Money UnitPrice { get; private set; }

    /// <summary>Percentual de consumo das licencas contratadas.</summary>
    public decimal UtilizationRate =>
        ContractedQuantity == 0 ? 0m : decimal.Round((decimal)UsedQuantity / ContractedQuantity * 100m, 2);

    /// <summary>Corrige fabricante e produto, sem alterar valores nem quantidade.</summary>
    public void CorrectProduct(string manufacturer, string product, Guid? userId = null)
    {
        Manufacturer = Guard.MaxLength(
            Guard.NotEmpty(manufacturer, nameof(manufacturer)), 120, nameof(manufacturer));
        Product = Guard.MaxLength(
            Guard.NotEmpty(product, nameof(product)), 200, nameof(product));
        Touch(userId);
    }

    public void DefinePlan(string? plan, BillingType billingType, Guid? userId = null)
    {
        Plan = Guard.Optional(plan);
        BillingType = billingType;
        Touch(userId);
    }

    /// <summary>
    /// Altera a quantidade contratada e recalcula o custo mensal. Devolve o
    /// impacto financeiro para que o caso de uso possa exibi-lo ao usuario.
    /// </summary>
    public Money ChangeContractedQuantity(int newQuantity, string reason, Guid? userId = null)
    {
        DomainException.ThrowIf(newQuantity <= 0, "A quantidade contratada deve ser maior que zero.");
        DomainException.ThrowIf(
            newQuantity == ContractedQuantity,
            "A nova quantidade deve ser diferente da atual.");
        DomainException.ThrowIf(
            newQuantity < UsedQuantity,
            $"Nao e possivel reduzir para {newQuantity}: existem {UsedQuantity} licenca(s) em uso.");

        var previousQuantity = ContractedQuantity;
        var previousMonthly = MonthlyAmount;

        ContractedQuantity = newQuantity;
        MonthlyAmount = UnitPrice.Multiply(newQuantity);
        Touch(userId);

        var monthlyImpact = new Money(
            Math.Abs(MonthlyAmount.Amount - previousMonthly.Amount),
            MonthlyAmount.Currency);

        RaiseTimeline(
            TimelineEventType.AlteracaoQuantidade,
            "Quantidade contratada alterada",
            $"Quantidade alterada de {previousQuantity} para {newQuantity}. " +
            $"Custo mensal: de {previousMonthly} para {MonthlyAmount} " +
            $"(impacto mensal de {monthlyImpact}, anual de {monthlyImpact.ToAnnual()}). " +
            $"Motivo: {Guard.NotEmpty(reason, nameof(reason))}",
            previousMonthly.Amount,
            MonthlyAmount.Amount);

        return monthlyImpact;
    }

    /// <summary>Atualiza o consumo real, normalmente sincronizado do tenant Microsoft.</summary>
    public void UpdateUsage(int usedQuantity, Guid? userId = null)
    {
        Guard.NotNegative(usedQuantity, nameof(usedQuantity));
        DomainException.ThrowIf(
            usedQuantity > ContractedQuantity,
            $"Uso ({usedQuantity}) nao pode exceder a quantidade contratada ({ContractedQuantity}).");

        UsedQuantity = usedQuantity;
        Touch(userId);
    }

    public void ScheduleNextAdjustment(DateOnly date, Guid? userId = null)
    {
        NextAdjustmentDate = date;
        Touch(userId);
    }

    /// <summary>
    /// Quando o valor mensal e reajustado diretamente, o preco unitario e
    /// recalculado para manter a coerencia entre os dois campos.
    /// </summary>
    protected override void OnPriceAdjusted(Money newMonthlyAmount)
    {
        if (ContractedQuantity > 0)
        {
            UnitPrice = new Money(
                newMonthlyAmount.Amount / ContractedQuantity,
                newMonthlyAmount.Currency);
        }
    }
}
