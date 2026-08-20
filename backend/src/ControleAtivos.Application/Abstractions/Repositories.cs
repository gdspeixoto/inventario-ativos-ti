using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Acesso as entidades para escrita. As consultas de leitura ficam nos
/// handlers, que projetam direto para DTO — carregar o agregado inteiro apenas
/// para montar uma listagem seria desperdicio.
/// </summary>
public interface IAssetRepository
{
    Task<LicenseAsset?> GetLicenseAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServerAsset?> GetServerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca o ativo sem distinguir licenca de servidor. Usado quando a
    /// operacao vale para qualquer tipo — vincular um custo, por exemplo.
    /// </summary>
    Task<Asset?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    void Add(Asset asset);

    void Remove(Asset asset);

    /// <summary>
    /// Se o ativo tem lancamentos de custo. Um ativo com historia financeira
    /// nao deve ser apagado — apenas desativado.
    /// </summary>
    Task<bool> HasCostEntriesAsync(Guid assetId, CancellationToken cancellationToken = default);
}

public interface IOrganizationalUnitRepository
{
    Task<OrganizationalUnit?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrganizationalUnit?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    void Add(OrganizationalUnit unit);

    void Remove(OrganizationalUnit unit);

    /// <summary>
    /// O que impede a exclusao da unidade, em linguagem de negocio. Lista
    /// vazia significa que ela pode ser removida.
    ///
    /// A checagem e feita antes de tentar o DELETE para que o usuario receba
    /// "existem 3 ativos vinculados" em vez de um erro de chave estrangeira.
    /// </summary>
    Task<IReadOnlyList<string>> GetDeletionBlockersAsync(
        Guid unitId,
        CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<AppUser?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByExternalSubjectAsync(string subject, CancellationToken cancellationToken = default);

    void Add(AppUser user);

    /// <summary>
    /// Nomes de exibicao por id, em uma consulta so. Resolver um a um geraria
    /// N+1 ao montar listas que mostram quem enviou cada anexo.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}

/// <summary>Metadados de anexos.</summary>
public interface IDocumentRepository
{
    Task<Document?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> ListAsync(
        TimelineEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    void Add(Document document);

    void Remove(Document document);
}

public interface ISupplierRepository
{
    void Remove(Supplier supplier);

    Task<bool> HasLinkedRecordsAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Supplier supplier);
}

public interface IContractRepository
{
    void Remove(Contract contract);

    Task<bool> HasLinkedRecordsAsync(Guid contractId, CancellationToken cancellationToken = default);

    Task<Contract?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken = default);

    void Add(Contract contract);
}

public interface ICostCenterRepository
{
    Task<CostCenter?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(CostCenter costCenter);
}

public interface IPriceAdjustmentRepository
{
    void Add(PriceAdjustment adjustment);
}

public interface ICostEntryRepository
{
    void Add(CostEntry entry);
}

public interface IManagementAssignmentRepository
{
    Task<ManagementAssignment?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasActiveAssignmentAsync(
        Guid userId,
        Guid organizationalUnitId,
        Domain.Enums.ManagementRole role,
        CancellationToken cancellationToken = default);

    void Add(ManagementAssignment assignment);
}
