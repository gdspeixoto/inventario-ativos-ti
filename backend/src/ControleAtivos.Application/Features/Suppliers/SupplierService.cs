using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Entities;

namespace ControleAtivos.Application.Features.Suppliers;

public sealed class SupplierService(
    ISupplierQueries queries,
    ISupplierRepository suppliers,
    IOrganizationalScopeResolver scopeResolver,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<SupplierListItemDto>> ListAsync(
        ListSuppliersQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? PagedResult<SupplierListItemDto>.Empty(query.Page, query.PageSize)
            : await queries.ListAsync(query, scope, cancellationToken);
    }

    public async Task<SupplierDetailDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return await queries.GetAsync(id, scope, cancellationToken)
            ?? throw new NotFoundException("Fornecedor", id);
    }

    /// <summary>
    /// Fornecedores nao pertencem a uma unidade organizacional: sao do tenant
    /// inteiro, ja que o mesmo fornecedor atende varias areas.
    /// </summary>
    public async Task<Guid> CreateAsync(
        CreateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier(
            Guid.NewGuid(),
            tenantContext.TenantId,
            command.Name,
            command.DocumentNumber);

        supplier.UpdateContactInfo(
            command.MainContactName,
            command.Email,
            command.Phone,
            command.Website,
            currentUser.UserId);

        if (command.Category is { } category)
        {
            supplier.Classify(category, command.SlaDescription, currentUser.UserId);
        }

        suppliers.Add(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return supplier.Id;
    }

    public async Task UpdateAsync(
        Guid supplierId,
        UpdateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        var supplier = await suppliers.GetAsync(supplierId, cancellationToken)
            ?? throw new NotFoundException("Fornecedor", supplierId);

        supplier.UpdateContactInfo(
            command.MainContactName,
            command.Email,
            command.Phone,
            command.Website,
            currentUser.UserId);

        if (command.Category is { } category)
        {
            supplier.Classify(category, command.SlaDescription, currentUser.UserId);
        }

        if (command.Status is { } status)
        {
            supplier.ChangeStatus(status, currentUser.UserId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
    /// <summary>
    /// Remove o fornecedor em definitivo.
    ///
    /// So e possivel enquanto nenhum ativo, contrato ou lancamento apontar para
    /// ele. Havendo vinculo, o caminho e marcar como inativo: o fornecedor
    /// deixa de aparecer nas selecoes, mas os registros historicos continuam
    /// sabendo de quem se tratava.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await suppliers.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException("Fornecedor", id);

        if (await suppliers.HasLinkedRecordsAsync(id, cancellationToken))
        {
            throw new ConflictException(
                "Este fornecedor possui ativos, contratos ou lancamentos vinculados e nao pode " +
                "ser excluido. Para retira-lo de uso, altere a situacao para inativo.");
        }

        suppliers.Remove(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Corrige a razao social — util quando o nome foi digitado errado.</summary>
    public async Task RenameAsync(
        Guid id,
        string name,
        CancellationToken cancellationToken = default)
    {
        var supplier = await suppliers.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException("Fornecedor", id);

        supplier.Rename(name, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

}
