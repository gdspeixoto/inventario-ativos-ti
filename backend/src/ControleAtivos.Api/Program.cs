using ControleAtivos.Api.Configuration;
using ControleAtivos.Api.Middleware;
using ControleAtivos.Api.Security;
using ControleAtivos.Application.Abstractions;
using ControleAtivos.Persistence.Context;
using ControleAtivos.Persistence.Seed;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Limita o corpo da requisicao: uploads passam por endpoint dedicado.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2 * 1024 * 1024;
    options.AddServerHeader = false;
});

builder.Services.AddControleAtivos(builder.Configuration, builder.Environment);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // camelCase no contrato HTTP; enums como texto para o payload ser legivel.
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Postgres")!,
        name: "postgres",
        tags: ["ready"]);

var app = builder.Build();

// Confia apenas nos headers de proxy do Traefik institucional.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
        exception is not null || httpContext.Response.StatusCode >= 500
            ? Serilog.Events.LogEventLevel.Error
            : Serilog.Events.LogEventLevel.Information;
});

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
}).AllowAnonymous();

app.MapControllers();

// Aplica migrations pendentes em desenvolvimento; em producao o deploy e explicito.
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue("Database:AutoMigrate", false))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ControleAtivosDbContext>();
    await dbContext.Database.MigrateAsync();

    // Dados de demonstracao. Nao roda em producao e e idempotente.
    if (app.Configuration.GetValue("Database:SeedDemoData", false))
    {
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using (tenantContext.BypassTenantFilter())
        {
            await DevelopmentSeeder.SeedAsync(dbContext);
        }
    }
}

/*
 * Credenciais locais de emergencia sao provisionadas fora do bloco acima: elas
 * precisam existir tambem em producao, que e justamente onde a indisponibilidade
 * do SSO doi. O seeder e idempotente e nao faz nada sem configuracao.
 */
{
    using var scope = app.Services.CreateScope();

    var access = scope.ServiceProvider
        .GetRequiredService<IOptions<AccessOptions>>().Value;

    if (access.LocalLogin.SeedCredentials.Length > 0)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ControleAtivosDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("LocalCredentialSeeder");

        using (tenantContext.BypassTenantFilter())
        {
            await LocalCredentialSeeder.SeedAsync(dbContext, hasher, access, logger);
        }
    }
}

await app.RunAsync();

/// <summary>Exposto para os testes de integracao via <c>WebApplicationFactory</c>.</summary>
public partial class Program;
