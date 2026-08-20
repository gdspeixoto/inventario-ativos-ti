using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Seed;

/// <summary>
/// Popula a base com dados de demonstracao em desenvolvimento.
///
/// Reproduz a estrutura institucional real (Contoso &gt; Negocios &gt; Areas
/// Tecnologicas &gt; Unidades Operacionais &gt; Nucleos) e inclui um gerente
/// atribuido a duas areas, para exercitar o caso que motivou a modelagem de
/// <see cref="ManagementAssignment"/> como entidade propria.
///
/// Nao roda em producao e nao faz nada se o tenant ja existir.
/// </summary>
public static class DevelopmentSeeder
{
    public static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public const string DemoTenantSlug = "contoso";

    public static async Task SeedAsync(
        ControleAtivosDbContext context,
        CancellationToken cancellationToken = default)
    {
        if (await context.Tenants.IgnoreQueryFilters()
                .AnyAsync(t => t.Id == DemoTenantId, cancellationToken))
        {
            return;
        }

        var tenant = new Tenant(DemoTenantId, "Contoso Holding", DemoTenantSlug);
        context.Tenants.Add(tenant);

        // --- Estrutura organizacional ---
        var matriz = Unit(OrganizationalUnitType.Matriz, "Contoso Holding", "CONTOSO", null);
        var negTec = Unit(OrganizationalUnitType.Negocio, "Negocios de Tecnologia", "NEG-TEC", matriz);
        var negEdu = Unit(OrganizationalUnitType.Negocio, "Educacao", "NEG-EDU", matriz);

        var atDigital = Unit(
            OrganizationalUnitType.AreaTecnologica, "Transformacao Digital", "AT-DIGITAL", negTec);
        var atManufatura = Unit(
            OrganizationalUnitType.AreaTecnologica, "Manufatura Avancada", "AT-MANUF", negTec);
        var atEnsino = Unit(
            OrganizationalUnitType.AreaTecnologica, "Tecnologia Educacional", "AT-ENSINO", negEdu);

        var uoSistemas = Unit(
            OrganizationalUnitType.UnidadeOperacional, "Desenvolvimento de Sistemas", "UO-SIST", atDigital);
        var uoDados = Unit(
            OrganizationalUnitType.UnidadeOperacional, "Engenharia de Dados", "UO-DADOS", atDigital);
        var uoRobotica = Unit(
            OrganizationalUnitType.UnidadeOperacional, "Robotica Industrial", "UO-ROBO", atManufatura);

        var nsInfra = Unit(
            OrganizationalUnitType.NucleoSuporte, "Nucleo de Infraestrutura", "NS-INFRA", uoSistemas);
        var nsSuporte = Unit(
            OrganizationalUnitType.NucleoSuporte, "Nucleo de Suporte ao Usuario", "NS-SUP", uoSistemas);

        context.OrganizationalUnits.AddRange(
            matriz, negTec, negEdu, atDigital, atManufatura, atEnsino,
            uoSistemas, uoDados, uoRobotica, nsInfra, nsSuporte);

        // --- Usuarios ---
        var ana = User("ana.souza", "Ana Souza", "ana.souza@example.com", "Gerente de TI");
        var carlos = User("carlos.lima", "Carlos Lima", "carlos.lima@example.com", "Coordenador de Infra");
        var marina = User("marina.rocha", "Marina Rocha", "marina.rocha@example.com", "Analista de TI");
        var paulo = User("paulo.dias", "Paulo Dias", "paulo.dias@example.com", "Analista Financeiro");

        ana.AssignPrimaryUnit(atDigital);
        carlos.AssignPrimaryUnit(nsInfra);
        marina.AssignPrimaryUnit(uoSistemas);
        paulo.AssignPrimaryUnit(matriz);

        context.Users.AddRange(ana, carlos, marina, paulo);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1);

        /*
         * Ana gerencia duas areas tecnologicas ao mesmo tempo. E exatamente o
         * cenario que impede modelar o gerente como campo unico na unidade.
         */
        context.ManagementAssignments.AddRange(
            new ManagementAssignment(
                Guid.NewGuid(), DemoTenantId, ana, atDigital, ManagementRole.Gerente, start, true),
            new ManagementAssignment(
                Guid.NewGuid(), DemoTenantId, ana, atManufatura, ManagementRole.Gerente, start),
            new ManagementAssignment(
                Guid.NewGuid(), DemoTenantId, carlos, nsInfra, ManagementRole.Coordenador, start, true),
            new ManagementAssignment(
                Guid.NewGuid(), DemoTenantId, carlos, nsSuporte, ManagementRole.Coordenador, start),
            new ManagementAssignment(
                Guid.NewGuid(), DemoTenantId, marina, uoSistemas, ManagementRole.ResponsavelTecnico, start),
            new ManagementAssignment(
                Guid.NewGuid(), DemoTenantId, paulo, matriz, ManagementRole.ResponsavelFinanceiro, start));

        // --- Centros de custo ---
        var ccTi = CostCenter("CC-TI-001", "Tecnologia da Informacao");
        var ccInfra = CostCenter("CC-TI-002", "Infraestrutura e Cloud");
        var ccEdu = CostCenter("CC-EDU-001", "Tecnologia Educacional");

        ccTi.LinkTo(atDigital);
        ccInfra.LinkTo(nsInfra);
        ccEdu.LinkTo(atEnsino);

        context.CostCenters.AddRange(ccTi, ccInfra, ccEdu);

        // --- Fornecedores ---
        var microsoft = Supplier("Microsoft CSP Brasil", "12345678000190", ContractCategory.Licenciamento);
        var azure = Supplier("Azure Cloud Services", "12345678000271", ContractCategory.Cloud);
        var dell = Supplier("Dell Technologies", "72381189000110", ContractCategory.Infraestrutura);
        var veeam = Supplier("Veeam Partner Brasil", "45678912000133", ContractCategory.Backup);
        var infraPro = Supplier("Consultoria InfraPro", "98765432000155", ContractCategory.Consultoria);

        context.Suppliers.AddRange(microsoft, azure, dell, veeam, infraPro);

        // --- Contratos ---
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var ctMicrosoft = new Contract(
            Guid.NewGuid(), DemoTenantId, microsoft, atDigital,
            "CT-2025-001", "Licenciamento Microsoft CSP", ContractCategory.Licenciamento,
            today.AddMonths(-10), today.AddDays(25), new Money(24_500m));

        var ctAzure = new Contract(
            Guid.NewGuid(), DemoTenantId, azure, nsInfra,
            "CT-2025-002", "Infraestrutura Azure", ContractCategory.Cloud,
            today.AddMonths(-8), today.AddDays(75), new Money(18_200m));

        var ctVeeam = new Contract(
            Guid.NewGuid(), DemoTenantId, veeam, nsInfra,
            "CT-2025-003", "Backup corporativo Veeam", ContractCategory.Backup,
            today.AddMonths(-14), today.AddDays(160), new Money(3_400m));

        var ctDell = new Contract(
            Guid.NewGuid(), DemoTenantId, dell, uoRobotica,
            "CT-2024-011", "Suporte de hardware Dell", ContractCategory.Suporte,
            today.AddMonths(-20), today.AddDays(210), new Money(5_900m));

        ctMicrosoft.AssignResponsible(ana);
        ctAzure.AssignResponsible(carlos);
        ctVeeam.AssignResponsible(carlos);
        ctDell.AssignResponsible(marina);

        context.Contracts.AddRange(ctMicrosoft, ctAzure, ctVeeam, ctDell);

        // --- Licencas ---
        var licencas = new[]
        {
            License("Microsoft 365 Business Premium", "LIC-M365-BP", "Microsoft 365 Business Premium",
                atDigital, 120, 104, 105m, microsoft, ctMicrosoft, ccTi, ana, paulo, 25),
            License("Microsoft 365 E3", "LIC-M365-E3", "Microsoft 365 E3",
                uoSistemas, 35, 32, 185m, microsoft, ctMicrosoft, ccTi, marina, paulo, 25),
            License("Power BI Pro", "LIC-PBI-PRO", "Power BI Pro",
                uoDados, 45, 18, 59.90m, microsoft, ctMicrosoft, ccTi, marina, paulo, 40),
            License("Exchange Online Plan 2", "LIC-EXO-P2", "Exchange Online Plan 2",
                atDigital, 80, 76, 48m, microsoft, ctMicrosoft, ccTi, carlos, paulo, 25),
            License("Project Plan 3", "LIC-PRJ-P3", "Project Plan 3",
                uoSistemas, 12, 9, 165m, microsoft, ctMicrosoft, ccTi, marina, paulo, 60),
            License("SQL Server Standard", "LIC-SQL-STD", "SQL Server Standard 2022",
                nsInfra, 4, 4, 2_100m, microsoft, ctMicrosoft, ccInfra, carlos, paulo, 90),
            License("Visio Plan 2", "LIC-VIS-P2", "Visio Plan 2",
                atManufatura, 15, 6, 79m, microsoft, ctMicrosoft, ccTi, marina, paulo, 120),
        };

        context.Assets.AddRange(licencas);

        // --- Servidores ---
        var servidores = new[]
        {
            Server("APP-PRD-01", "SRV-APP-PRD-01", "app-prd-01.contoso.local", ServerType.Virtual,
                ServerEnvironment.Producao, 1_120m, nsInfra, azure, ctAzure, ccInfra, carlos,
                "Ubuntu Server", "22.04", 8, 32, 500, hoursSinceBackup: 6),
            Server("DB-PRD-01", "SRV-DB-PRD-01", "db-prd-01.contoso.local", ServerType.BancoDados,
                ServerEnvironment.Producao, 3_850m, nsInfra, azure, ctAzure, ccInfra, carlos,
                "Windows Server", "2022", 16, 64, 2000, hoursSinceBackup: 10),
            Server("AD-01", "SRV-AD-01", "ad-01.contoso.local", ServerType.Virtual,
                ServerEnvironment.Producao, 920m, nsInfra, azure, ctAzure, ccInfra, carlos,
                "Windows Server", "2019", 4, 16, 300, hoursSinceBackup: 200),
            Server("BKP-01", "SRV-BKP-01", "bkp-01.contoso.local", ServerType.Backup,
                ServerEnvironment.Backup, 760m, nsInfra, veeam, ctVeeam, ccInfra, carlos,
                "Linux", "Debian 12", 4, 16, 8000, hoursSinceBackup: 18),
            Server("HML-WEB-01", "SRV-HML-WEB-01", "hml-web-01.contoso.local", ServerType.Aplicacao,
                ServerEnvironment.Homologacao, 410m, uoSistemas, azure, ctAzure, ccTi, marina,
                "Ubuntu Server", "22.04", 4, 8, 200, hoursSinceBackup: null),
            Server("ROBO-SIM-01", "SRV-ROBO-SIM-01", "robo-sim-01.contoso.local", ServerType.Fisico,
                ServerEnvironment.Desenvolvimento, 2_300m, uoRobotica, dell, ctDell, null, null,
                "Windows Server", "2022", 32, 128, 4000, hoursSinceBackup: null),
        };

        context.Assets.AddRange(servidores);

        await context.SaveChangesAsync(cancellationToken);

        // --- Historico financeiro ---
        // Feito depois do primeiro save para que os ativos ja tenham identidade.
        await SeedFinancialHistoryAsync(context, licencas[0], ana.Id, paulo.Id, cancellationToken);
        await SeedCostEntriesAsync(context, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return;

        OrganizationalUnit Unit(
            OrganizationalUnitType type,
            string name,
            string code,
            OrganizationalUnit? parent) =>
            new(Guid.NewGuid(), DemoTenantId, type, name, code, parent);

        AppUser User(string subject, string name, string email, string title)
        {
            var user = new AppUser(Guid.NewGuid(), DemoTenantId, subject, name, email);
            user.SyncFromIdentityProvider(name, email, title);
            return user;
        }

        CostCenter CostCenter(string code, string name) =>
            new(Guid.NewGuid(), DemoTenantId, code, name);

        Supplier Supplier(string name, string document, ContractCategory category)
        {
            var supplier = new Supplier(Guid.NewGuid(), DemoTenantId, name, document);
            supplier.Classify(category, "SLA de 8 horas uteis para chamados criticos.");
            supplier.UpdateContactInfo(
                "Atendimento Corporativo",
                $"contato@{name.Split(' ')[0].ToLowerInvariant()}.com.br",
                "(71) 3000-0000",
                null);
            return supplier;
        }

        LicenseAsset License(
            string name,
            string code,
            string product,
            OrganizationalUnit unit,
            int contracted,
            int used,
            decimal unitPrice,
            Supplier supplier,
            Contract contract,
            CostCenter costCenter,
            AppUser? technical,
            AppUser? financial,
            int renewalInDays)
        {
            var license = new LicenseAsset(
                Guid.NewGuid(), DemoTenantId, unit, name, code, "Microsoft", product,
                contracted, new Money(unitPrice));

            license.UpdateUsage(used);
            license.DefinePlan(null, BillingType.Mensal);
            license.DefineValidity(today.AddMonths(-10), today.AddDays(renewalInDays));
            license.LinkToSupplier(supplier);
            license.LinkToContract(contract);
            license.AssignCostCenter(costCenter);

            if (technical is not null || financial is not null)
            {
                license.AssignResponsibles(technical, financial);
            }

            return license;
        }

        ServerAsset Server(
            string name,
            string code,
            string hostname,
            ServerType type,
            ServerEnvironment environment,
            decimal infrastructure,
            OrganizationalUnit unit,
            Supplier supplier,
            Contract contract,
            CostCenter? costCenter,
            AppUser? technical,
            string os,
            string osVersion,
            int cpu,
            int memory,
            int storage,
            int? hoursSinceBackup)
        {
            var server = new ServerAsset(
                Guid.NewGuid(), DemoTenantId, unit, name, code, hostname, type, environment,
                new Money(infrastructure));

            server.DefinePlatform(os, osVersion, supplier.Name, "Brazil South", null);
            server.Resize(cpu, memory, storage, "Configuracao inicial no cadastro");
            server.LinkToSupplier(supplier);
            server.LinkToContract(contract);

            if (costCenter is not null)
            {
                server.AssignCostCenter(costCenter);
            }

            if (technical is not null)
            {
                server.AssignResponsibles(technical, null);
            }

            server.DefineBackup(
                hoursSinceBackup is null ? null : "Diario as 02:00",
                hoursSinceBackup is null ? null : DateTimeOffset.UtcNow.AddHours(-hoursSinceBackup.Value));

            server.DefineOperations("99,5% de disponibilidade", "Domingos, 02:00 as 06:00");

            server.UpdateCostBreakdown(
                new Money(infrastructure),
                new Money(Math.Round(infrastructure * 0.15m, 2)),
                new Money(Math.Round(infrastructure * 0.08m, 2)),
                new Money(Math.Round(infrastructure * 0.05m, 2)),
                "Composicao inicial de custos");

            return server;
        }
    }

    /// <summary>
    /// Aplica reajustes reais para que a timeline e o historico financeiro
    /// tenham conteudo desde o primeiro acesso.
    /// </summary>
    private static async Task SeedFinancialHistoryAsync(
        ControleAtivosDbContext context,
        LicenseAsset license,
        Guid createdBy,
        Guid approvedBy,
        CancellationToken cancellationToken)
    {
        var tracked = await context.Licenses.FirstAsync(l => l.Id == license.Id, cancellationToken);

        var adjustment = tracked.ApplyPriceAdjustment(
            tracked.MonthlyAmount.Multiply(1.1413m),
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
            "Reajuste anual do fornecedor apos negociacao comercial",
            AdjustmentIndex.NegociacaoManual,
            createdBy,
            approvedBy);

        context.PriceAdjustments.Add(adjustment);
    }

    /// <summary>Gera 12 meses de lancamentos para alimentar o grafico de evolucao.</summary>
    private static async Task SeedCostEntriesAsync(
        ControleAtivosDbContext context,
        CancellationToken cancellationToken)
    {
        var assets = await context.Assets
            .Select(a => new { a.Id, a.OrganizationalUnitId, a.Kind, a.SupplierId, a.CostCenterId,
                Amount = a.MonthlyAmount.Amount })
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var offset = 11; offset >= 0; offset--)
        {
            var competence = new DateOnly(today.Year, today.Month, 1).AddMonths(-offset);

            foreach (var asset in assets)
            {
                /*
                 * Aplica uma leve inflacao retroativa: meses mais antigos custam
                 * menos, o que torna a curva de evolucao legivel na demonstracao.
                 */
                var factor = 1m - offset * 0.008m;

                var entry = new CostEntry(
                    Guid.NewGuid(),
                    DemoTenantId,
                    asset.OrganizationalUnitId,
                    CostType.Mensal,
                    asset.Kind == AssetKind.License ? CostCategory.Licenciamento : CostCategory.Cloud,
                    new Money(Math.Round(asset.Amount * factor, 2)),
                    competence,
                    $"Competencia {competence:MM/yyyy}");

                context.CostEntries.Add(entry);
            }
        }
    }
}
