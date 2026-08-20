using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Entities;

namespace ControleAtivos.Application.Features.Organization;

/// <summary>
/// Casos de uso da estrutura organizacional e das atribuicoes gerenciais.
/// </summary>
public sealed class OrganizationService(
    IOrganizationQueries queries,
    IOrganizationalUnitRepository organizationalUnits,
    IManagementAssignmentRepository assignments,
    IUserRepository users,
    IOrganizationalScopeResolver scopeResolver,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<OrganizationalUnitTreeDto>> GetTreeAsync(
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty ? [] : await queries.GetTreeAsync(scope, cancellationToken);
    }

    public async Task<IReadOnlyList<ManagementAssignmentDto>> ListAssignmentsAsync(
        Guid? userId,
        Guid? organizationalUnitId,
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? []
            : await queries.ListAssignmentsAsync(
                userId,
                organizationalUnitId,
                onlyActive,
                scope,
                cancellationToken);
    }

    public async Task<Guid> CreateUnitAsync(
        CreateOrganizationalUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await organizationalUnits.CodeExistsAsync(command.Code, cancellationToken))
        {
            throw new ConflictException($"Ja existe uma unidade com o codigo '{command.Code}'.");
        }

        OrganizationalUnit? parent = null;

        if (command.ParentId is { } parentId)
        {
            parent = await organizationalUnits.GetAsync(parentId, cancellationToken)
                ?? throw new NotFoundException("Unidade organizacional pai", parentId);

            var scope = await scopeResolver.GetAsync(cancellationToken);

            // Criar sob uma unidade exige acesso a ela: e o ponto de enxerto na arvore.
            if (!scope.CanAccess(parent.Id))
            {
                throw new ForbiddenException("Voce nao possui acesso a unidade organizacional pai.");
            }
        }

        var unit = new OrganizationalUnit(
            Guid.NewGuid(),
            tenantContext.TenantId,
            command.Type,
            command.Name,
            command.Code,
            parent);

        unit.Describe(command.Description, currentUser.UserId);

        organizationalUnits.Add(unit);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return unit.Id;
    }

    /// <summary>
    /// Cria um vinculo de gestao. Vinculos anteriores do mesmo usuario em
    /// outras unidades permanecem ativos — e assim que um gerente responde por
    /// mais de uma area ou nucleo.
    /// </summary>
    public async Task<Guid> CreateAssignmentAsync(
        CreateManagementAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException("Usuario", command.UserId);

        var unit = await organizationalUnits.GetAsync(command.OrganizationalUnitId, cancellationToken)
            ?? throw new NotFoundException("Unidade organizacional", command.OrganizationalUnitId);

        if (await assignments.HasActiveAssignmentAsync(
                user.Id,
                unit.Id,
                command.Role,
                cancellationToken))
        {
            throw new ConflictException(
                $"O usuario ja possui um vinculo ativo de '{command.Role}' nesta unidade.");
        }

        var assignment = new ManagementAssignment(
            Guid.NewGuid(),
            tenantContext.TenantId,
            user,
            unit,
            command.Role,
            command.StartDate,
            command.IsPrimary);

        if (!command.IncludesDescendants)
        {
            assignment.RestrictToOwnUnit(currentUser.UserId);
        }

        assignments.Add(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return assignment.Id;
    }

    public async Task FinishAssignmentAsync(
        Guid assignmentId,
        FinishManagementAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var assignment = await assignments.GetAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundException("Atribuicao gerencial", assignmentId);

        assignment.Finish(command.EndDate, command.Notes, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
    /// <summary>
    /// Detalhe de uma unidade. Fora do escopo do usuario responde como
    /// inexistente, para nao revelar a existencia de areas que ele nao ve.
    /// </summary>
    public async Task<OrganizationalUnitDetailDto> GetUnitAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return await queries.GetUnitDetailAsync(id, scope, cancellationToken)
            ?? throw new NotFoundException("Unidade organizacional", id);
    }

    /// <summary>Corrige o cadastro da unidade. Codigo e tipo nao mudam.</summary>
    public async Task UpdateUnitAsync(
        Guid id,
        UpdateOrganizationalUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var unit = await organizationalUnits.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException("Unidade organizacional", id);

        await EnsureCanWriteAsync(unit.Id, cancellationToken);

        unit.Rename(command.Name, currentUser.UserId);
        unit.Describe(command.Description, currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Remove a unidade em definitivo.
    ///
    /// Desativar e excluir resolvem problemas diferentes: desativar encerra uma
    /// area que existiu e mantem a estrutura de pe, com os ativos e o historico
    /// no lugar; excluir desfaz um cadastro criado por engano. Por isso a
    /// exclusao so acontece quando nada depende da unidade — e, quando algo
    /// depende, a mensagem diz exatamente o que.
    /// </summary>
    public async Task DeleteUnitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await organizationalUnits.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException("Unidade organizacional", id);

        await EnsureCanWriteAsync(unit.Id, cancellationToken);

        var blockers = await organizationalUnits.GetDeletionBlockersAsync(id, cancellationToken);

        if (blockers.Count > 0)
        {
            throw new ConflictException(
                $"Esta unidade nao pode ser excluida porque possui {string.Join(", ", blockers)}. " +
                "Para retira-la de uso preservando o historico, desative-a.");
        }

        organizationalUnits.Remove(unit);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanWriteAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        if (!scope.CanAccess(unitId))
        {
            throw new ForbiddenException("Voce nao possui acesso a esta unidade organizacional.");
        }
    }

}
