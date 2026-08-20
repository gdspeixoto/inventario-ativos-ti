using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using FluentAssertions;

namespace ControleAtivos.UnitTests.Domain;

/// <summary>
/// Requisito central do produto: um gerente pode responder por mais de uma
/// area, unidade ou nucleo simultaneamente.
/// </summary>
public sealed class ManagementAssignmentTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Mesmo_gerente_pode_liderar_duas_areas_ao_mesmo_tempo()
    {
        var gerente = NewUser();
        var matriz = NewUnit(OrganizationalUnitType.Matriz, "CONTOSO", null);
        var negocio = NewUnit(OrganizationalUnitType.Negocio, "NEG-TEC", matriz);
        var areaA = NewUnit(OrganizationalUnitType.AreaTecnologica, "AT-A", negocio);
        var areaB = NewUnit(OrganizationalUnitType.AreaTecnologica, "AT-B", negocio);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var vinculoA = NewAssignment(gerente, areaA, ManagementRole.Gerente, hoje);
        var vinculoB = NewAssignment(gerente, areaB, ManagementRole.Gerente, hoje);

        vinculoA.IsCurrentlyActive().Should().BeTrue();
        vinculoB.IsCurrentlyActive().Should().BeTrue();
        vinculoA.OrganizationalUnitId.Should().NotBe(vinculoB.OrganizationalUnitId);
    }

    [Fact]
    public void Vinculo_encerrado_deixa_de_valer_a_partir_da_data_final()
    {
        var gerente = NewUser();
        var matriz = NewUnit(OrganizationalUnitType.Matriz, "CONTOSO", null);
        var inicio = new DateOnly(2026, 1, 1);

        var vinculo = NewAssignment(gerente, matriz, ManagementRole.Gerente, inicio);
        vinculo.Finish(new DateOnly(2026, 6, 30), "Mudanca de estrutura");

        vinculo.IsActiveOn(new DateOnly(2026, 6, 30)).Should().BeTrue();
        vinculo.IsActiveOn(new DateOnly(2026, 7, 1)).Should().BeFalse();
        vinculo.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void Encerramento_antes_do_inicio_e_rejeitado()
    {
        var vinculo = NewAssignment(
            NewUser(),
            NewUnit(OrganizationalUnitType.Matriz, "CONTOSO", null),
            ManagementRole.Gerente,
            new DateOnly(2026, 5, 1));

        var act = () => vinculo.Finish(new DateOnly(2026, 4, 1), "Erro de digitacao");

        act.Should().Throw<DomainException>().WithMessage("*anterior a data de inicio*");
    }

    [Fact]
    public void Vinculo_alcanca_descendentes_por_padrao()
    {
        var vinculo = NewAssignment(
            NewUser(),
            NewUnit(OrganizationalUnitType.Matriz, "CONTOSO", null),
            ManagementRole.Gerente,
            DateOnly.FromDateTime(DateTime.UtcNow));

        vinculo.IncludesDescendants.Should().BeTrue();

        vinculo.RestrictToOwnUnit();
        vinculo.IncludesDescendants.Should().BeFalse();
    }

    [Fact]
    public void Usuario_de_outro_tenant_nao_pode_receber_atribuicao()
    {
        var usuarioDeOutroTenant = new AppUser(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "sub-externo",
            "Fulano",
            "fulano@empresa.com");

        var act = () => NewAssignment(
            usuarioDeOutroTenant,
            NewUnit(OrganizationalUnitType.Matriz, "CONTOSO", null),
            ManagementRole.Gerente,
            DateOnly.FromDateTime(DateTime.UtcNow));

        act.Should().Throw<DomainException>().WithMessage("*outro tenant*");
    }

    private static AppUser NewUser() =>
        new(Guid.NewGuid(), TenantId, Guid.NewGuid().ToString(), "Ana Souza", "ana.souza@example.com");

    private static OrganizationalUnit NewUnit(
        OrganizationalUnitType type,
        string code,
        OrganizationalUnit? parent) =>
        new(Guid.NewGuid(), TenantId, type, code, code, parent);

    private static ManagementAssignment NewAssignment(
        AppUser user,
        OrganizationalUnit unit,
        ManagementRole role,
        DateOnly start) =>
        new(Guid.NewGuid(), TenantId, user, unit, role, start);
}
