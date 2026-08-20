using System.IdentityModel.Tokens.Jwt;
using ControleAtivos.Api.Configuration;
using ControleAtivos.Api.Security;
using ControleAtivos.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ControleAtivos.IntegrationTests;

[Collection(nameof(DatabaseCollection))]
public sealed class LocalAuthenticationTests(DatabaseFixture fixture)
{
    private const string SigningKey = "chave-de-teste-com-mais-de-32-bytes-para-hmac-sha256";
    private const string Password = "SenhaDeEmergencia#2026";

    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public async Task Credencial_valida_emite_token_com_tenant_e_papeis()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildService(env);

        var resultado = await service.AuthenticateAsync(env.Username, Password, CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.Roles.Should().Contain("controle-ativos-admin");

        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado.AccessToken);

        token.Issuer.Should().Be(LocalAuthenticationService.Issuer);
        token.Audiences.Should().Contain(LocalAuthenticationService.Audience);

        token.Claims.Should().Contain(c =>
            c.Type == "tenant_id" && c.Value == env.TenantId.ToString(),
            "sem a claim de tenant o middleware recusaria a requisicao seguinte");

        token.Claims.Should().Contain(c => c.Type == "amr" && c.Value == "pwd",
            "o token local precisa ser distinguivel do corporativo numa auditoria");
    }

    [Fact]
    public async Task Senha_incorreta_e_recusada_e_contabilizada()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildService(env);

        var resultado = await service.AuthenticateAsync(env.Username, "senha-errada", CancellationToken.None);

        resultado.Should().BeNull();

        await using var verificacao = fixture.CreateContext(new TestTenantContext(env.TenantId, bypass: true));

        var credencial = await verificacao.LocalCredentials
            .FirstAsync(c => c.Username == env.Username);

        credencial.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Usuario_inexistente_e_recusado_sem_lancar()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildService(env);

        var resultado = await service.AuthenticateAsync(
            "nao.existe.mesmo", Password, CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Bloqueio_impede_login_mesmo_com_a_senha_certa()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildService(env);

        for (var i = 0; i < LocalCredential.MaxFailedAttempts; i++)
        {
            await service.AuthenticateAsync(env.Username, "senha-errada", CancellationToken.None);
        }

        var resultado = await service.AuthenticateAsync(env.Username, Password, CancellationToken.None);

        resultado.Should().BeNull("a conta esta bloqueada por tempo apos as tentativas");
    }

    [Fact]
    public async Task Credencial_expirada_nao_autentica()
    {
        var env = await BuildEnvironmentAsync(expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        var service = BuildService(env);

        var resultado = await service.AuthenticateAsync(env.Username, Password, CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Credencial_desativada_nao_autentica()
    {
        var env = await BuildEnvironmentAsync();

        await using (var context = fixture.CreateContext(new TestTenantContext(env.TenantId, bypass: true)))
        {
            var credencial = await context.LocalCredentials.FirstAsync(c => c.Username == env.Username);
            credencial.Deactivate();
            await context.SaveChangesAsync();
        }

        var resultado = await BuildService(env)
            .AuthenticateAsync(env.Username, Password, CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Login_bem_sucedido_zera_tentativas_anteriores()
    {
        var env = await BuildEnvironmentAsync();
        var service = BuildService(env);

        await service.AuthenticateAsync(env.Username, "senha-errada", CancellationToken.None);
        await service.AuthenticateAsync(env.Username, Password, CancellationToken.None);

        await using var verificacao = fixture.CreateContext(new TestTenantContext(env.TenantId, bypass: true));

        var credencial = await verificacao.LocalCredentials.FirstAsync(c => c.Username == env.Username);

        credencial.FailedAttempts.Should().Be(0);
        credencial.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Papel_revogado_nao_entra_no_token()
    {
        var env = await BuildEnvironmentAsync();

        await using (var context = fixture.CreateContext(new TestTenantContext(env.TenantId, bypass: true)))
        {
            var papel = await context.UserRoleAssignments.FirstAsync(a => a.UserId == env.UserId);
            papel.Revoke();
            await context.SaveChangesAsync();
        }

        var resultado = await BuildService(env)
            .AuthenticateAsync(env.Username, Password, CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.Roles.Should().BeEmpty("a revogacao vale a partir do proximo login");
    }

    private LocalAuthenticationService BuildService(TestEnvironment env)
    {
        /*
         * O login acontece antes de o tenant estar resolvido, entao o servico
         * recebe um contexto sem tenant e depende do bypass interno — que e
         * exatamente o comportamento sob teste.
         *
         * A instancia do contexto de tenant e compartilhada entre o DbContext e
         * o servico de proposito: no container de DI ela e scoped, ou seja, a
         * mesma na requisicao inteira. Duplica-la aqui faria o bypass valer num
         * objeto e a consulta ler outro — o teste passaria a exercitar um
         * arranjo que nao existe em producao.
         */
        var tenantContext = new TestTenantContext(Guid.Empty);
        var context = fixture.CreateContext(tenantContext);

        var options = Options.Create(new AccessOptions
        {
            LocalLogin = new LocalLoginOptions
            {
                Enabled = true,
                SigningKey = SigningKey,
                TokenLifetimeMinutes = 60,
            },
        });

        return new LocalAuthenticationService(
            context,
            _hasher,
            tenantContext,
            options,
            NullLogger<LocalAuthenticationService>.Instance);
    }

    private async Task<TestEnvironment> BuildEnvironmentAsync(DateTimeOffset? expiresAt = null)
    {
        var tenantId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();
        var username = $"admin.{suffix}";

        await using var context = fixture.CreateContext(new TestTenantContext(tenantId));

        context.Tenants.Add(new Tenant(tenantId, $"tenant-{suffix}", $"tenant-{suffix}"));

        var user = new AppUser(
            Guid.NewGuid(), tenantId, $"local:{username}", "Admin Emergencia", $"{username}@example.com");

        context.Users.Add(user);

        context.LocalCredentials.Add(new LocalCredential(
            Guid.NewGuid(), tenantId, user, username, _hasher.Hash(Password), expiresAt));

        context.UserRoleAssignments.Add(new UserRoleAssignment(
            Guid.NewGuid(), tenantId, user, "controle-ativos-admin"));

        await context.SaveChangesAsync();

        return new TestEnvironment(tenantId, user.Id, username);
    }

    private sealed record TestEnvironment(Guid TenantId, Guid UserId, string Username);
}
