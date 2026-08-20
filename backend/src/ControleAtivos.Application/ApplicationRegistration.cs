using ControleAtivos.Application.Features.Alerts;
using ControleAtivos.Application.Features.Contracts;
using ControleAtivos.Application.Features.Costs;
using ControleAtivos.Application.Features.Licenses;
using ControleAtivos.Application.Features.Documents;
using ControleAtivos.Application.Features.Organization;
using ControleAtivos.Application.Features.Servers;
using ControleAtivos.Application.Features.Suppliers;
using ControleAtivos.Application.Features.Timeline;
using Microsoft.Extensions.DependencyInjection;

namespace ControleAtivos.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<LicenseService>();
        services.AddScoped<ServerService>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<TimelineService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ContractService>();
        services.AddScoped<SupplierService>();
        services.AddScoped<CostService>();
        services.AddScoped<CostEntryService>();
        services.AddScoped<AlertService>();
        services.AddScoped<ReportService>();

        return services;
    }
}
