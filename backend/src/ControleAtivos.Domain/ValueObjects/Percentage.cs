using System.Globalization;

namespace ControleAtivos.Domain.ValueObjects;

/// <summary>
/// Percentual com 4 casas decimais. Aceita valores negativos: reajuste para
/// baixo (renegociacao, glosa) e um caso legitimo e precisa aparecer no historico.
/// </summary>
public readonly record struct Percentage
{
    public Percentage(decimal value)
    {
        Value = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public decimal Value { get; }

    public static Percentage Zero => new(0m);

    public bool IsIncrease => Value > 0m;

    public bool IsDecrease => Value < 0m;

    public decimal Absolute => Math.Abs(Value);

    /// <summary>Formato brasileiro, usado nas descricoes da timeline.</summary>
    private static readonly NumberFormatInfo BrazilianFormat = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 2,
    };

    public override string ToString() => $"{Value.ToString("N2", BrazilianFormat)}%";
}
