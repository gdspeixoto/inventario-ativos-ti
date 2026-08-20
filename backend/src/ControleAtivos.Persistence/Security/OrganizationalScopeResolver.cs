using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Security;

/// <summary>
/// Calcula o escopo organizacional do usuario e o mantem em cache pelo tempo de
/// vida da requisicao — varios servicos consultam o escopo na mesma chamada, e
/// repetir a consulta a cada um seria desperdicio.
///
/// Regra: administradores e auditores enxergam o tenant inteiro; os demais
/// enxergam as unidades onde possuem atribuicao gerencial vigente, mais as
/// sub-arvores quando o vinculo permite descendentes, mais a lotacao.
/// </summary>
public sealed class OrganizationalScopeResolver(
    ControleAtivosDbContext dbContext,
    ICurrentUser currentUser) : IOrganizationalScopeResolver
{
    private IOrganizationalScope? _cached;

    public async Task<IOrganizationalScope> GetAsync(CancellationToken cancellationToken = default)
    {
        return _cached ??= await ResolveAsync(cancellationToken);
    }

    private async Task<IOrganizationalScope> ResolveAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return OrganizationalScope.Empty();
        }

        if (currentUser.HasPermission(Permissions.TenantAdmin) ||
            currentUser.HasPermission(Permissions.AuditRead))
        {
            return OrganizationalScope.Global();
        }

        if (currentUser.UserId is not { } userId)
        {
            return OrganizationalScope.Empty();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var assignments = await dbContext.ManagementAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
            .Select(a => new
            {
                a.OrganizationalUnitId,
                a.IncludesDescendants,
                UnitPath = a.OrganizationalUnit.Path,
            })
            .ToListAsync(cancellationToken);

        var primaryUnitId = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PrimaryOrganizationalUnitId)
            .FirstOrDefaultAsync(cancellationToken);

        var unitIds = assignments.Select(a => a.OrganizationalUnitId).ToHashSet();

        if (primaryUnitId is { } primary)
        {
            unitIds.Add(primary);
        }

        /*
         * Os prefixos de path alcancam a sub-arvore inteira em uma unica
         * comparacao de string. Os ids concretos ainda sao expandidos porque a
         * maioria dos filtros usa a chave estrangeira, nao o path.
         */
        var prefixes = assignments
            .Where(a => a.IncludesDescendants)
            .Select(a => a.UnitPath)
            .Distinct()
            .ToList();

        if (prefixes.Count > 0)
        {
            var descendantIds = await dbContext.OrganizationalUnits
                .AsNoTracking()
                .Where(u => prefixes.Any(prefix => u.Path.StartsWith(prefix)))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            foreach (var id in descendantIds)
            {
                unitIds.Add(id);
            }
        }

        return unitIds.Count == 0
            ? OrganizationalScope.Empty()
            : OrganizationalScope.Restricted(unitIds, prefixes);
    }
}
