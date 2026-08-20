using System.Globalization;
using ControleAtivos.Domain.Common;

namespace ControleAtivos.Domain.ValueObjects;

/// <summary>
/// Valor monetario com moeda. Impede a soma acidental de valores em moedas
/// diferentes — erro comum quando contratos em USD convivem com contratos em BRL.
/// </summary>
public readonly record struct Money
{
    public const string DefaultCurrency = "BRL";

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        DomainException.ThrowIf(amount < 0, "Valor monetario nao pode ser negativo.");
        DomainException.ThrowIf(string.IsNullOrWhiteSpace(currency), "Moeda e obrigatoria.");
        DomainException.ThrowIf(currency.Length != 3, "Moeda deve seguir o padrao ISO 4217 (3 letras).");

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        DomainException.ThrowIf(factor < 0, "Fator de multiplicacao nao pode ser negativo.");
        return new Money(Amount * factor, Currency);
    }

    /// <summary>Projecao anual de um valor mensal recorrente.</summary>
    public Money ToAnnual() => Multiply(12m);

    /// <summary>
    /// Variacao percentual deste valor em relacao a um valor anterior.
    /// Um valor anterior zerado nao tem percentual definido.
    /// </summary>
    public Percentage PercentageDifferenceFrom(Money previous)
    {
        EnsureSameCurrency(previous);

        if (previous.Amount == 0m)
        {
            return Percentage.Zero;
        }

        return new Percentage((Amount - previous.Amount) / previous.Amount * 100m);
    }

    private void EnsureSameCurrency(Money other) =>
        DomainException.ThrowIf(
            Currency != other.Currency,
            $"Operacao invalida entre moedas diferentes: {Currency} e {other.Currency}.");

    /*
     * As descricoes geradas aqui vao direto para a timeline lida pelo usuario.
     * A aplicacao roda com InvariantGlobalization, entao o formato brasileiro
     * (milhar com ponto, decimal com virgula) e montado explicitamente em vez
     * de depender da cultura do processo.
     */
    private static readonly NumberFormatInfo BrazilianFormat = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 2,
    };

    public override string ToString() => $"{Currency} {Amount.ToString("N2", BrazilianFormat)}";
}
