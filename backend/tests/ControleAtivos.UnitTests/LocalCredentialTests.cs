using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Entities;
using FluentAssertions;

namespace ControleAtivos.UnitTests;

public sealed class LocalCredentialTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Credencial_nova_pode_autenticar()
    {
        var credential = Build();

        credential.CanAuthenticate.Should().BeTrue();
        credential.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public void Nome_de_usuario_e_normalizado()
    {
        var credential = Build(username: "  Admin.Emergencia  ");

        credential.Username.Should().Be("admin.emergencia");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_de_usuario_curto_demais_e_rejeitado(string username)
    {
        var act = () => Build(username: username);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Bloqueia_apos_o_limite_de_tentativas()
    {
        var credential = Build();

        for (var i = 0; i < LocalCredential.MaxFailedAttempts - 1; i++)
        {
            credential.RegisterFailedAttempt();
        }

        credential.IsLockedOut.Should().BeFalse("ainda falta uma tentativa para o limite");

        credential.RegisterFailedAttempt();

        credential.IsLockedOut.Should().BeTrue();
        credential.CanAuthenticate.Should().BeFalse();
    }

    [Fact]
    public void Bloqueio_e_temporario_e_nao_permanente()
    {
        var credential = Build();

        for (var i = 0; i < LocalCredential.MaxFailedAttempts; i++)
        {
            credential.RegisterFailedAttempt();
        }

        // Bloqueio permanente transformaria forca bruta em negacao de servico
        // contra o proprio administrador.
        credential.LockedUntil.Should().NotBeNull();
        credential.LockedUntil!.Value.Should().BeCloseTo(
            DateTimeOffset.UtcNow.Add(LocalCredential.LockoutDuration),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Login_bem_sucedido_zera_as_tentativas()
    {
        var credential = Build();
        credential.RegisterFailedAttempt();
        credential.RegisterFailedAttempt();

        credential.RegisterSuccessfulLogin();

        credential.FailedAttempts.Should().Be(0);
        credential.LockedUntil.Should().BeNull();
        credential.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void Credencial_expirada_nao_autentica()
    {
        var credential = Build(expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        credential.IsExpired.Should().BeTrue();
        credential.CanAuthenticate.Should().BeFalse("conta de emergencia esquecida e porta aberta");
    }

    [Fact]
    public void Credencial_desativada_nao_autentica()
    {
        var credential = Build();

        credential.Deactivate();

        credential.CanAuthenticate.Should().BeFalse();
    }

    [Fact]
    public void Troca_de_senha_libera_o_bloqueio()
    {
        var credential = Build();

        for (var i = 0; i < LocalCredential.MaxFailedAttempts; i++)
        {
            credential.RegisterFailedAttempt();
        }

        credential.ChangePassword("pbkdf2$sha256$210000$c2FsdA==$bm92bw==");

        credential.IsLockedOut.Should().BeFalse();
        credential.FailedAttempts.Should().Be(0);
        credential.CanAuthenticate.Should().BeTrue();
    }

    [Fact]
    public void Credencial_de_outro_tenant_e_rejeitada()
    {
        var user = new AppUser(Guid.NewGuid(), Guid.NewGuid(), "sub", "Outro", "outro@example.com");

        var act = () => new LocalCredential(
            Guid.NewGuid(), TenantId, user, "admin.emergencia", "hash");

        act.Should().Throw<DomainException>().WithMessage("*outro tenant*");
    }

    private static LocalCredential Build(
        string username = "admin.emergencia",
        DateTimeOffset? expiresAt = null)
    {
        var user = new AppUser(
            Guid.NewGuid(), TenantId, "local:admin", "Admin Emergencia", "admin@example.com");

        return new LocalCredential(
            Guid.NewGuid(),
            TenantId,
            user,
            username,
            "pbkdf2$sha256$210000$c2FsdA==$aGFzaA==",
            expiresAt);
    }
}

public sealed class UserRoleAssignmentTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Concessao_nasce_ativa()
    {
        var assignment = Build();

        assignment.IsActive.Should().BeTrue();
        assignment.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Revogacao_preserva_o_historico()
    {
        var assignment = Build();
        var revogadoPor = Guid.NewGuid();

        assignment.Revoke(revogadoPor);

        assignment.IsActive.Should().BeFalse();
        assignment.RevokedAt.Should().NotBeNull();
        assignment.RevokedByUserId.Should().Be(revogadoPor);
        assignment.GrantedAt.Should().NotBe(default, "a concessao original continua registrada");
    }

    [Fact]
    public void Revogar_duas_vezes_e_rejeitado()
    {
        var assignment = Build();
        assignment.Revoke();

        var act = () => assignment.Revoke();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Restauracao_reativa_sem_criar_registro_novo()
    {
        var assignment = Build();
        assignment.Revoke();

        assignment.Restore(Guid.NewGuid());

        assignment.IsActive.Should().BeTrue();
        assignment.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Papel_para_usuario_de_outro_tenant_e_rejeitado()
    {
        var user = new AppUser(Guid.NewGuid(), Guid.NewGuid(), "sub", "Outro", "outro@example.com");

        var act = () => new UserRoleAssignment(
            Guid.NewGuid(), TenantId, user, "controle-ativos-admin");

        act.Should().Throw<DomainException>().WithMessage("*outro tenant*");
    }

    private static UserRoleAssignment Build()
    {
        var user = new AppUser(
            Guid.NewGuid(), TenantId, "sub-1", "Gabriel", "gabriel@example.com");

        return new UserRoleAssignment(
            Guid.NewGuid(), TenantId, user, "controle-ativos-admin");
    }
}
