using System.Security.Cryptography;
using ControleAtivos.Application.Abstractions;

namespace ControleAtivos.Api.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256, da propria BCL.
///
/// A escolha e deliberadamente conservadora. Argon2id resiste melhor a ataque
/// com hardware dedicado, mas exige dependencia externa; PBKDF2 e recomendado
/// pelo NIST, esta na biblioteca padrao e, com contagem de iteracoes adequada,
/// protege de sobra um punhado de credenciais de emergencia.
///
/// Formato armazenado: <c>pbkdf2$sha256$&lt;iteracoes&gt;$&lt;salt&gt;$&lt;hash&gt;</c>.
/// Os parametros viajam junto com o hash para que aumentar o custo no futuro
/// nao invalide as senhas ja cadastradas.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 210_000;
    private const string Prefix = "pbkdf2$sha256";

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join('$', Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        var parts = hash.Split('$');

        // pbkdf2 | sha256 | iteracoes | salt | hash
        if (parts.Length != 5 || parts[0] != "pbkdf2" || parts[1] != "sha256")
        {
            return false;
        }

        if (!int.TryParse(parts[2], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

        // Comparacao em tempo fixo: um `==` vazaria o tamanho do prefixo
        // correto pelo tempo de resposta.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
