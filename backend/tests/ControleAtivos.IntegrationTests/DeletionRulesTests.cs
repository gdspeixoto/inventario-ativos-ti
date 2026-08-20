using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;
using ControleAtivos.Persistence.Repositories;
using FluentAssertions;

namespace ControleAtivos.IntegrationTests;

/// <summary>
/// Regras de exclusao.
///
/// Excluir e desativar respondem a situacoes diferentes. Desativar registra que
/// algo real deixou de valer — um servidor desligado, um contrato encerrado — e
/// isso e informacao que precisa sobreviver. Excluir desfaz um cadastro que nao
/// deveria ter existido, tipicamente um erro de digitacao percebido logo em
/// seguida.
///
/// A verificacao precisa consultar o banco: as colecoes em memoria so estao
/// populadas quando o EF carrega o agregado, e uma checagem que erra em
/// silencio deixaria o usuario apagar dados com historico. Por isso estes
/// testes rodam contra o PostgreSQL de verdade.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class DeletionRulesTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Unidade_sem_dependencias_pode_ser_excluida()
    {
        var (tenantId, sufixo) = NewTenant();

        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));
        context.Tenants.Add(new Tenant(tenantId, $"tenant-{sufixo}", $"tenant-{sufixo}"));

        var unidade = new OrganizationalUnit(
            Guid.NewGuid(), tenantId, OrganizationalUnitType.Matriz, "CONTOSO", $"MAT-{sufixo}", null);

        context.OrganizationalUnits.Add(unidade);
        await context.SaveChangesAsync();

        var repositorio = new OrganizationalUnitRepository(context);
        var impedimentos = await repositorio.GetDeletionBlockersAsync(unidade.Id);

        impedimentos.Should().BeEmpty("nada aponta para ela: e um cadastro descartavel");
    }

    [Fact]
    public async Task Unidade_com_filha_nao_pode_ser_excluida()
    {
        var (tenantId, sufixo) = NewTenant();

        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));
        context.Tenants.Add(new Tenant(tenantId, $"tenant-{sufixo}", $"tenant-{sufixo}"));

        var matriz = new OrganizationalUnit(
            Guid.NewGuid(), tenantId, OrganizationalUnitType.Matriz, "CONTOSO", $"MAT-{sufixo}", null);

        var filha = new OrganizationalUnit(
            Guid.NewGuid(), tenantId, OrganizationalUnitType.Negocio, "Tecnologia", $"NEG-{sufixo}", matriz);

        context.OrganizationalUnits.AddRange(matriz, filha);
        await context.SaveChangesAsync();

        var repositorio = new OrganizationalUnitRepository(context);
        var impedimentos = await repositorio.GetDeletionBlockersAsync(matriz.Id);

        impedimentos.Should().ContainSingle();
        impedimentos[0].Should().Contain("subordinada", "a mensagem precisa dizer o que impede");
    }

    [Fact]
    public async Task Unidade_com_ativo_nao_pode_ser_excluida()
    {
        var (tenantId, sufixo) = NewTenant();

        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));
        context.Tenants.Add(new Tenant(tenantId, $"tenant-{sufixo}", $"tenant-{sufixo}"));

        var unidade = new OrganizationalUnit(
            Guid.NewGuid(), tenantId, OrganizationalUnitType.Matriz, "CONTOSO", $"MAT-{sufixo}", null);

        context.OrganizationalUnits.Add(unidade);

        context.Assets.Add(new LicenseAsset(
            Guid.NewGuid(),
            tenantId,
            unidade,
            "Microsoft 365",
            $"LIC-{sufixo}",
            "Microsoft",
            "Microsoft 365 E3",
            contractedQuantity: 10,
            unitPrice: new Money(100m)));

        await context.SaveChangesAsync();

        var repositorio = new OrganizationalUnitRepository(context);
        var impedimentos = await repositorio.GetDeletionBlockersAsync(unidade.Id);

        impedimentos.Should().ContainSingle();
        impedimentos[0].Should().Contain("ativo(s)");
        impedimentos[0].Should().Contain("1", "o numero orienta o que precisa ser movido antes");
    }

    [Fact]
    public async Task Impedimentos_sao_acumulados_para_orientar_o_usuario()
    {
        var (tenantId, sufixo) = NewTenant();

        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));
        context.Tenants.Add(new Tenant(tenantId, $"tenant-{sufixo}", $"tenant-{sufixo}"));

        var matriz = new OrganizationalUnit(
            Guid.NewGuid(), tenantId, OrganizationalUnitType.Matriz, "CONTOSO", $"MAT-{sufixo}", null);

        var filha = new OrganizationalUnit(
            Guid.NewGuid(), tenantId, OrganizationalUnitType.Negocio, "Tecnologia", $"NEG-{sufixo}", matriz);

        context.OrganizationalUnits.AddRange(matriz, filha);

        context.Assets.Add(new LicenseAsset(
            Guid.NewGuid(),
            tenantId,
            matriz,
            "Microsoft 365",
            $"LIC-{sufixo}",
            "Microsoft",
            "Microsoft 365 E3",
            contractedQuantity: 5,
            unitPrice: new Money(50m)));

        await context.SaveChangesAsync();

        var repositorio = new OrganizationalUnitRepository(context);
        var impedimentos = await repositorio.GetDeletionBlockersAsync(matriz.Id);

        // Mostrar tudo de uma vez evita o vaivem de corrigir um problema por
        // tentativa ate descobrir a lista inteira.
        impedimentos.Should().HaveCount(2);
    }

    [Fact]
    public async Task Ativo_sem_lancamentos_pode_ser_excluido()
    {
        var (tenantId, sufixo) = NewTenant();

        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));
        context.Tenants.Add(new Tenant(tenantId, $"tenant-{sufixo}", $"tenant-{sufixo}"));

        var unidade = new OrganizationalUnit(
            Guid.NewGuid(), tenantId, OrganizationalUnitType.Matriz, "CONTOSO", $"MAT-{sufixo}", null);

        context.OrganizationalUnits.Add(unidade);

        var licenca = new LicenseAsset(
            Guid.NewGuid(),
            tenantId,
            unidade,
            "Licenca Errada",
            $"LIC-{sufixo}",
            "Microsoft",
            "Produto",
            contractedQuantity: 1,
            unitPrice: new Money(10m));

        context.Assets.Add(licenca);
        await context.SaveChangesAsync();

        var repositorio = new AssetRepository(context);

        licenca.CanBeDeleted.Should().BeTrue();
        (await repositorio.HasCostEntriesAsync(licenca.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Correcao_cadastral_persiste_e_preserva_o_valor()
    {
        var (tenantId, sufixo) = NewTenant();
        Guid licencaId;

        await using (var context = fixture.CreateContext(new TestTenantContext(tenantId)))
        {
            context.Tenants.Add(new Tenant(tenantId, $"tenant-{sufixo}", $"tenant-{sufixo}"));

            var unidade = new OrganizationalUnit(
                Guid.NewGuid(), tenantId, OrganizationalUnitType.Matriz, "CONTOSO", $"MAT-{sufixo}", null);

            context.OrganizationalUnits.Add(unidade);

            var licenca = new LicenseAsset(
                Guid.NewGuid(),
                tenantId,
                unidade,
                "Nome Digitado Errado",
                $"LIC-{sufixo}",
                "Microsft",
                "Microsoft 365",
                contractedQuantity: 10,
                unitPrice: new Money(100m));

            licencaId = licenca.Id;
            context.Assets.Add(licenca);
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext(new TestTenantContext(tenantId)))
        {
            var repositorio = new AssetRepository(context);
            var licenca = await repositorio.GetLicenseAsync(licencaId);

            licenca!.Rename("Microsoft 365 E3", $"LIC-{sufixo}");
            licenca.CorrectProduct("Microsoft", "Microsoft 365 E3");

            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext(new TestTenantContext(tenantId)))
        {
            var licenca = await new AssetRepository(context).GetLicenseAsync(licencaId);

            licenca!.Name.Should().Be("Microsoft 365 E3");
            licenca.Manufacturer.Should().Be("Microsoft");
            licenca.MonthlyAmount.Amount.Should().Be(1_000m, "corrigir cadastro nao mexe em dinheiro");
            licenca.Status.Should().Be(AssetStatus.Ativo, "nem na situacao");
        }
    }

    private static (Guid TenantId, string Suffix) NewTenant() =>
        (Guid.NewGuid(), Guid.NewGuid().ToString("N")[..8].ToUpperInvariant());
}
