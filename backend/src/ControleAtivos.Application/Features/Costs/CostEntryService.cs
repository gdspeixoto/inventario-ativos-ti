using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Application.Features.Costs;

/// <summary>
/// Lancamentos de custo realizados.
///
/// Existe uma diferenca importante entre este registro e o valor mensal do
/// ativo: o ativo declara quanto <em>deveria</em> custar de forma recorrente,
/// enquanto o lancamento registra o que foi efetivamente gasto numa
/// competencia. Sem ele, despesas pontuais — uma consultoria, uma migracao,
/// um excedente de consumo em nuvem — nao teriam onde ser registradas, e a
/// diferenca entre orcado e realizado ficaria invisivel.
/// </summary>
public sealed class CostEntryService(
    ICostQueries queries,
    ICostEntryRepository entries,
    IOrganizationalUnitRepository organizationalUnits,
    IAssetRepository assets,
    IContractRepository contracts,
    IOrganizationalScopeResolver scopeResolver,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<CostEntryDto>> ListAsync(
        ListCostEntriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? PagedResult<CostEntryDto>.Empty(query.Page, query.PageSize)
            : await queries.ListEntriesAsync(query, scope, cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        CreateCostEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var unit = await organizationalUnits.GetAsync(command.OrganizationalUnitId, cancellationToken)
            ?? throw new NotFoundException("Unidade organizacional", command.OrganizationalUnitId);

        await EnsureCanWriteAsync(unit.Id, cancellationToken);

        var entry = new CostEntry(
            Guid.NewGuid(),
            unit.TenantId,
            unit.Id,
            command.Type,
            command.Category,
            new Money(command.Amount, command.Currency),
            command.CompetenceMonth,
            command.Description);

        /*
         * O vinculo com ativo ou contrato traz fornecedor e centro de custo
         * junto. Herdar essa classificacao da origem evita que a mesma despesa
         * apareca agrupada de formas diferentes conforme quem a lancou.
         */
        if (command.AssetId is { } assetId)
        {
            var asset = await assets.GetAsync(assetId, cancellationToken)
                ?? throw new NotFoundException("Ativo", assetId);

            await EnsureCanWriteAsync(asset.OrganizationalUnitId, cancellationToken);

            entry.LinkToAsset(asset);
        }

        if (command.ContractId is { } contractId)
        {
            var contract = await contracts.GetAsync(contractId, cancellationToken)
                ?? throw new NotFoundException("Contrato", contractId);

            await EnsureCanWriteAsync(contract.OrganizationalUnitId, cancellationToken);

            entry.LinkToContract(contract);
        }

        entries.Add(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }

    private async Task EnsureCanWriteAsync(Guid organizationalUnitId, CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        if (!scope.CanAccess(organizationalUnitId))
        {
            throw new ForbiddenException(
                "Voce nao possui acesso a unidade organizacional deste lancamento.");
        }
    }
}
