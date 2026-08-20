using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ControleAtivos.Api.Configuration;
using ControleAtivos.Api.Security;
using ControleAtivos.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ControleAtivos.Api.Controllers;

/// <summary>
/// Autenticacao por credencial local de emergencia.
///
/// O caminho normal de entrada e o SSO corporativo, que nao passa por aqui:
/// o frontend fala direto com o Keycloak. Estes endpoints existem para o caso
/// de o provedor estar indisponivel.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(
    LocalAuthenticationService localAuthentication,
    IOptions<AccessOptions> accessOptions,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost("local/login")]
    [AllowAnonymous]
    [EnableRateLimiting("writes")]
    [ProducesResponseType<LocalLoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocalLoginResponse>> LocalLogin(
        [FromBody] LocalLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLocalLoginAvailable())
        {
            // 404, e nao 403: um caminho desligado nao precisa anunciar que existe.
            return NotFound();
        }

        var result = await localAuthentication.AuthenticateAsync(
            request.Username,
            request.Password,
            cancellationToken);

        if (result is null)
        {
            // Mensagem unica para credencial invalida, conta bloqueada ou
            // expirada: distinguir os casos ajudaria a enumerar usuarios.
            return Unauthorized(new
            {
                title = "Credenciais invalidas",
                detail = "Usuario ou senha incorretos.",
                status = StatusCodes.Status401Unauthorized,
            });
        }

        return Ok(new LocalLoginResponse(
            result.AccessToken,
            "Bearer",
            (int)Math.Max(0, (result.ExpiresAt - DateTime.UtcNow).TotalSeconds),
            result.DisplayName,
            result.Roles));
    }

    /// <summary>
    /// Informa ao frontend se deve exibir o formulario de senha. Publico de
    /// proposito: e a primeira chamada da tela de login, antes de haver token.
    /// </summary>
    [HttpGet("local/available")]
    [AllowAnonymous]
    [ProducesResponseType<LocalLoginAvailabilityResponse>(StatusCodes.Status200OK)]
    public ActionResult<LocalLoginAvailabilityResponse> LocalLoginAvailability() =>
        Ok(new LocalLoginAvailabilityResponse(IsLocalLoginAvailable()));

    /// <summary>
    /// Identidade e permissoes efetivas do chamador, ja combinando o que veio
    /// do token com o que foi concedido dentro do sistema. O frontend usa isto
    /// para decidir o que mostrar — a decisao de acesso continua no servidor.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> Me([FromServices] ICurrentUser currentUser) =>
        Ok(new CurrentUserResponse(
            currentUser.UserId,
            currentUser.DisplayName,
            currentUser.Email,
            [.. currentUser.Roles],
            [.. currentUser.Permissions]));

    private bool IsLocalLoginAvailable()
    {
        var options = accessOptions.Value.LocalLogin;

        if (!options.Enabled)
        {
            return false;
        }

        if (environment.IsProduction() && !options.AllowedInProduction)
        {
            return false;
        }

        return System.Text.Encoding.UTF8.GetByteCount(options.SigningKey) >= 32;
    }
}

public sealed class LocalLoginRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Resposta do login local.
///
/// Os nomes seguem o formato de um token endpoint OIDC (<c>access_token</c>,
/// <c>expires_in</c>) porque o cliente ja sabe consumir esse contrato — manter
/// um formato proprio obrigaria o frontend a tratar este caso como excecao.
/// </summary>
public sealed record LocalLoginResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("roles")] string[] Roles);

public sealed record LocalLoginAvailabilityResponse(bool Enabled);

public sealed record CurrentUserResponse(
    Guid? UserId,
    string? DisplayName,
    string? Email,
    string[] Roles,
    string[] Permissions);
