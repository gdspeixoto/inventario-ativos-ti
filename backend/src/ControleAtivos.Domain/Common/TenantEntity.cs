namespace ControleAtivos.Domain.Common;

/// <summary>
/// Entidade pertencente a um tenant. Toda tabela de negocio deriva daqui, o que
/// permite ao <c>DbContext</c> aplicar o filtro global de isolamento sem que
/// cada consulta precise lembrar de filtrar.
/// </summary>
public abstract class TenantEntity : Entity
{
    protected TenantEntity(Guid id, Guid tenantId)
        : base(id)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("A entidade precisa pertencer a um tenant.");
        }

        TenantId = tenantId;
    }

    protected TenantEntity()
    {
    }

    public Guid TenantId { get; private set; }
}
