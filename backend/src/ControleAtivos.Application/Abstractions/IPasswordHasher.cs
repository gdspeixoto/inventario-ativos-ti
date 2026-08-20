namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Derivacao e verificacao de senha para as credenciais locais de emergencia.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
