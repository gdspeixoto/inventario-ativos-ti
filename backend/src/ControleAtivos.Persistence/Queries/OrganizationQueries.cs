using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Features.Organization;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

public sealed class OrganizationQueries(ControleAtivosDbContext context) : IOrganizationQueries
{
    public async Task<IReadOnlyList<OrganizationalUnitTreeDto>> GetTreeAsync(
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var query = context.OrganizationalUnits.AsNoTracking();

        if (!scope.HasGlobalScope)
        {
            if (scope.IsEmpty)
            {
                return [];
            }

            var unitIds = scope.UnitIds.ToList();
            query = query.Where(u => unitIds.Contains(u.Id));
        }

        /*
         * Custos e contagens vem agregados por unidade em uma consulta separada.
         * Fazer isso com subquery por no geraria N+1 numa arvore de cinco niveis.
         */
        var aggregates = await context.Assets
            .AsNoTracking()
            .Where(a => a.Status == AssetStatus.Ativo)
            .GroupBy(a => a.OrganizationalUnitId)
            .Select(g => new
            {
                UnitId = g.Key,
                Count = g.Count(),
                MonthlyCost = g.Sum(a => a.MonthlyAmount.Amount),
            })
            .ToDictionaryAsync(x => x.UnitId, cancellationToken);

        var units = await query
            .OrderBy(u => u.Path)
            .Select(u => new
            {
                u.Id,
                u.ParentId,
                u.Type,
                u.Name,
                u.Code,
                u.Path,
                u.Level,
                u.IsActive,
            })
            .ToListAsync(cancellationToken);

        var byParent = units.ToLookup(u => u.ParentId);

        List<OrganizationalUnitTreeDto> Build(Guid? parentId) =>
            byParent[parentId]
                .Select(u => new OrganizationalUnitTreeDto(
                    u.Id,
                    u.ParentId,
                    u.Type,
                    u.Name,
                    u.Code,
                    u.Path,
                    u.Level,
                    u.IsActive,
                    aggregates.TryGetValue(u.Id, out var agg) ? agg.Count : 0,
                    aggregates.TryGetValue(u.Id, out var cost) ? cost.MonthlyCost : 0m,
                    Build(u.Id)))
                .ToList();

        /*
         * As raizes da resposta sao os nos cujo pai nao esta no conjunto
         * visivel — assim um gerente de area recebe a propria area como raiz,
         * em vez de uma lista vazia por nao enxergar a matriz.
         */
        var visibleIds = units.Select(u => u.Id).ToHashSet();

        return units
            .Where(u => u.ParentId is null || !visibleIds.Contains(u.ParentId.Value))
            .Select(u => new OrganizationalUnitTreeDto(
                u.Id,
                u.ParentId,
                u.Type,
                u.Name,
                u.Code,
                u.Path,
                u.Level,
                u.IsActive,
                aggregates.TryGetValue(u.Id, out var agg) ? agg.Count : 0,
                aggregates.TryGetValue(u.Id, out var cost) ? cost.MonthlyCost : 0m,
                Build(u.Id)))
            .ToList();
    }

    /// <summary>
    /// Detalhe de uma unidade, com o que a arvore nao mostra.
    ///
    /// Os impedimentos de exclusao vem junto porque a tela precisa deles antes
    /// de oferecer o botao: mostrar "Excluir" e so entao revelar que existem
    /// tres unidades subordinadas faria o usuario percorrer o caminho inteiro
    /// para descobrir que ele nao existia.
    /// </summary>
    public async Task<OrganizationalUnitDetailDto?> GetUnitDetailAsync(
        Guid unitId,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        if (!scope.CanAccess(unitId))
        {
            // Fora do escopo responde como inexistente: dizer "sem permissao"
            // confirmaria que a unidade existe.
            return null;
        }

        var unit = await context.OrganizationalUnits
            .AsNoTracking()
            .Where(u => u.Id == unitId)
            .Select(u => new
            {
                u.Id,
                u.ParentId,
                u.Type,
                u.Name,
                u.Code,
                u.Path,
                u.Description,
                u.Level,
                u.IsActive,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (unit is null)
        {
            return null;
        }

        // A cadeia acima sai do proprio caminho materializado, sem consulta
        // recursiva: o Path ja carrega os codigos dos ancestrais em ordem.
        var ancestorCodes = unit.Path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipLast(1)
            .ToList();

        var ancestors = ancestorCodes.Count == 0
            ? []
            : await context.OrganizationalUnits
                .AsNoTracking()
                .Where(u => ancestorCodes.Contains(u.Code))
                .Select(u => new { u.Id, u.Name, u.Code, u.Level })
                .OrderBy(u => u.Level)
                .ToListAsync(cancellationToken);

        var assets = await context.Assets
            .AsNoTracking()
            .Where(a => a.OrganizationalUnitId == unitId && a.Status == AssetStatus.Ativo)
            .GroupBy(a => a.Kind)
            .Select(g => new { Kind = g.Key, Count = g.Count(), Monthly = g.Sum(a => a.MonthlyAmount.Amount) })
            .ToListAsync(cancellationToken);

        var children = await context.OrganizationalUnits
            .AsNoTracking()
            .Where(u => u.ParentId == unitId)
            .OrderBy(u => u.Name)
            .Select(u => new OrganizationalUnitTreeDto(
                u.Id,
                u.ParentId,
                u.Type,
                u.Name,
                u.Code,
                u.Path,
                u.Level,
                u.IsActive,
                0,
                0m,
                new List<OrganizationalUnitTreeDto>()))
            .ToListAsync(cancellationToken);

        var contractCount = await context.Contracts
            .AsNoTracking()
            .CountAsync(c => c.OrganizationalUnitId == unitId, cancellationToken);

        var assignments = await ListAssignmentsAsync(null, unitId, true, scope, cancellationToken);

        var monthly = assets.Sum(a => a.Monthly);

        var blockers = new List<string>();

        if (children.Count > 0)
        {
            blockers.Add($"{children.Count} unidade(s) subordinada(s)");
        }

        var assetCount = assets.Sum(a => a.Count);

        if (assetCount > 0)
        {
            blockers.Add($"{assetCount} ativo(s) vinculado(s)");
        }

        if (contractCount > 0)
        {
            blockers.Add($"{contractCount} contrato(s) vinculado(s)");
        }

        if (assignments.Count > 0)
        {
            blockers.Add($"{assignments.Count} vinculo(s) de gestao");
        }

        return new OrganizationalUnitDetailDto(
            unit.Id,
            unit.ParentId,
            ancestors.LastOrDefault()?.Name,
            unit.Type,
            unit.Name,
            unit.Code,
            unit.Path,
            unit.Description,
            unit.Level,
            unit.IsActive,
            assetCount,
            assets.FirstOrDefault(a => a.Kind == AssetKind.License)?.Count ?? 0,
            assets.FirstOrDefault(a => a.Kind == AssetKind.Server)?.Count ?? 0,
            contractCount,
            children.Count,
            monthly,
            monthly * 12,
            ancestors.Select(a => new OrganizationalUnitBreadcrumbDto(a.Id, a.Name, a.Code)).ToList(),
            children,
            assignments,
            blockers);
    }

    public async Task<IReadOnlyList<ManagementAssignmentDto>> ListAssignmentsAsync(
        Guid? userId,
        Guid? organizationalUnitId,
        bool onlyActive,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var query = context.ManagementAssignments.AsNoTracking();

        if (!scope.HasGlobalScope)
        {
            if (scope.IsEmpty)
            {
                return [];
            }

            var unitIds = scope.UnitIds.ToList();
            query = query.Where(a => unitIds.Contains(a.OrganizationalUnitId));
        }

        if (userId is { } user)
        {
            query = query.Where(a => a.UserId == user);
        }

        if (organizationalUnitId is { } unit)
        {
            query = query.Where(a => a.OrganizationalUnitId == unit);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (onlyActive)
        {
            query = query.Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today));
        }

        return await query
            .OrderBy(a => a.OrganizationalUnit.Path)
            .ThenBy(a => a.User.DisplayName)
            .Select(a => new ManagementAssignmentDto(
                a.Id,
                a.UserId,
                a.User.DisplayName,
                a.User.Email,
                a.OrganizationalUnitId,
                a.OrganizationalUnit.Name,
                a.OrganizationalUnit.Path,
                a.OrganizationalUnit.Type,
                a.Role,
                a.StartDate,
                a.EndDate,
                a.IsPrimary,
                a.IncludesDescendants,
                a.StartDate <= today && (a.EndDate == null || a.EndDate >= today)))
            .ToListAsync(cancellationToken);
    }
}
