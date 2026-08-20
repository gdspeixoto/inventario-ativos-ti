using ControleAtivos.Application.Abstractions;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.IntegrationTests;

/// <summary>
/// Conecta a bateria de testes a um PostgreSQL real.
///
/// Usar o banco de verdade, e nao o provider InMemory, e essencial aqui: os
/// filtros globais de tenant, os indices filtrados e as projecoes de
/// <c>ComplexProperty</c> se comportam de forma diferente — um teste que passa
/// em memoria pode falhar em producao.
///
/// Por padrao aponta para o container de desenvolvimento
/// (<c>infra/docker-compose.yml</c>), em um database separado. Em CI, defina
/// <c>CONTROLE_ATIVOS_TEST_CONNECTION</c> apontando para o servico do pipeline.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DefaultConnection =
        "Host=localhost;Port=5433;Database=controle_ativos_tests;" +
        "Username=controle_ativos_app;Password=1c46e8b7c589e6f1c81b2db32b732b7a3d90f5c231dc2c6e";

    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("CONTROLE_ATIVOS_TEST_CONNECTION") ?? DefaultConnection;

    public async Task InitializeAsync()
    {
        await using var context = CreateContext(new TestTenantContext(Guid.Empty, bypass: true));

        // O schema e criado uma vez; cada teste trabalha com dados proprios,
        // identificados por sufixo aleatorio, e nao interfere nos demais.
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public ControleAtivosDbContext CreateContext(
        ITenantContext tenantContext,
        ICurrentUser? currentUser = null)
    {
        var options = new DbContextOptionsBuilder<ControleAtivosDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "controle_ativos"))
            .Options;

        return new ControleAtivosDbContext(
            options,
            tenantContext,
            currentUser ?? new TestCurrentUser());
    }
}

[CollectionDefinition(nameof(DatabaseCollection))]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
