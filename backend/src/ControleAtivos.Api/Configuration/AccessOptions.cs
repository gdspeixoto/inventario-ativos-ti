namespace ControleAtivos.Api.Configuration;

/// <summary>
/// Como o sistema resolve tenant e papeis quando o provedor de identidade nao
/// pode fornece-los.
///
/// O cenario que justifica estas opcoes: os usuarios vem federados do AD e nao
/// sao editaveis no Keycloak, entao nem a claim de tenant nem os papeis podem
/// ser atribuidos la. A autenticacao continua sendo do provedor corporativo; a
/// autorizacao passa a ser decidida aqui.
/// </summary>
public sealed class AccessOptions
{
    public const string SectionName = "Access";

    /// <summary>
    /// Tenant atribuido a quem autentica sem claim de tenant. So faz sentido
    /// em instalacao de organizacao unica; com varios tenants, deixe vazio e
    /// exija a claim, sob pena de misturar dados entre organizacoes.
    /// </summary>
    public string? DefaultTenantSlug { get; init; }

    /// <summary>
    /// Usuarios que recebem <c>controle-ativos-admin</c> automaticamente no
    /// primeiro acesso, identificados por e-mail ou pelo <c>sub</c> do token.
    ///
    /// Serve para o problema do primeiro administrador: sem isto, ninguem teria
    /// permissao para conceder permissao a ninguem. Depois que a equipe estiver
    /// configurada pela interface, esvazie a lista — ela e um atalho de
    /// inicializacao, nao um mecanismo de administracao.
    /// </summary>
    public string[] BootstrapAdmins { get; init; } = [];

    /// <summary>Credenciais locais de emergencia. Ver <see cref="LocalLoginOptions"/>.</summary>
    public LocalLoginOptions LocalLogin { get; init; } = new();
}

/// <summary>
/// Login por usuario e senha guardados no proprio sistema.
///
/// Existe para acesso <em>break-glass</em>: quando o SSO esta fora e ainda e
/// preciso entrar. Nao substitui o login corporativo — ele traz MFA e gestao
/// central de identidade, que este caminho nao tem.
/// </summary>
public sealed class LocalLoginOptions
{
    /// <summary>
    /// Desligado por padrao. Ligar em producao e uma decisao consciente, que
    /// deve vir acompanhada de <see cref="AllowedInProduction"/>.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Trava adicional: mesmo com <see cref="Enabled"/>, o login local so sobe
    /// em producao se isto for verdadeiro. Evita que uma configuracao copiada
    /// do ambiente de desenvolvimento abra o caminho sem que ninguem perceba.
    /// </summary>
    public bool AllowedInProduction { get; init; }

    /// <summary>Validade do token emitido pelo login local.</summary>
    public int TokenLifetimeMinutes { get; init; } = 60;

    /// <summary>
    /// Chave HMAC usada para assinar o token local. Precisa ter ao menos 32
    /// bytes e **nao** deve ser versionada: use variavel de ambiente ou cofre
    /// de segredos.
    /// </summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>
    /// Credenciais provisionadas na subida. Util para criar a conta de
    /// emergencia sem depender de uma interface que talvez nao esteja
    /// acessivel no momento da falha.
    /// </summary>
    public SeedCredential[] SeedCredentials { get; init; } = [];
}

/// <summary>Credencial local criada automaticamente na inicializacao.</summary>
public sealed class SeedCredential
{
    public string Username { get; init; } = string.Empty;

    /// <summary>Senha inicial em texto claro, lida da configuracao e imediatamente derivada.</summary>
    public string Password { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    /// <summary>Dias ate a credencial expirar. Zero ou negativo significa sem expiracao.</summary>
    public int ExpiresInDays { get; init; }
}
