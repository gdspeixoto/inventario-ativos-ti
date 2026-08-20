using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using FluentAssertions;

namespace ControleAtivos.UnitTests.Domain;

/// <summary>
/// A hierarquia institucional e a base do controle de acesso: se a arvore
/// aceitar estruturas invalidas, o escopo organizacional passa a conceder
/// visibilidade errada.
/// </summary>
public sealed class OrganizationalHierarchyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Matriz_sem_pai_fica_no_nivel_raiz()
    {
        var matriz = NewUnit(OrganizationalUnitType.Matriz, "CONTOSO", null);

        matriz.Level.Should().Be(0);
        matriz.ParentId.Should().BeNull();
        matriz.Path.Should().Be("/CONTOSO/");
    }

    [Fact]
    public void Negocio_exige_unidade_pai()
    {
        var act = () => NewUnit(OrganizationalUnitType.Negocio, "NEG-TEC", null);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Somente Matriz ou Filial podem existir sem unidade pai.");
    }

    [Fact]
    public void Path_acumula_a_cadeia_completa_ate_o_nucleo()
    {
        var (_, _, _, _, nucleo) = BuildFullHierarchy();

        nucleo.Path.Should().Be("/CONTOSO/NEG-TEC/AT-DIGITAL/UO-SISTEMAS/NS-SUPORTE/");
        nucleo.Level.Should().Be(4);
    }

    [Fact]
    public void Unidade_nao_pode_ficar_abaixo_de_nivel_igual_ou_inferior()
    {
        var (_, negocio, _, _, _) = BuildFullHierarchy();

        var act = () => NewUnit(OrganizationalUnitType.Negocio, "NEG-OUTRO", negocio);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*nao pode ficar abaixo*");
    }

    [Fact]
    public void Prefixo_de_descendencia_alcanca_toda_a_sub_arvore()
    {
        var (_, _, area, unidade, nucleo) = BuildFullHierarchy();

        var prefixo = area.DescendantPathPrefix();

        unidade.Path.Should().StartWith(prefixo);
        nucleo.Path.Should().StartWith(prefixo);
    }

    [Fact]
    public void Unidade_de_outro_tenant_nao_pode_ser_pai()
    {
        var matrizOutroTenant = new OrganizationalUnit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganizationalUnitType.Matriz,
            "OUTRA",
            "OUTRA",
            null);

        var act = () => NewUnit(OrganizationalUnitType.Negocio, "NEG-TEC", matrizOutroTenant);

        act.Should().Throw<DomainException>().WithMessage("*outro tenant*");
    }

    private static (
        OrganizationalUnit Matriz,
        OrganizationalUnit Negocio,
        OrganizationalUnit Area,
        OrganizationalUnit Unidade,
        OrganizationalUnit Nucleo) BuildFullHierarchy()
    {
        var matriz = NewUnit(OrganizationalUnitType.Matriz, "CONTOSO", null);
        var negocio = NewUnit(OrganizationalUnitType.Negocio, "NEG-TEC", matriz);
        var area = NewUnit(OrganizationalUnitType.AreaTecnologica, "AT-DIGITAL", negocio);
        var unidade = NewUnit(OrganizationalUnitType.UnidadeOperacional, "UO-SISTEMAS", area);
        var nucleo = NewUnit(OrganizationalUnitType.NucleoSuporte, "NS-SUPORTE", unidade);

        return (matriz, negocio, area, unidade, nucleo);
    }

    private static OrganizationalUnit NewUnit(
        OrganizationalUnitType type,
        string code,
        OrganizationalUnit? parent) =>
        new(Guid.NewGuid(), TenantId, type, code, code, parent);
}
