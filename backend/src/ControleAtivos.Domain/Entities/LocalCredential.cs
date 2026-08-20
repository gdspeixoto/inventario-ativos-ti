using ControleAtivos.Domain.Common;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Credencial local de emergencia (<em>break-glass</em>).
///
/// Existe para um unico cenario: o provedor de identidade corporativo estar
/// indisponivel e ainda assim ser necessario entrar no sistema. Nao e um
/// caminho alternativo de login para o dia a dia — para isso existe o SSO, que
/// traz MFA e a gestao centralizada de identidade.
///
/// Consequencias desse proposito, refletidas no desenho:
///
/// <list type="bullet">
/// <item>o hash e responsabilidade da camada de aplicacao (PBKDF2); o dominio
/// so guarda o resultado e nunca ve a senha em texto claro;</item>
/// <item>toda tentativa e contabilizada, e o bloqueio e por tempo, nao
/// permanente — bloqueio permanente transformaria um ataque de forca bruta em
/// negacao de servico contra o proprio administrador;</item>
/// <item>a credencial expira sozinha: uma conta de emergencia esquecida e uma
/// porta aberta que ninguem esta olhando.</item>
/// </list>
/// </summary>
public sealed class LocalCredential : TenantEntity
{
    /// <summary>Tentativas malsucedidas toleradas antes do bloqueio temporario.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Duracao do bloqueio apos estourar <see cref="MaxFailedAttempts"/>.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private LocalCredential()
    {
    }

    public LocalCredential(
        Guid id,
        Guid tenantId,
        AppUser user,
        string username,
        string passwordHash,
        DateTimeOffset? expiresAt = null)
        : base(id, tenantId)
    {
        DomainException.ThrowIf(
            user.TenantId != tenantId,
            "O usuario pertence a outro tenant.");

        UserId = user.Id;
        User = user;
        Username = NormalizeUsername(username);
        PasswordHash = Guard.MaxLength(Guard.NotEmpty(passwordHash, nameof(passwordHash)), 500, nameof(passwordHash));
        ExpiresAt = expiresAt;
        IsActive = true;
        PasswordChangedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public AppUser User { get; private set; } = null!;

    public string Username { get; private set; } = string.Empty;

    /// <summary>
    /// Hash PBKDF2 com salt embutido, no formato produzido pela camada de
    /// aplicacao. Nunca a senha, nunca reversivel.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset PasswordChangedAt { get; private set; }

    /// <summary>Validade da credencial. Nulo significa sem expiracao.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;

    public bool IsLockedOut => LockedUntil.HasValue && LockedUntil.Value > DateTimeOffset.UtcNow;

    /// <summary>
    /// Se a credencial pode ser usada agora. Reune os tres motivos de recusa
    /// para que o servico nao precise reimplementar a regra — e nao possa
    /// esquecer de checar um deles.
    /// </summary>
    public bool CanAuthenticate => IsActive && !IsExpired && !IsLockedOut;

    public void RegisterSuccessfulLogin()
    {
        FailedAttempts = 0;
        LockedUntil = null;
        LastLoginAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RegisterFailedAttempt()
    {
        FailedAttempts++;

        if (FailedAttempts >= MaxFailedAttempts)
        {
            LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
        }

        Touch();
    }

    public void ChangePassword(string passwordHash, Guid? userId = null)
    {
        PasswordHash = Guard.MaxLength(Guard.NotEmpty(passwordHash, nameof(passwordHash)), 500, nameof(passwordHash));
        PasswordChangedAt = DateTimeOffset.UtcNow;
        FailedAttempts = 0;
        LockedUntil = null;
        Touch(userId);
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        Touch(userId);
    }

    public void Activate(Guid? userId = null)
    {
        IsActive = true;
        FailedAttempts = 0;
        LockedUntil = null;
        Touch(userId);
    }

    private static string NormalizeUsername(string username)
    {
        var normalized = Guard.NotEmpty(username, nameof(username)).Trim().ToLowerInvariant();

        DomainException.ThrowIf(
            normalized.Length < 3,
            "O nome de usuario precisa ter ao menos 3 caracteres.");

        return Guard.MaxLength(normalized, 100, nameof(username));
    }
}
