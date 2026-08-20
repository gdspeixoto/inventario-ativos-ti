using ControleAtivos.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ControleAtivos.Persistence.Context;

/// <summary>
/// Usado apenas pelo <c>dotnet ef</c> ao gerar migrations. Fora do processo web
/// nao existe requisicao, logo nao existe tenant nem usuario: as implementacoes
/// abaixo sao inertes de proposito.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ControleAtivosDbContext>
{
    public ControleAtivosDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CONTROLE_ATIVOS_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=controle_ativos;Username=controle_ativos_app;Password=design-time";

        var options = new DbContextOptionsBuilder<ControleAtivosDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "controle_ativos"))
            .Options;

        return new ControleAtivosDbContext(options, new DesignTimeTenantContext(), new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public string? TenantSlug => null;

        public bool HasTenant => false;

        public bool IsTenantFilterBypassed => true;

        public IDisposable BypassTenantFilter() => new NoopScope();

        private sealed class NoopScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;

        public string? ExternalSubject => null;

        public string? DisplayName => null;

        public string? Email => null;

        public bool IsAuthenticated => false;

        public IReadOnlySet<string> Roles { get; } = new HashSet<string>();

        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();

        public bool HasPermission(string permission) => false;

        public bool HasRole(string role) => false;
    }
}
