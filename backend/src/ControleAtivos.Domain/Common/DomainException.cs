namespace ControleAtivos.Domain.Common;

/// <summary>
/// Violacao de invariante do dominio. A API traduz para HTTP 422 — e um erro do
/// chamador, nao uma falha do servidor.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public static void ThrowIf(bool condition, string message)
    {
        if (condition)
        {
            throw new DomainException(message);
        }
    }
}
