using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;
using ControleAtivos.Persistence.Queries;
using FluentAssertions;

namespace ControleAtivos.IntegrationTests;

/// <summary>
/// O resumo de custos agrega valores monetarios agrupando por fornecedor,
/// unidade e categoria. Como <c>Money</c> e um complex type, uma agregacao
/// escrita de forma ingenua compila normalmente e so quebra em execucao, com
/// 500 — foi assim que o defeito passou despercebido ate alguem conseguir
/// autenticar e abrir a tela. Este teste executa a consulta de verdade contra
/// o banco, que e a unica forma de detectar falha de traducao LINQ.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class CostSummaryQueryTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Resumo_de_custos_e_traduzido_para_SQL_e_agrega_por_grupo()
    {
        var env = await BuildEnvironmentAsync();

        await using var context = fixture.CreateContext(new TestTenantContext(env.TenantId));
        var queries = new CostQueries(context);

        var resumo = await queries.GetSummaryAsync(10m, OrganizationalScope.Global());

        resumo.CurrentMonthlyCost.Should().Be(3_000m, "1.000 + 2.000 dos dois ativos ativos");
        resumo.ProjectedAnnualCost.Should().Be(36_000m);

        resumo.BySupplier.Should().ContainSingle();
        resumo.BySupplier[0].Label.Should().Be(env.SupplierName);
        resumo.BySupplier[0].MonthlyAmount.Should().Be(3_000m);
        resumo.BySupplier[0].ItemCount.Should().Be(2);

        resumo.ByOrganizationalUnit.Should().ContainSingle();
        resumo.ByOrganizationalUnit[0].MonthlyAmount.Should().Be(3_000m);

        resumo.ByCategory.Should().ContainSingle(g => g.Label == nameof(AssetKind.License));
        resumo.ByCategory.Single(g => g.Label == nameof(AssetKind.License))
            .MonthlyAmount.Should().Be(3_000m);
    }

    private async Task<TestEnvironment> BuildEnvironmentAsync()
    {
        var tenantId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));

        context.Tenants.Add(new Tenant(tenantId, $"tenant-{suffix}", $"tenant-{suffix}"));

        var unidade = new OrganizationalUnit(
            Guid.NewGuid(), tenantId, OrganizationalUnitType.Matriz, "CONTOSO", $"MAT-{suffix}", null);

        context.OrganizationalUnits.Add(unidade);

        var fornecedor = new Supplier(Guid.NewGuid(), tenantId, $"Fornecedor {suffix}");
        context.Suppliers.Add(fornecedor);

        foreach (var valor in new[] { 1_000m, 2_000m })
        {
            var licenca = new LicenseAsset(
                Guid.NewGuid(),
                tenantId,
                unidade,
                $"Licenca {valor}",
                $"LIC-{suffix}-{valor}",
                "Microsoft",
                "Microsoft 365",
                contractedQuantity: 10,
                unitPrice: new Money(valor / 10m));

            licenca.LinkToSupplier(fornecedor);
            context.Assets.Add(licenca);
        }

        await context.SaveChangesAsync();

        return new TestEnvironment(tenantId, fornecedor.Name);
    }

    private sealed record TestEnvironment(Guid TenantId, string SupplierName);
}
