using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.Events;
using ControleAtivos.Domain.ValueObjects;
using FluentAssertions;

namespace ControleAtivos.UnitTests;

/// <summary>
/// Correcao cadastral e exclusao.
///
/// Sao operacoes distintas de desativar, e a diferenca importa: desativar
/// registra que algo real deixou de operar — informacao que precisa sobreviver;
/// corrigir conserta um dado errado; excluir desfaz um cadastro que nao deveria
/// existir. Confundi-las apagaria historico legitimo.
/// </summary>
public sealed class AssetCorrectionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Correcao_altera_nome_e_codigo()
    {
        var license = BuildLicense();

        license.Rename("Microsoft 365 E3", "lic-m365-e3");

        license.Name.Should().Be("Microsoft 365 E3");
        license.Code.Should().Be("LIC-M365-E3", "o codigo e sempre normalizado em maiusculas");
    }

    [Fact]
    public void Correcao_registra_o_valor_anterior_na_timeline()
    {
        var license = BuildLicense();
        license.ClearDomainEvents();

        license.Rename("Nome Corrigido", "COD-NOVO");

        var evento = license.DomainEvents.OfType<TimelineEntryRequested>().Single();

        evento.EventType.Should().Be(TimelineEventType.EdicaoCadastral);
        evento.Description.Should().Contain("Licenca Original", "quem audita precisa ver o valor anterior");
        evento.Description.Should().Contain("Nome Corrigido");
    }

    [Fact]
    public void Correcao_nao_e_cancelamento()
    {
        var license = BuildLicense();
        license.ClearDomainEvents();

        license.Rename("Outro Nome", "OUTRO-COD");

        var evento = license.DomainEvents.OfType<TimelineEntryRequested>().Single();

        evento.EventType.Should().NotBe(TimelineEventType.Cancelamento);
        license.Status.Should().Be(AssetStatus.Ativo, "corrigir o cadastro nao muda a situacao");
    }

    [Fact]
    public void Correcao_sem_mudanca_nao_gera_evento()
    {
        var license = BuildLicense();
        license.ClearDomainEvents();

        license.Rename(license.Name, license.Code);

        license.DomainEvents.Should().BeEmpty("nada mudou, logo nao ha o que registrar");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Correcao_rejeita_nome_vazio(string name)
    {
        var license = BuildLicense();

        var act = () => license.Rename(name, "COD-VALIDO");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Licenca_recem_criada_pode_ser_excluida()
    {
        var license = BuildLicense();

        license.CanBeDeleted.Should().BeTrue("cadastro sem consequencia financeira e apenas um engano");
    }

    [Fact]
    public void Licenca_com_reajuste_nao_pode_ser_excluida()
    {
        var license = BuildLicense();

        license.ApplyPriceAdjustment(
            new Money(1_200m),
            new DateOnly(2026, 3, 15),
            "Reajuste anual",
            AdjustmentIndex.Igpm,
            Guid.NewGuid());

        license.CanBeDeleted.Should().BeFalse(
            "ha historia financeira: o caminho correto passa a ser desativar");
    }

    [Fact]
    public void Correcao_de_produto_nao_altera_valor_nem_quantidade()
    {
        var license = BuildLicense();
        var valorAntes = license.MonthlyAmount;
        var quantidadeAntes = license.ContractedQuantity;

        license.CorrectProduct("Microsoft", "Microsoft 365 Business Premium");

        license.Manufacturer.Should().Be("Microsoft");
        license.MonthlyAmount.Should().Be(valorAntes);
        license.ContractedQuantity.Should().Be(quantidadeAntes);
    }

    [Fact]
    public void Correcao_de_identificacao_do_servidor_preserva_custos()
    {
        var server = BuildServer();
        var custoAntes = server.MonthlyAmount;

        server.CorrectIdentification("novo-host.contoso.local", ServerType.Cloud, ServerEnvironment.Homologacao);

        server.Hostname.Should().Be("novo-host.contoso.local");
        server.Environment.Should().Be(ServerEnvironment.Homologacao);
        server.MonthlyAmount.Should().Be(custoAntes, "corrigir o cadastro nao mexe em dinheiro");
    }

    [Fact]
    public void Desativar_servidor_preserva_o_registro()
    {
        var server = BuildServer();

        server.Decommission("Migrado para nuvem");

        // Desativacao e evento de negocio: o servidor existiu e isso permanece.
        server.Status.Should().Be(AssetStatus.Descontinuado);
        server.DomainEvents.OfType<TimelineEntryRequested>().Should().NotBeEmpty();
    }

    private static LicenseAsset BuildLicense()
    {
        var unit = new OrganizationalUnit(
            Guid.NewGuid(), TenantId, OrganizationalUnitType.Matriz, "CONTOSO", "MAT-1", null);

        return new LicenseAsset(
            Guid.NewGuid(),
            TenantId,
            unit,
            "Licenca Original",
            "COD-ORIGINAL",
            "Microsoft",
            "Microsoft 365",
            contractedQuantity: 10,
            unitPrice: new Money(100m));
    }

    private static ServerAsset BuildServer()
    {
        var unit = new OrganizationalUnit(
            Guid.NewGuid(), TenantId, OrganizationalUnitType.Matriz, "CONTOSO", "MAT-2", null);

        return new ServerAsset(
            Guid.NewGuid(),
            TenantId,
            unit,
            "Servidor Teste",
            "SRV-1",
            "host.contoso.local",
            ServerType.Virtual,
            ServerEnvironment.Producao,
            new Money(1_000m));
    }
}

/// <summary>
/// Hierarquia de unidades.
///
/// A checagem de dependencias para exclusao NAO vive na entidade: a colecao
/// <c>Children</c> so e populada quando o EF carrega o agregado, entao uma
/// propriedade em memoria responderia "sem filhas" para uma unidade que tem
/// filhas no banco. Uma verificacao que erra em silencio e pior que nenhuma.
/// Por isso quem responde e <c>GetDeletionBlockersAsync</c>, consultando o
/// banco, coberto nos testes de integracao.
/// </summary>
public sealed class OrganizationalUnitHierarchyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Filha_aponta_para_a_unidade_pai()
    {
        var parent = new OrganizationalUnit(
            Guid.NewGuid(), TenantId, OrganizationalUnitType.Matriz, "CONTOSO", "MAT", null);

        var child = new OrganizationalUnit(
            Guid.NewGuid(), TenantId, OrganizationalUnitType.Negocio, "Tecnologia", "NEG", parent);

        child.ParentId.Should().Be(parent.Id);
        child.Level.Should().Be(parent.Level + 1);
    }

    [Fact]
    public void Desativar_preserva_a_unidade_e_a_hierarquia()
    {
        var parent = new OrganizationalUnit(
            Guid.NewGuid(), TenantId, OrganizationalUnitType.Matriz, "CONTOSO", "MAT", null);

        var child = new OrganizationalUnit(
            Guid.NewGuid(), TenantId, OrganizationalUnitType.Negocio, "Tecnologia", "NEG", parent);

        parent.Deactivate();

        parent.IsActive.Should().BeFalse();
        child.ParentId.Should().Be(parent.Id, "a estrutura continua de pe apos a desativacao");
    }

    [Fact]
    public void Correcao_de_nome_nao_altera_o_codigo_nem_o_caminho()
    {
        var unit = new OrganizationalUnit(
            Guid.NewGuid(), TenantId, OrganizationalUnitType.Matriz, "CONTOSO", "MAT", null);

        var caminhoAntes = unit.Path;

        unit.Rename("CONTOSO - Sede");

        unit.Name.Should().Be("CONTOSO - Sede");
        unit.Code.Should().Be("MAT");
        unit.Path.Should().Be(caminhoAntes, "o caminho e derivado do codigo, que nao mudou");
    }
}
