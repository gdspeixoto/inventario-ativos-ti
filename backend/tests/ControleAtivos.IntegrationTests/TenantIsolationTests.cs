using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.IntegrationTests;

/// <summary>
/// Isolamento entre tenants. E a garantia mais importante do sistema: uma falha
/// aqui expoe dados de uma organizacao a outra.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class TenantIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Consulta_nao_retorna_ativos_de_outro_tenant()
    {
        var (tenantA, tenantB) = await SeedTwoTenantsAsync();

        await using var contextA = fixture.CreateContext(new TestTenantContext(tenantA));
        var licencasDoTenantA = await contextA.Licenses.ToListAsync();

        licencasDoTenantA.Should().HaveCount(1);
        licencasDoTenantA[0].TenantId.Should().Be(tenantA);

        await using var contextB = fixture.CreateContext(new TestTenantContext(tenantB));
        var licencasDoTenantB = await contextB.Licenses.ToListAsync();

        licencasDoTenantB.Should().HaveCount(1);
        licencasDoTenantB[0].TenantId.Should().Be(tenantB);
    }

    [Fact]
    public async Task Busca_por_id_de_ativo_de_outro_tenant_retorna_nulo()
    {
        var (tenantA, tenantB) = await SeedTwoTenantsAsync();

        await using var contextB = fixture.CreateContext(new TestTenantContext(tenantB));
        var idDoTenantA = await contextB.Licenses
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantA)
            .Select(l => l.Id)
            .FirstAsync();

        // Mesmo conhecendo o id, o tenant B nao alcanca o registro do tenant A.
        var encontrado = await contextB.Licenses.FirstOrDefaultAsync(l => l.Id == idDoTenantA);

        encontrado.Should().BeNull();
    }

    [Fact]
    public async Task Contagem_respeita_o_tenant_corrente()
    {
        var (tenantA, _) = await SeedTwoTenantsAsync();

        await using var contextA = fixture.CreateContext(new TestTenantContext(tenantA));
        await using var contextGlobal = fixture.CreateContext(
            new TestTenantContext(Guid.Empty, bypass: true));

        var doTenant = await contextA.Assets.CountAsync();
        var total = await contextGlobal.Assets.CountAsync();

        doTenant.Should().BeLessThan(total, "existem ativos de outros tenants na base");
    }

    [Fact]
    public async Task Timeline_de_um_tenant_nao_vaza_para_o_outro()
    {
        var (tenantA, tenantB) = await SeedTwoTenantsAsync();

        await using var contextA = fixture.CreateContext(new TestTenantContext(tenantA));
        await using var contextB = fixture.CreateContext(new TestTenantContext(tenantB));

        var eventosA = await contextA.TimelineEvents.ToListAsync();
        var eventosB = await contextB.TimelineEvents.ToListAsync();

        eventosA.Should().NotBeEmpty();
        eventosB.Should().NotBeEmpty();
        eventosA.Should().OnlyContain(e => e.TenantId == tenantA);
        eventosB.Should().OnlyContain(e => e.TenantId == tenantB);
    }

    /// <summary>Cria dois tenants, cada um com uma unidade e uma licenca.</summary>
    private async Task<(Guid TenantA, Guid TenantB)> SeedTwoTenantsAsync()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await SeedTenantAsync(tenantA, "tenant-a");
        await SeedTenantAsync(tenantB, "tenant-b");

        return (tenantA, tenantB);
    }

    private async Task SeedTenantAsync(Guid tenantId, string slug)
    {
        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));

        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        context.Tenants.Add(new Tenant(tenantId, slug, $"{slug}-{suffix}"));

        var unit = new OrganizationalUnit(
            Guid.NewGuid(),
            tenantId,
            OrganizationalUnitType.Matriz,
            "CONTOSO",
            $"MAT-{suffix}",
            null);

        context.OrganizationalUnits.Add(unit);

        context.Assets.Add(new LicenseAsset(
            Guid.NewGuid(),
            tenantId,
            unit,
            "Microsoft 365 Business Premium",
            $"LIC-{suffix}",
            "Microsoft",
            "Microsoft 365 Business Premium",
            10,
            new Money(105m)));

        await context.SaveChangesAsync();
    }
}
