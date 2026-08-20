using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Licenses;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;
using ControleAtivos.Persistence.Context;
using ControleAtivos.Persistence.Queries;
using ControleAtivos.Persistence.Repositories;
using ControleAtivos.Persistence.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.IntegrationTests;

/// <summary>
/// Fluxo completo de licencas contra o banco real: cadastro, reajuste,
/// alteracao de quantidade e a timeline gerada automaticamente.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class LicenseWorkflowTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Reajuste_persiste_historico_e_timeline_na_mesma_transacao()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildLicenseService(env);

        var licenseId = await service.CreateAsync(new CreateLicenseCommand
        {
            Name = "Microsoft 365 Business Premium",
            Code = $"LIC-{env.Suffix}",
            Manufacturer = "Microsoft",
            Product = "Microsoft 365 Business Premium",
            OrganizationalUnitId = env.UnitId,
            ContractedQuantity = 120,
            UnitPrice = 92m,
        });

        var resultado = await service.ApplyPriceAdjustmentAsync(licenseId, new ApplyPriceAdjustmentCommand
        {
            NewMonthlyAmount = 12_600m,
            EffectiveDate = new DateOnly(2026, 3, 15),
            Reason = "Reajuste anual do fornecedor apos negociacao comercial",
            IndexApplied = AdjustmentIndex.NegociacaoManual,
        });

        resultado.PreviousAmount.Should().Be(11_040m);
        resultado.NewAmount.Should().Be(12_600m);
        resultado.AbsoluteDifference.Should().Be(1_560m);
        resultado.AnnualImpact.Should().Be(18_720m);
        resultado.ExceedsAlertThreshold.Should().BeTrue("14,13% ultrapassa o limite de 10%");

        await using var verificacao = fixture.CreateContext(new TestTenantContext(env.TenantId));

        var historico = await verificacao.PriceAdjustments
            .Where(p => p.TargetId == licenseId)
            .ToListAsync();

        historico.Should().HaveCount(1);
        historico[0].PreviousAmount.Amount.Should().Be(11_040m);

        var eventos = await verificacao.TimelineEvents
            .Where(e => e.EntityId == licenseId)
            .ToListAsync();

        eventos.Should().Contain(e => e.EventType == TimelineEventType.Criacao);
        eventos.Should().Contain(e => e.EventType == TimelineEventType.ReajusteAplicado);

        var reajuste = eventos.Single(e => e.EventType == TimelineEventType.ReajusteAplicado);
        reajuste.PreviousAmount.Should().Be(11_040m);
        reajuste.NewAmount.Should().Be(12_600m);
        reajuste.UserId.Should().Be(env.UserId, "a autoria do evento vem do usuario da requisicao");
    }

    [Fact]
    public async Task Alteracao_de_quantidade_recalcula_custo_e_registra_impacto()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildLicenseService(env);

        var licenseId = await service.CreateAsync(new CreateLicenseCommand
        {
            Name = "Exchange Online Plan 2",
            Code = $"LIC-{env.Suffix}",
            Manufacturer = "Microsoft",
            Product = "Exchange Online Plan 2",
            OrganizationalUnitId = env.UnitId,
            ContractedQuantity = 80,
            UnitPrice = 48m,
        });

        var resultado = await service.ChangeQuantityAsync(licenseId, new ChangeLicenseQuantityCommand
        {
            NewQuantity = 100,
            Reason = "Contratacao de 20 novas licencas",
        });

        resultado.PreviousMonthlyAmount.Should().Be(3_840m);
        resultado.NewMonthlyAmount.Should().Be(4_800m);
        resultado.MonthlyImpact.Should().Be(960m);
        resultado.AnnualImpact.Should().Be(11_520m);

        await using var verificacao = fixture.CreateContext(new TestTenantContext(env.TenantId));
        var licenca = await verificacao.Licenses.SingleAsync(l => l.Id == licenseId);

        licenca.ContractedQuantity.Should().Be(100);
        licenca.MonthlyAmount.Amount.Should().Be(4_800m);
    }

    [Fact]
    public async Task Codigo_duplicado_e_rejeitado()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildLicenseService(env);

        var command = new CreateLicenseCommand
        {
            Name = "Power BI Pro",
            Code = $"LIC-{env.Suffix}",
            Manufacturer = "Microsoft",
            Product = "Power BI Pro",
            OrganizationalUnitId = env.UnitId,
            ContractedQuantity = 45,
            UnitPrice = 59.90m,
        };

        await service.CreateAsync(command);

        var act = async () => await service.CreateAsync(command);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Listagem_projeta_utilizacao_e_valores_derivados()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildLicenseService(env);

        var licenseId = await service.CreateAsync(new CreateLicenseCommand
        {
            Name = "Microsoft 365 E3",
            Code = $"LIC-{env.Suffix}",
            Manufacturer = "Microsoft",
            Product = "Microsoft 365 E3",
            OrganizationalUnitId = env.UnitId,
            ContractedQuantity = 35,
            UnitPrice = 185m,
        });

        await service.UpdateUsageAsync(licenseId, new UpdateLicenseUsageCommand { UsedQuantity = 32 });

        var pagina = await service.ListAsync(new ListLicensesQuery { PageSize = 50 });
        var item = pagina.Items.Single(l => l.Id == licenseId);

        item.AvailableQuantity.Should().Be(3);
        item.UtilizationRate.Should().BeApproximately(91.43m, 0.01m);
        item.MonthlyAmount.Should().Be(6_475m);
        item.AnnualAmount.Should().Be(77_700m);
    }

    /// <summary>
    /// Um usuario com escopo restrito a um nucleo nao deve enxergar ativos de
    /// outra area — mesmo pertencendo ao mesmo tenant.
    /// </summary>
    [Fact]
    public async Task Usuario_fora_do_escopo_nao_enxerga_o_ativo()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildLicenseService(env);

        var licenseId = await service.CreateAsync(new CreateLicenseCommand
        {
            Name = "Project Plan 3",
            Code = $"LIC-{env.Suffix}",
            Manufacturer = "Microsoft",
            Product = "Project Plan 3",
            OrganizationalUnitId = env.UnitId,
            ContractedQuantity = 12,
            UnitPrice = 165m,
        });

        // Outra area do mesmo tenant, com um gerente proprio.
        var (outraAreaId, outroUsuarioId) = await SeedIsolatedBranchAsync(env);

        var servicoDoOutro = BuildLicenseService(env with
        {
            UserId = outroUsuarioId,
            UnitId = outraAreaId,
        });

        var visiveis = await servicoDoOutro.ListAsync(new ListLicensesQuery { PageSize = 50 });
        visiveis.Items.Should().NotContain(l => l.Id == licenseId);

        var act = async () => await servicoDoOutro.GetAsync(licenseId);
        await act.Should().ThrowAsync<NotFoundException>(
            "itens fora do escopo respondem 404 para nao revelar sua existencia");
    }

    private LicenseService BuildLicenseService(TestEnvironment env)
    {
        var currentUser = new TestCurrentUser(
            env.UserId,
            "Ana Souza",
            Permissions.AssetsRead,
            Permissions.AssetsWrite,
            Permissions.CostsWrite,
            Permissions.AssetsApproveFinancialChange);

        var context = fixture.CreateContext(new TestTenantContext(env.TenantId), currentUser);

        var scopeResolver = new OrganizationalScopeResolver(context, currentUser);

        return new LicenseService(
            new AssetQueries(context),
            new AssetRepository(context),
            new OrganizationalUnitRepository(context),
            new SupplierRepository(context),
            new ContractRepository(context),
            new CostCenterRepository(context),
            new UserRepository(context),
            new PriceAdjustmentRepository(context),
            scopeResolver,
            currentUser,
            context);
    }

    /// <summary>
    /// Monta um tenant com matriz, uma area e um gerente atribuido a ela.
    /// O escopo do gerente cobre a area e seus descendentes.
    /// </summary>
    private async Task<TestEnvironment> BuildEnvironmentAsync()
    {
        var tenantId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));

        context.Tenants.Add(new Tenant(tenantId, $"tenant-{suffix}", $"tenant-{suffix}"));

        var matriz = new OrganizationalUnit(
            Guid.NewGuid(),
            tenantId,
            OrganizationalUnitType.Matriz,
            "CONTOSO",
            $"MAT-{suffix}",
            null);

        var negocio = new OrganizationalUnit(
            Guid.NewGuid(),
            tenantId,
            OrganizationalUnitType.Negocio,
            "Negocios de Tecnologia",
            $"NEG-{suffix}",
            matriz);

        var area = new OrganizationalUnit(
            Guid.NewGuid(),
            tenantId,
            OrganizationalUnitType.AreaTecnologica,
            "Transformacao Digital",
            $"AT-{suffix}",
            negocio);

        context.OrganizationalUnits.AddRange(matriz, negocio, area);

        var usuario = new AppUser(
            Guid.NewGuid(),
            tenantId,
            $"sub-{suffix}",
            "Ana Souza",
            $"ana.{suffix.ToLowerInvariant()}@example.com");

        context.Users.Add(usuario);

        context.ManagementAssignments.Add(new ManagementAssignment(
            Guid.NewGuid(),
            tenantId,
            usuario,
            area,
            ManagementRole.Gerente,
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-6),
            isPrimary: true));

        await context.SaveChangesAsync();

        return new TestEnvironment(tenantId, area.Id, usuario.Id, suffix);
    }

    private async Task<(Guid UnitId, Guid UserId)> SeedIsolatedBranchAsync(TestEnvironment env)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using var context = fixture.CreateContext(new TestTenantContext(env.TenantId));

        var matriz = await context.OrganizationalUnits
            .FirstAsync(u => u.Type == OrganizationalUnitType.Matriz && u.Id != env.UnitId
                || u.Type == OrganizationalUnitType.Matriz);

        var outraArea = new OrganizationalUnit(
            Guid.NewGuid(),
            env.TenantId,
            OrganizationalUnitType.Negocio,
            "Outro Negocio",
            $"NEG2-{suffix}",
            matriz);

        context.OrganizationalUnits.Add(outraArea);

        var usuario = new AppUser(
            Guid.NewGuid(),
            env.TenantId,
            $"sub2-{suffix}",
            "Carlos Lima",
            $"carlos.{suffix.ToLowerInvariant()}@example.com");

        context.Users.Add(usuario);

        context.ManagementAssignments.Add(new ManagementAssignment(
            Guid.NewGuid(),
            env.TenantId,
            usuario,
            outraArea,
            ManagementRole.Gerente,
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1)));

        await context.SaveChangesAsync();

        return (outraArea.Id, usuario.Id);
    }

    private sealed record TestEnvironment(Guid TenantId, Guid UnitId, Guid UserId, string Suffix);
}
