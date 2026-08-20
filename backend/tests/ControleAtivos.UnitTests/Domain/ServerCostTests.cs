using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.Events;
using ControleAtivos.Domain.ValueObjects;
using FluentAssertions;

namespace ControleAtivos.UnitTests.Domain;

/// <summary>
/// Composicao de custos de servidores. Reproduz o cenario da especificacao:
/// APP-PRD-01 com RAM ampliada de 16 GB para 32 GB e custo mensal indo de
/// R$ 850,00 para R$ 1.120,00.
/// </summary>
public sealed class ServerCostTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Custo_total_soma_os_quatro_componentes()
    {
        var servidor = NewServer(infrastructure: 850m);

        var impacto = servidor.UpdateCostBreakdown(
            new Money(850m),
            new Money(180m),
            new Money(60m),
            new Money(30m),
            "Composicao inicial detalhada");

        servidor.MonthlyAmount.Amount.Should().Be(1_120m);
        servidor.AnnualAmount.Amount.Should().Be(13_440m);
        impacto.Amount.Should().Be(270m);
    }

    [Fact]
    public void Atualizacao_de_custo_registra_impacto_mensal_e_anual_na_timeline()
    {
        var servidor = NewServer(infrastructure: 850m);
        servidor.ClearDomainEvents();

        servidor.UpdateCostBreakdown(
            new Money(1_120m),
            Money.Zero(),
            Money.Zero(),
            Money.Zero(),
            "Ampliacao de memoria de 16 GB para 32 GB");

        var evento = servidor.DomainEvents
            .OfType<TimelineEntryRequested>()
            .Single(e => e.EventType == TimelineEventType.AlteracaoValor);

        evento.PreviousAmount.Should().Be(850m);
        evento.NewAmount.Should().Be(1_120m);
        evento.Description.Should().Contain("BRL 270,00");
        evento.Description.Should().Contain("BRL 3.240,00");
    }

    [Fact]
    public void Redimensionamento_registra_configuracao_anterior_e_atual()
    {
        var servidor = NewServer(infrastructure: 850m);
        servidor.Resize(8, 16, 200, "Configuracao inicial");
        servidor.ClearDomainEvents();

        servidor.Resize(8, 32, 200, "Aumento de carga da aplicacao");

        var evento = servidor.DomainEvents
            .OfType<TimelineEntryRequested>()
            .Single(e => e.EventType == TimelineEventType.ServidorAlterado);

        evento.Description.Should().Contain("16 GB RAM");
        evento.Description.Should().Contain("32 GB RAM");
    }

    [Fact]
    public void Servidor_sem_backup_recente_e_detectado()
    {
        var servidor = NewServer(infrastructure: 760m);

        servidor.HasRecentBackup(2).Should().BeFalse("nenhum backup foi registrado");

        servidor.DefineBackup("Diario 02:00", DateTimeOffset.UtcNow.AddDays(-5));
        servidor.HasRecentBackup(2).Should().BeFalse();

        servidor.DefineBackup("Diario 02:00", DateTimeOffset.UtcNow.AddHours(-6));
        servidor.HasRecentBackup(2).Should().BeTrue();
    }

    [Fact]
    public void Desativacao_muda_status_e_informa_economia()
    {
        var servidor = NewServer(infrastructure: 3_850m);
        servidor.ClearDomainEvents();

        servidor.Decommission("Migracao concluida para o novo cluster");

        servidor.Status.Should().Be(AssetStatus.Descontinuado);

        var evento = servidor.DomainEvents
            .OfType<TimelineEntryRequested>()
            .Single(e => e.EventType == TimelineEventType.ServidorDesativado);

        evento.Description.Should().Contain("Economia mensal estimada: BRL 3.850,00");
    }

    [Fact]
    public void Reajuste_direto_recai_sobre_a_linha_de_infraestrutura()
    {
        var servidor = NewServer(infrastructure: 850m);
        servidor.UpdateCostBreakdown(
            new Money(850m),
            new Money(180m),
            new Money(60m),
            new Money(30m),
            "Composicao inicial");

        servidor.ApplyPriceAdjustment(
            new Money(1_400m),
            new DateOnly(2026, 4, 2),
            "Upgrade de instancia na nuvem",
            AdjustmentIndex.NegociacaoManual,
            Guid.NewGuid());

        servidor.InfrastructureMonthlyCost.Amount.Should().Be(1_130m);
        servidor.LicenseMonthlyCost.Amount.Should().Be(180m, "os demais componentes nao mudam");
        servidor.MonthlyAmount.Amount.Should().Be(1_400m);
    }

    private static ServerAsset NewServer(decimal infrastructure)
    {
        var matriz = new OrganizationalUnit(
            Guid.NewGuid(),
            TenantId,
            OrganizationalUnitType.Matriz,
            "CONTOSO",
            "CONTOSO",
            null);

        return new ServerAsset(
            Guid.NewGuid(),
            TenantId,
            matriz,
            "APP-PRD-01",
            $"SRV-{Guid.NewGuid():N}"[..20],
            "app-prd-01.contoso.local",
            ServerType.Virtual,
            ServerEnvironment.Producao,
            new Money(infrastructure));
    }
}
