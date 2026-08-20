using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.ValueObjects;
using FluentAssertions;

namespace ControleAtivos.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Valor_negativo_e_rejeitado()
    {
        var act = () => new Money(-1m);

        act.Should().Throw<DomainException>().WithMessage("*nao pode ser negativo*");
    }

    [Fact]
    public void Arredonda_para_duas_casas_afastando_do_zero()
    {
        new Money(10.555m).Amount.Should().Be(10.56m);
        new Money(10.554m).Amount.Should().Be(10.55m);
    }

    [Fact]
    public void Soma_entre_moedas_diferentes_e_bloqueada()
    {
        var reais = new Money(100m);
        var dolares = new Money(100m, "USD");

        var act = () => reais.Add(dolares);

        act.Should().Throw<DomainException>().WithMessage("*moedas diferentes*");
    }

    [Fact]
    public void Projecao_anual_multiplica_por_doze()
    {
        new Money(1_120m).ToAnnual().Amount.Should().Be(13_440m);
    }

    [Fact]
    public void Percentual_sobre_base_zero_retorna_zero_em_vez_de_dividir_por_zero()
    {
        var resultado = new Money(100m).PercentageDifferenceFrom(Money.Zero());

        resultado.Value.Should().Be(0m);
    }

    [Fact]
    public void Reducao_de_valor_gera_percentual_negativo()
    {
        var percentual = new Money(80m).PercentageDifferenceFrom(new Money(100m));

        percentual.Value.Should().Be(-20m);
        percentual.IsDecrease.Should().BeTrue();
        percentual.Absolute.Should().Be(20m);
    }

    [Theory]
    [InlineData("BR")]
    [InlineData("BRLL")]
    [InlineData("")]
    public void Moeda_fora_do_padrao_iso_e_rejeitada(string currency)
    {
        var act = () => new Money(10m, currency);

        act.Should().Throw<DomainException>();
    }
}
