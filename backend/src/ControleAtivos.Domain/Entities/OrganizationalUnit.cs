using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// No da arvore institucional (Contoso &gt; Negocios &gt; Areas Tecnologicas &gt;
/// Unidades Operacionais &gt; Nucleos de Suporte).
///
/// A modelagem combina <em>adjacency list</em> (<see cref="ParentId"/>) com um
/// <em>materialized path</em> (<see cref="Path"/>). O path permite responder
/// "todos os descendentes desta unidade" com um unico <c>LIKE 'prefixo%'</c>,
/// que e exatamente a pergunta feita em toda listagem quando o usuario tem
/// escopo sobre uma sub-arvore.
/// </summary>
public sealed class OrganizationalUnit : TenantEntity
{
    public const char PathSeparator = '/';

    private readonly List<OrganizationalUnit> _children = [];
    private readonly List<ManagementAssignment> _managementAssignments = [];

    private OrganizationalUnit()
    {
    }

    public OrganizationalUnit(
        Guid id,
        Guid tenantId,
        OrganizationalUnitType type,
        string name,
        string code,
        OrganizationalUnit? parent)
        : base(id, tenantId)
    {
        Type = type;
        Name = Guard.MaxLength(Guard.NotEmpty(name, nameof(name)), 200, nameof(name));
        Code = NormalizeCode(code);
        IsActive = true;

        AttachTo(parent);
    }

    public OrganizationalUnitType Type { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Codigo curto e unico dentro do tenant (ex.: <c>TI-INFRA</c>).</summary>
    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid? ParentId { get; private set; }

    public OrganizationalUnit? Parent { get; private set; }

    /// <summary>
    /// Caminho materializado, ex.: <c>/CONTOSO/NEG-TEC/AT-DIGITAL/UO-SISTEMAS/</c>.
    /// Sempre inicia e termina com o separador, o que evita que
    /// <c>LIKE '/A%'</c> case indevidamente com <c>/AB</c>.
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    public int Level { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<OrganizationalUnit> Children => _children.AsReadOnly();

    public IReadOnlyCollection<ManagementAssignment> ManagementAssignments =>
        _managementAssignments.AsReadOnly();

    public void Rename(string name, Guid? userId = null)
    {
        Name = Guard.MaxLength(Guard.NotEmpty(name, nameof(name)), 200, nameof(name));
        Touch(userId);
    }

    public void Describe(string? description, Guid? userId = null)
    {
        Description = Guard.Optional(description);
        Touch(userId);
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        Touch(userId);
    }

    public void MoveTo(OrganizationalUnit? parent, Guid? userId = null)
    {
        DomainException.ThrowIf(
            parent is not null && parent.Id == Id,
            "Uma unidade organizacional nao pode ser pai de si mesma.");

        DomainException.ThrowIf(
            parent is not null && parent.Path.Contains($"{PathSeparator}{Code}{PathSeparator}", StringComparison.Ordinal),
            "Movimentacao criaria um ciclo na arvore organizacional.");

        AttachTo(parent);
        Touch(userId);
    }

    /// <summary>Prefixo usado para consultar esta unidade e todos os descendentes.</summary>
    public string DescendantPathPrefix() => Path;

    private void AttachTo(OrganizationalUnit? parent)
    {
        if (parent is null)
        {
            DomainException.ThrowIf(
                Type is not (OrganizationalUnitType.Matriz or OrganizationalUnitType.Filial),
                "Somente Matriz ou Filial podem existir sem unidade pai.");

            ParentId = null;
            Parent = null;
            Level = 0;
            Path = $"{PathSeparator}{Code}{PathSeparator}";
            return;
        }

        DomainException.ThrowIf(
            parent.TenantId != TenantId,
            "A unidade pai pertence a outro tenant.");

        DomainException.ThrowIf(
            (int)Type <= (int)parent.Type,
            $"Uma unidade do tipo '{Type}' nao pode ficar abaixo de '{parent.Type}'.");

        ParentId = parent.Id;
        Parent = parent;
        Level = parent.Level + 1;
        Path = $"{parent.Path}{Code}{PathSeparator}";
    }

    private static string NormalizeCode(string code)
    {
        var normalized = Guard.NotEmpty(code, nameof(code)).Trim().ToUpperInvariant();

        DomainException.ThrowIf(
            normalized.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_'),
            "Codigo da unidade aceita apenas letras, numeros, hifen e underline.");

        return Guard.MaxLength(normalized, 40, nameof(code));
    }
}
