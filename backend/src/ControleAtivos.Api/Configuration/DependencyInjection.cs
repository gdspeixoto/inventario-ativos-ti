using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using ControleAtivos.Api.Security;
using ControleAtivos.Application;
using ControleAtivos.Application.Abstractions;
using ControleAtivos.Persistence;
using ControleAtivos.Persistence.Security;
using ControleAtivos.Persistence.Storage;
using ControleAtivos.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ControleAtivos.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddControleAtivos(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddPersistence(configuration);
        services.AddSecurity(configuration, environment);
        services.AddRateLimiting();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ControleAtivosDbContext>());

        services.Configure<DocumentStorageOptions>(
            configuration.GetSection(DocumentStorageOptions.SectionName));

        services.AddPersistenceServices();
        services.AddApplicationServices();

        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' nao configurada. " +
                "Defina via variavel de ambiente ConnectionStrings__Postgres ou user-secrets.");

        services.AddScoped<TenantSessionInterceptor>();

        services.AddDbContext<ControleAtivosDbContext>((provider, options) =>
        {
            // Define app.current_tenant a cada conexao aberta, para que as
            // politicas de RLS do PostgreSQL saibam de quem sao as linhas.
            options.AddInterceptors(provider.GetRequiredService<TenantSessionInterceptor>());

            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "controle_ativos");
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });

            // Nunca habilitar em producao: expoe parametros com dados sensiveis no log.
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        return services;
    }

    /// <summary>Scheme que roteia o token para o validador do emissor correto.</summary>
    public const string CompositeScheme = "ControleAtivos";

    /// <summary>Scheme dos tokens emitidos pelo login local de emergencia.</summary>
    public const string LocalScheme = "ControleAtivosLocal";

    /// <summary>
    /// Decide se o login local sobe, aplicando as duas travas: a chave precisa
    /// ser suficientemente longa e, em producao, a liberacao tem de ser
    /// explicita. Uma configuracao copiada do ambiente de desenvolvimento nao
    /// deve abrir o caminho sem que alguem tenha decidido isso.
    /// </summary>
    private static bool ResolveLocalLoginEnabled(LocalLoginOptions options, IHostEnvironment environment)
    {
        if (!options.Enabled)
        {
            return false;
        }

        if (environment.IsProduction() && !options.AllowedInProduction)
        {
            return false;
        }

        // HMAC-SHA256 com chave menor que o tamanho do bloco enfraquece a
        // assinatura; melhor nao subir o caminho do que subi-lo mal.
        return Encoding.UTF8.GetByteCount(options.SigningKey) >= 32;
    }

    private static string? ExtractBearerToken(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;
    }

    /// <summary>
    /// Le o emissor declarado no token apenas para escolher o validador. O
    /// conteudo ainda nao e confiavel neste ponto: a assinatura sera conferida
    /// pelo scheme escolhido, e um token que mente sobre o emissor apenas
    /// falha na validacao seguinte.
    /// </summary>
    private static bool IsLocallyIssued(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
        {
            return false;
        }

        try
        {
            return handler.ReadJwtToken(token).Issuer == LocalAuthenticationService.Issuer;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services
            .AddOptions<KeycloakOptions>()
            .Bind(configuration.GetSection(KeycloakOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var keycloak = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
            ?? new KeycloakOptions();

        services
            .AddOptions<AccessOptions>()
            .Bind(configuration.GetSection(AccessOptions.SectionName));

        var access = configuration.GetSection(AccessOptions.SectionName).Get<AccessOptions>()
            ?? new AccessOptions();

        var localLoginEnabled = ResolveLocalLoginEnabled(access.LocalLogin, environment);

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<LocalAuthenticationService>();

        var authentication = services.AddAuthentication(options =>
        {
            /*
             * Com o login local ativo existem dois emissores validos: o
             * Keycloak e o proprio sistema. Um policy scheme escolhe o
             * validador certo lendo o `iss` do token, em vez de afrouxar a
             * validacao de um deles para aceitar os dois.
             */
            options.DefaultScheme = localLoginEnabled
                ? CompositeScheme
                : JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme = options.DefaultScheme;
        });

        if (localLoginEnabled)
        {
            authentication.AddPolicyScheme(CompositeScheme, CompositeScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var token = ExtractBearerToken(context);

                    return IsLocallyIssued(token)
                        ? LocalScheme
                        : JwtBearerDefaults.AuthenticationScheme;
                };
            });

            authentication.AddJwtBearer(LocalScheme, options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = LocalAuthenticationService.Issuer,
                    ValidateAudience = true,
                    ValidAudience = LocalAuthenticationService.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(access.LocalLogin.SigningKey)),

                    // O token local e HMAC; aceitar outro algoritmo aqui abriria
                    // caminho para confusao de algoritmo.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                };
            });
        }

        authentication
            .AddJwtBearer(options =>
            {
                options.Authority = keycloak.Authority;
                options.Audience = keycloak.Audience;
                options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = keycloak.Authority,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role,
                };

                options.Events = new JwtBearerEvents
                {
                    /*
                     * O Keycloak entrega papeis aninhados em realm_access.roles e
                     * resource_access.<client>.roles. O pipeline de autorizacao do
                     * ASP.NET Core so enxerga claims planas, entao a projecao e
                     * feita aqui, uma unica vez por token.
                     */
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is not ClaimsIdentity identity)
                        {
                            return Task.CompletedTask;
                        }

                        foreach (var role in ExtractRoles(context.Principal, keycloak.ClientId))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        }

                        return Task.CompletedTask;
                    },

                    // Nao revelar o motivo exato da falha ao cliente.
                    OnChallenge = context =>
                    {
                        context.Response.Headers.Remove("WWW-Authenticate");
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddControleAtivosAuthorization();

        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? new CorsOptions();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (corsOptions.AllowedOrigins.Length == 0)
                {
                    // Sem origem configurada, nenhuma origem cruzada e aceita.
                    policy.WithOrigins("https://localhost");
                    return;
                }

                policy
                    .WithOrigins(corsOptions.AllowedOrigins)
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                    .WithHeaders("Content-Type", "Authorization", TenantHeaderName)
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }

    private const string TenantHeaderName = "X-Tenant-Id";

    private static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            /*
             * Particiona por usuario autenticado e, na ausencia dele, por IP.
             * Sem a particao por usuario, varios usuarios atras do mesmo NAT
             * corporativo compartilhariam a mesma cota.
             */
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var key =
                    context.User.FindFirst("sub")?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonimo";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
            });

            // Escrita tem cota propria, mais restritiva.
            options.AddPolicy("writes", context =>
            {
                var key =
                    context.User.FindFirst("sub")?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonimo";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }

    private static IEnumerable<string> ExtractRoles(ClaimsPrincipal principal, string clientId)
    {
        var roles = new List<string>();

        AppendRolesFrom(principal.FindFirst("realm_access")?.Value, null, roles);
        AppendRolesFrom(principal.FindFirst("resource_access")?.Value, clientId, roles);

        return roles.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void AppendRolesFrom(string? json, string? clientId, List<string> destination)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (clientId is not null)
            {
                if (!root.TryGetProperty(clientId, out var client))
                {
                    return;
                }

                root = client;
            }

            if (!root.TryGetProperty("roles", out var rolesElement) ||
                rolesElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            destination.AddRange(
                rolesElement
                    .EnumerateArray()
                    .Select(element => element.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))!);
        }
        catch (JsonException)
        {
            // Token com formato inesperado nao concede papel algum.
        }
    }
}
