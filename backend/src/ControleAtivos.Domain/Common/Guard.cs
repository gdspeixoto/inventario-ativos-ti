namespace ControleAtivos.Domain.Common;

/// <summary>Validacoes de invariantes reutilizadas pelas entidades.</summary>
public static class Guard
{
    public static string NotEmpty(string? value, string field)
    {
        DomainException.ThrowIf(string.IsNullOrWhiteSpace(value), $"O campo '{field}' e obrigatorio.");
        return value!.Trim();
    }

    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Guid NotEmpty(Guid value, string field)
    {
        DomainException.ThrowIf(value == Guid.Empty, $"O campo '{field}' e obrigatorio.");
        return value;
    }

    public static int NotNegative(int value, string field)
    {
        DomainException.ThrowIf(value < 0, $"O campo '{field}' nao pode ser negativo.");
        return value;
    }

    public static decimal NotNegative(decimal value, string field)
    {
        DomainException.ThrowIf(value < 0, $"O campo '{field}' nao pode ser negativo.");
        return value;
    }

    public static string MaxLength(string value, int max, string field)
    {
        DomainException.ThrowIf(
            value.Length > max,
            $"O campo '{field}' excede o tamanho maximo de {max} caracteres.");
        return value;
    }
}
