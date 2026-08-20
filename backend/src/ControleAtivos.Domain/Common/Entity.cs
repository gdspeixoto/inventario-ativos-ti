namespace ControleAtivos.Domain.Common;

/// <summary>
/// Raiz de toda entidade do dominio. A identidade e um GUID gerado pela
/// aplicacao (nao pelo banco) para permitir montar grafos de objetos antes de
/// persistir e para evitar IDs sequenciais enumeraveis nas rotas da API.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("O identificador da entidade nao pode ser vazio.");
        }

        Id = id;
    }

    /// <summary>Construtor exigido pelo EF Core.</summary>
    protected Entity()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Marca a entidade como alterada. Chamado pelos metodos de mutacao do
    /// dominio; o interceptor de auditoria complementa com o usuario atual.
    /// </summary>
    protected void Touch(Guid? userId = null)
    {
        UpdatedAt = DateTimeOffset.UtcNow;

        if (userId.HasValue)
        {
            UpdatedByUserId = userId;
        }
    }

    public void SetCreationAudit(Guid? userId)
    {
        CreatedByUserId ??= userId;
    }

    public void SetUpdateAudit(Guid? userId)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedByUserId = userId;
    }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
