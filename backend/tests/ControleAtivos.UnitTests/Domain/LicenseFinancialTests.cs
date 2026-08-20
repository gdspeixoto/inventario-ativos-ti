using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.Events;
using ControleAtivos.Domain.ValueObjects;
using FluentAssertions;

namespace ControleAtivos.UnitTests.Domain;

/// <summary>
/// Regras financeiras de licencas. Os numeros usados reproduzem o cenario real
/// descrito na especificacao: Microsoft 365 Business Premium reajustado de
/// R$ 92,00 para R$ 105,00 por usuario.
/// </summary>
public sealed class LicenseFinancialTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Custo_mensal_e_o_produto_de_quantidade_por_valor_unitario()
    {
        var licenca = NewLicense(contractedQuantity: 120, unitPrice: 92m);

        licenca.MonthlyAmount.Amount.Should().Be(11_040m);
        licenca.AnnualAmount.Amount.Should().Be(132_480m);
    }

    [Fact]
    public void Quantidade_disponivel_e_derivada_do_uso()
    {
        var licenca = NewLicense(contractedQuantity: 120, unitPrice: 92m);
        licenca.UpdateUsage(104);

        licenca.AvailableQuantity.Should().Be(16);
        licenca.UtilizationRate.Should().Be(86.67m);
    }

    [Fact]
    public void Uso_acima_do_contratado_e_rejeitado()
    {
        var licenca = NewLicense(contractedQuantity: 10, unitPrice: 50m);

        var act = () => licenca.UpdateUsage(11);

        act.Should().Throw<DomainException>().WithMessage("*nao pode exceder*");
    }

    [Fact]
    public void Reajuste_calcula_diferenca_percentual_e_impacto_anual()
    {
        var licenca = NewLicense(contractedQuantity: 1, unitPrice: 92m);

        var reajuste = licenca.ApplyPriceAdjustment(
            new Money(105m),
            new DateOnly(2026, 3, 15),
            "Reajuste anual do fornecedor apos negociacao comercial",
            AdjustmentIndex.NegociacaoManual,
            UserId);

        reajuste.PreviousAmount.Amount.Should().Be(92m);
        reajuste.NewAmount.Amount.Should().Be(105m);
        reajuste.AbsoluteDifference.Should().Be(13m);
        reajuste.PercentageDifference.Value.Should().Be(14.1304m);
        reajuste.AnnualImpact.Should().Be(156m);
        reajuste.IsIncrease.Should().BeTrue();
    }

    [Fact]
    public void Reajuste_do_valor_mensal_recalcula_o_preco_unitario()
    {
        var licenca = NewLicense(contractedQuantity: 100, unitPrice: 92m);

        licenca.ApplyPriceAdjustment(
            new Money(10_500m),
            new DateOnly(2026, 3, 15),
            "Renegociacao de contrato",
            AdjustmentIndex.NegociacaoManual,
            UserId);

        licenca.UnitPrice.Amount.Should().Be(105m);
    }

    [Fact]
    public void Reajuste_registra_evento_de_timeline_com_valores()
    {
        var licenca = NewLicense(contractedQuantity: 1, unitPrice: 92m);
        licenca.ClearDomainEvents();

        licenca.ApplyPriceAdjustment(
            new Money(105m),
            new DateOnly(2026, 3, 15),
            "Reajuste anual",
            AdjustmentIndex.Ipca,
            UserId);

        var evento = licenca.DomainEvents
            .OfType<TimelineEntryRequested>()
            .Single(e => e.EventType == TimelineEventType.ReajusteAplicado);

        evento.PreviousAmount.Should().Be(92m);
        evento.NewAmount.Should().Be(105m);
        evento.Description.Should().Contain("14,13%");
    }

    [Fact]
    public void Reajuste_com_valor_identico_e_rejeitado()
    {
        var licenca = NewLicense(contractedQuantity: 1, unitPrice: 92m);

        var act = () => licenca.ApplyPriceAdjustment(
            new Money(92m),
            new DateOnly(2026, 3, 15),
            "Sem mudanca",
            AdjustmentIndex.NegociacaoManual,
            UserId);

        act.Should().Throw<DomainException>().WithMessage("*diferente do valor atual*");
    }

    [Fact]
    public void Alteracao_de_quantidade_recalcula_custo_e_devolve_o_impacto()
    {
        var licenca = NewLicense(contractedQuantity: 80, unitPrice: 48m);

        var impacto = licenca.ChangeContractedQuantity(100, "Contratacao de 20 novas licencas");

        licenca.MonthlyAmount.Amount.Should().Be(4_800m);
        impacto.Amount.Should().Be(960m);
        impacto.ToAnnual().Amount.Should().Be(11_520m);
    }

    [Fact]
    public void Reducao_abaixo_do_uso_atual_e_bloqueada()
    {
        var licenca = NewLicense(contractedQuantity: 120, unitPrice: 92m);
        licenca.UpdateUsage(104);

        var act = () => licenca.ChangeContractedQuantity(100, "Reducao de custos");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*existem 104 licenca(s) em uso*");
    }

    [Fact]
    public void Ativo_cancelado_nao_aceita_reajuste()
    {
        var licenca = NewLicense(contractedQuantity: 10, unitPrice: 50m);
        licenca.ChangeStatus(AssetStatus.Cancelado, "Produto descontinuado pelo fabricante");

        var act = () => licenca.ApplyPriceAdjustment(
            new Money(600m),
            new DateOnly(2026, 3, 15),
            "Tentativa indevida",
            AdjustmentIndex.NegociacaoManual,
            UserId);

        act.Should().Throw<DomainException>().WithMessage("*cancelado ou descontinuado*");
    }

    [Fact]
    public void Simulacao_nao_altera_o_ativo()
    {
        var licenca = NewLicense(contractedQuantity: 1, unitPrice: 92m);

        var (diferenca, percentual, impactoAnual) =
            PriceAdjustment.Simulate(licenca.MonthlyAmount, new Money(105m));

        diferenca.Should().Be(13m);
        percentual.Value.Should().Be(14.1304m);
        impactoAnual.Should().Be(156m);
        licenca.MonthlyAmount.Amount.Should().Be(92m, "a simulacao nao persiste nada");
    }

    private static LicenseAsset NewLicense(int contractedQuantity, decimal unitPrice)
    {
        var matriz = new OrganizationalUnit(
            Guid.NewGuid(),
            TenantId,
            OrganizationalUnitType.Matriz,
            "CONTOSO",
            "CONTOSO",
            null);

        return new LicenseAsset(
            Guid.NewGuid(),
            TenantId,
            matriz,
            "Microsoft 365 Business Premium",
            $"LIC-{Guid.NewGuid():N}"[..20],
            "Microsoft",
            "Microsoft 365 Business Premium",
            contractedQuantity,
            new Money(unitPrice));
    }
}
