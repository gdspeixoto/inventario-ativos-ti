using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ControleAtivos.Api.Configuration;
using ControleAtivos.Application.Abstractions;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ControleAtivos.Api.Security;

/// <summary>
/// Autenticacao por credencial local, para uso de emergencia.
///
/// O token emitido aqui e deliberadamente distinguivel do token corporativo:
/// carrega <c>amr=pwd</c> e emissor proprio, de modo que uma investigacao
/// consiga separar o que entrou pelo SSO do que entrou pelo caminho de
/// excecao. Ele tambem vive menos tempo e nao tem refresh — reautenticar e
/// barato, e uma sessao longa por senha e exatamente o que nao se quer.
/// </summary>
public sealed class LocalAuthenticationService(
    ControleAtivosDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITenantContext tenantContext,
    IOptions<AccessOptions> accessOptions,
    ILogger<LocalAuthenticationService> logger)
{
    public const string Issuer = "controle-ativos-local";
    public const string Audience = "controle-ativos-api";

    private readonly LocalLoginOptions _options = accessOptions.Value.LocalLogin;

    /// <summary>
    /// Valida usuario e senha e devolve o token, ou <c>null</c> quando a
    /// autenticacao falha.
    ///
    /// Todas as recusas retornam <c>null</c> sem distincao: dizer ao chamador
    /// que o usuario existe mas a senha esta errada, ou que a conta esta
    /// bloqueada, entrega ao atacante justamente o que ele precisa para
    /// enumerar contas.
    /// </summary>
    public async Task<LocalLoginResult?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var normalized = username.Trim().ToLowerInvariant();

        /*
         * O login acontece antes de existir tenant na requisicao, entao a
         * consulta precisa atravessar o filtro global. E o unico ponto do
         * sistema que faz isso a partir de dado nao autenticado — por isso a
         * busca e por chave unica e o resultado so vira sessao depois de a
         * senha conferir.
         */
        using var bypass = tenantContext.BypassTenantFilter();

        var credential = await dbContext.LocalCredentials
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Username == normalized, cancellationToken);

        if (credential is null)
        {
            // Deriva mesmo assim: sem isto, a diferenca de tempo entre usuario
            // existente e inexistente permite enumerar contas validas.
            passwordHasher.Verify(password, DummyHash);
            return null;
        }

        if (!credential.CanAuthenticate)
        {
            logger.LogWarning(
                "Tentativa de login local recusada para {Username}: ativa={IsActive}, expirada={IsExpired}, bloqueada={IsLocked}.",
                normalized,
                credential.IsActive,
                credential.IsExpired,
                credential.IsLockedOut);

            return null;
        }

        if (!passwordHasher.Verify(password, credential.PasswordHash))
        {
            credential.RegisterFailedAttempt();
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogWarning(
                "Senha incorreta no login local de {Username}. Tentativas: {Attempts}.",
                normalized,
                credential.FailedAttempts);

            return null;
        }

        credential.RegisterSuccessfulLogin();
        credential.User.RegisterLogin();
        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = await dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == credential.UserId && a.IsActive)
            .Select(a => a.Role)
            .ToListAsync(cancellationToken);

        logger.LogWarning(
            "Login local bem-sucedido de {Username} ({UserId}). Este caminho e de emergencia.",
            normalized,
            credential.UserId);

        return BuildToken(credential, roles);
    }

    private LocalLoginResult BuildToken(LocalCredential credential, IReadOnlyCollection<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.TokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, credential.User.ExternalSubject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("preferred_username", credential.Username),
            new("name", credential.User.DisplayName),
            new("email", credential.User.Email),
            new("tenant_id", credential.TenantId.ToString()),

            // Marca a forma de autenticacao: senha, nao SSO.
            new("amr", "pwd"),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LocalLoginResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            credential.User.DisplayName,
            roles.ToArray());
    }

    /*
     * Hash descartavel, com o mesmo custo de derivacao de um real, usado para
     * igualar o tempo de resposta quando o usuario nao existe.
     */
    private const string DummyHash =
        "pbkdf2$sha256$210000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
}

public sealed record LocalLoginResult(
    string AccessToken,
    DateTime ExpiresAt,
    string DisplayName,
    string[] Roles);
