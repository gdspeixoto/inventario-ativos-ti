using ControleAtivos.Api.Security;
using FluentAssertions;

namespace ControleAtivos.UnitTests;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Senha_correta_e_aceita()
    {
        var hash = _hasher.Hash("SenhaDeEmergencia#2026");

        _hasher.Verify("SenhaDeEmergencia#2026", hash).Should().BeTrue();
    }

    [Fact]
    public void Senha_incorreta_e_recusada()
    {
        var hash = _hasher.Hash("SenhaDeEmergencia#2026");

        _hasher.Verify("SenhaDeEmergencia#2025", hash).Should().BeFalse();
    }

    [Fact]
    public void Hashes_da_mesma_senha_diferem_pelo_salt()
    {
        var primeiro = _hasher.Hash("mesma-senha");
        var segundo = _hasher.Hash("mesma-senha");

        primeiro.Should().NotBe(segundo, "cada hash usa salt proprio");
        _hasher.Verify("mesma-senha", primeiro).Should().BeTrue();
        _hasher.Verify("mesma-senha", segundo).Should().BeTrue();
    }

    [Fact]
    public void Hash_nao_contem_a_senha_em_texto_claro()
    {
        var hash = _hasher.Hash("senha-secreta-visivel");

        hash.Should().NotContain("senha-secreta-visivel");
    }

    [Fact]
    public void Parametros_de_derivacao_viajam_no_hash()
    {
        var hash = _hasher.Hash("qualquer");

        // Permite aumentar o custo no futuro sem invalidar as senhas atuais.
        hash.Should().StartWith("pbkdf2$sha256$210000$");
        hash.Split('$').Should().HaveCount(5);
    }

    [Theory]
    [InlineData("")]
    [InlineData("formato-invalido")]
    [InlineData("pbkdf2$sha256$naoehnumero$c2FsdA==$aGFzaA==")]
    [InlineData("bcrypt$sha256$210000$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2$sha256$210000$nao-e-base64!$aGFzaA==")]
    public void Hash_malformado_e_recusado_sem_lancar(string hash)
    {
        var act = () => _hasher.Verify("qualquer", hash);

        act.Should().NotThrow();
        _hasher.Verify("qualquer", hash).Should().BeFalse();
    }

    [Fact]
    public void Senha_vazia_nao_gera_hash()
    {
        var act = () => _hasher.Hash("   ");

        act.Should().Throw<ArgumentException>();
    }
}
