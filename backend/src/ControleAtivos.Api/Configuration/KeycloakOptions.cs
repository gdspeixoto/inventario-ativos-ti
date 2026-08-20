using System.ComponentModel.DataAnnotations;

namespace ControleAtivos.Api.Configuration;

/// <summary>Parametros do realm institucional usado para autenticar a API.</summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    [Required]
    public string Authority { get; init; } = string.Empty;

    /// <summary>Audiencia esperada no token. Tokens de outro client sao rejeitados.</summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>Client id usado para ler papeis em <c>resource_access</c>.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Somente para desenvolvimento com Keycloak sem TLS valido.</summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Origens autorizadas. Wildcard nao e aceito.</summary>
    public string[] AllowedOrigins { get; init; } = [];
}
