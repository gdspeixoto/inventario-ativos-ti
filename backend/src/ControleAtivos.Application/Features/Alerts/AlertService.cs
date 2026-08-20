using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;

namespace ControleAtivos.Application.Features.Alerts;

public sealed class AlertService(
    IAlertQueries queries,
    IAlertRepository alerts,
    IAlertScanner scanner,
    IOrganizationalScopeResolver scopeResolver,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<AlertDto>> ListAsync(
        ListAlertsQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? PagedResult<AlertDto>.Empty(query.Page, query.PageSize)
            : await queries.ListAsync(query, scope, cancellationToken);
    }

    public async Task ResolveAsync(
        Guid alertId,
        ResolveAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        var alert = await LoadInScopeAsync(alertId, cancellationToken);

        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Usuario nao identificado para encerrar o alerta.");

        alert.Resolve(userId, command.Notes);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task IgnoreAsync(
        Guid alertId,
        IgnoreAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        var alert = await LoadInScopeAsync(alertId, cancellationToken);

        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Usuario nao identificado para encerrar o alerta.");

        alert.Ignore(userId, command.Justification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Dispara a varredura de pendencias. Pode ser chamada por rotina agendada
    /// ou manualmente pela interface.
    /// </summary>
    public async Task<AlertScanResultDto> ScanAsync(
        AlertScanSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        if (scope.IsEmpty)
        {
            return new AlertScanResultDto(0, 0, 0, []);
        }

        var result = await scanner.ScanAsync(settings ?? new AlertScanSettings(), scope, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<Domain.Entities.Alert> LoadInScopeAsync(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        var alert = await alerts.GetAsync(alertId, cancellationToken)
            ?? throw new NotFoundException("Alerta", alertId);

        var scope = await scopeResolver.GetAsync(cancellationToken);

        // Fora do escopo responde 404, como nas demais consultas.
        if (!scope.CanAccess(alert.OrganizationalUnitId))
        {
            throw new NotFoundException("Alerta", alertId);
        }

        return alert;
    }
}
