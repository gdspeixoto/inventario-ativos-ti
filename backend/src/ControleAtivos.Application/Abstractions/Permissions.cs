namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Permissoes reconhecidas pelo backend. As policies do ASP.NET Core sao
/// registradas a partir desta lista; o frontend usa os mesmos nomes apenas para
/// esconder botoes, o que e conveniencia visual e nao controle de acesso.
/// </summary>
public static class Permissions
{
    public const string AssetsRead = "assets.read";
    public const string AssetsWrite = "assets.write";
    public const string AssetsApproveFinancialChange = "assets.approve-financial";

    public const string ContractsRead = "contracts.read";
    public const string ContractsWrite = "contracts.write";

    public const string SuppliersRead = "suppliers.read";
    public const string SuppliersWrite = "suppliers.write";

    public const string CostsRead = "costs.read";
    public const string CostsWrite = "costs.write";

    public const string AlertsRead = "alerts.read";
    public const string AlertsWrite = "alerts.write";

    public const string ReportsRead = "reports.read";
    public const string AuditRead = "audit.read";

    /// <summary>
    /// Anexos. Separado de contratos porque documento acompanha tambem ativos e
    /// fornecedores — e porque anexar arquivo e uma capacidade que pode ser
    /// concedida sem dar direito de alterar o contrato em si.
    /// </summary>
    public const string DocumentsRead = "documents.read";

    public const string DocumentsWrite = "documents.write";

    public const string OrganizationRead = "organization.read";
    public const string OrganizationWrite = "organization.write";

    public const string TenantAdmin = "tenant.admin";

    public static readonly IReadOnlyList<string> All =
    [
        AssetsRead,
        AssetsWrite,
        AssetsApproveFinancialChange,
        ContractsRead,
        ContractsWrite,
        SuppliersRead,
        SuppliersWrite,
        CostsRead,
        CostsWrite,
        AlertsRead,
        AlertsWrite,
        ReportsRead,
        AuditRead,
        DocumentsRead,
        DocumentsWrite,
        OrganizationRead,
        OrganizationWrite,
        TenantAdmin,
    ];
}

/// <summary>
/// Papeis do Keycloak e o conjunto de permissoes que cada um concede.
/// Manter o mapa no backend evita depender de o realm estar perfeitamente
/// configurado para que a autorizacao funcione.
/// </summary>
public static class Roles
{
    public const string TenantAdmin = "controle-ativos-admin";
    public const string ItManager = "controle-ativos-gestor-ti";
    public const string BusinessManager = "controle-ativos-gerente";
    public const string ItAnalyst = "controle-ativos-analista";
    public const string Finance = "controle-ativos-financeiro";
    public const string Auditor = "controle-ativos-auditor";

    public static IReadOnlySet<string> PermissionsFor(string role) => role switch
    {
        TenantAdmin => Permissions.All.ToHashSet(),

        ItManager => new HashSet<string>
        {
            Permissions.AssetsRead, Permissions.AssetsWrite, Permissions.AssetsApproveFinancialChange,
            Permissions.ContractsRead, Permissions.ContractsWrite,
            Permissions.SuppliersRead, Permissions.SuppliersWrite,
            Permissions.CostsRead, Permissions.CostsWrite,
            Permissions.AlertsRead, Permissions.AlertsWrite,
            Permissions.ReportsRead, Permissions.OrganizationRead,
            Permissions.DocumentsRead, Permissions.DocumentsWrite,
        },

        BusinessManager => new HashSet<string>
        {
            Permissions.AssetsRead, Permissions.ContractsRead, Permissions.SuppliersRead,
            Permissions.CostsRead, Permissions.AlertsRead, Permissions.AlertsWrite,
            Permissions.ReportsRead, Permissions.OrganizationRead,
            Permissions.AssetsApproveFinancialChange,
            Permissions.DocumentsRead,
        },

        ItAnalyst => new HashSet<string>
        {
            Permissions.AssetsRead, Permissions.AssetsWrite,
            Permissions.ContractsRead, Permissions.SuppliersRead,
            Permissions.CostsRead, Permissions.AlertsRead, Permissions.AlertsWrite,
            Permissions.OrganizationRead,
            Permissions.DocumentsRead, Permissions.DocumentsWrite,
        },

        Finance => new HashSet<string>
        {
            Permissions.AssetsRead, Permissions.ContractsRead, Permissions.ContractsWrite,
            Permissions.SuppliersRead, Permissions.CostsRead, Permissions.CostsWrite,
            Permissions.ReportsRead, Permissions.AlertsRead, Permissions.OrganizationRead,
            Permissions.DocumentsRead, Permissions.DocumentsWrite,
        },

        Auditor => new HashSet<string>
        {
            Permissions.AssetsRead, Permissions.ContractsRead, Permissions.SuppliersRead,
            Permissions.CostsRead, Permissions.AlertsRead, Permissions.ReportsRead,
            Permissions.AuditRead, Permissions.OrganizationRead,
            Permissions.DocumentsRead,
        },

        _ => new HashSet<string>(),
    };
}
