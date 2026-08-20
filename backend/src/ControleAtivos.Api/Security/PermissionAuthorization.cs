using ControleAtivos.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace ControleAtivos.Api.Security;

/// <summary>Exige uma permissao nomeada do catalogo <see cref="Permissions"/>.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionHandler(ICurrentUser currentUser)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (currentUser.IsAuthenticated && currentUser.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Registra uma policy por permissao. Cada endpoint declara a permissao exigida
/// via <c>RequireAuthorization(Permissions.X)</c>, o que torna a omissao
/// visivel na leitura da rota.
/// </summary>
public static class AuthorizationRegistration
{
    public static IServiceCollection AddControleAtivosAuthorization(this IServiceCollection services)
    {
        /*
         * Scoped, e nao singleton: o handler depende de ICurrentUser, que e
         * resolvido por requisicao. Registra-lo como singleton capturaria o
         * usuario da primeira requisicao para todas as demais.
         */
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new PermissionRequirement(permission));
                });
            }
        });

        return services;
    }
}
