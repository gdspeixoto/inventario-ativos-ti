using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Licenses;
using ControleAtivos.Application.Features.Servers;
using ControleAtivos.Application.Features.Timeline;

namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Consultas de leitura, projetadas direto para DTO.
///
/// Ficam separadas dos repositorios de escrita porque a necessidade e outra:
/// listagens precisam de projecao, paginacao e joins seletivos, nao do agregado
/// completo com rastreamento de mudancas.
///
/// Toda implementacao deve aplicar o <see cref="IOrganizationalScope"/> recebido
/// — o filtro de tenant ja vem do <c>DbContext</c>, mas o escopo organizacional
/// e responsabilidade da consulta.
/// </summary>
public interface IAssetQueries
{
    Task<PagedResult<LicenseListItemDto>> ListLicensesAsync(
        ListLicensesQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<LicenseDetailDto?> GetLicenseAsync(
        Guid id,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ServerListItemDto>> ListServersAsync(
        ListServersQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<ServerDetailDto?> GetServerAsync(
        Guid id,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}

public interface ITimelineQueries
{
    Task<PagedResult<TimelineEventDto>> ListAsync(
        ListTimelineQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}

public interface IDashboardQueries
{
    Task<Features.Dashboard.DashboardSummaryDto> GetSummaryAsync(
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}

public interface IOrganizationQueries
{
    Task<IReadOnlyList<Features.Organization.OrganizationalUnitTreeDto>> GetTreeAsync(
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<Features.Organization.OrganizationalUnitDetailDto?> GetUnitDetailAsync(
        Guid unitId,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Features.Organization.ManagementAssignmentDto>> ListAssignmentsAsync(
        Guid? userId,
        Guid? organizationalUnitId,
        bool onlyActive,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}
