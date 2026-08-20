using ControleAtivos.Domain.Common;

namespace ControleAtivos.Domain.Entities;

/// <summary>Centro de custo contabil ao qual as despesas sao rateadas.</summary>
public sealed class CostCenter : TenantEntity
{
    private CostCenter()
    {
    }

    public CostCenter(Guid id, Guid tenantId, string code, string name)
        : base(id, tenantId)
    {
        Code = Guard.MaxLength(Guard.NotEmpty(code, nameof(code)).ToUpperInvariant(), 40, nameof(code));
        Name = Guard.MaxLength(Guard.NotEmpty(name, nameof(name)), 200, nameof(name));
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid? OrganizationalUnitId { get; private set; }

    public OrganizationalUnit? OrganizationalUnit { get; private set; }

    public bool IsActive { get; private set; }

    public void LinkTo(OrganizationalUnit unit, Guid? userId = null)
    {
        DomainException.ThrowIf(unit.TenantId != TenantId, "A unidade pertence a outro tenant.");
        OrganizationalUnitId = unit.Id;
        OrganizationalUnit = unit;
        Touch(userId);
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        Touch(userId);
    }
}
