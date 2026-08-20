using ControleAtivos.Application.Abstractions;
using ControleAtivos.Persistence.Queries;
using ControleAtivos.Persistence.Repositories;
using ControleAtivos.Persistence.Security;
using ControleAtivos.Persistence.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ControleAtivos.Persistence;

public static class PersistenceRegistration
{
    /// <summary>
    /// Registra repositorios, consultas e o resolvedor de escopo.
    ///
    /// Tudo scoped: os componentes dependem do <c>DbContext</c> e do usuario da
    /// requisicao. Um singleton aqui vazaria dados entre requisicoes.
    /// </summary>
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationalScopeResolver, OrganizationalScopeResolver>();

        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IOrganizationalUnitRepository, OrganizationalUnitRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<ICostCenterRepository, CostCenterRepository>();
        services.AddScoped<IPriceAdjustmentRepository, PriceAdjustmentRepository>();
        services.AddScoped<ICostEntryRepository, CostEntryRepository>();
        services.AddScoped<IManagementAssignmentRepository, ManagementAssignmentRepository>();

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        /*
         * O storage e singleton porque nao depende do DbContext nem do usuario
         * da requisicao: ele so escreve e le bytes, e a separacao por tenant
         * vem no parametro de cada chamada.
         */
        services.AddSingleton<IDocumentStorage, FileSystemDocumentStorage>();

        services.AddScoped<IAssetQueries, AssetQueries>();
        services.AddScoped<ITimelineQueries, TimelineQueries>();
        services.AddScoped<IOrganizationQueries, OrganizationQueries>();
        services.AddScoped<IDashboardQueries, DashboardQueries>();
        services.AddScoped<IContractQueries, ContractQueries>();
        services.AddScoped<ISupplierQueries, SupplierQueries>();
        services.AddScoped<ICostQueries, CostQueries>();
        services.AddScoped<IAlertQueries, AlertQueries>();
        services.AddScoped<IReportQueries, ReportQueries>();
        services.AddScoped<IAlertScanner, AlertScanner>();

        return services;
    }
}
