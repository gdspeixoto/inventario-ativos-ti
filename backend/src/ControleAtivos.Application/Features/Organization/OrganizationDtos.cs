using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Organization;

/// <summary>No da arvore organizacional, com os filhos aninhados.</summary>
public sealed record OrganizationalUnitTreeDto(
    Guid Id,
    Guid? ParentId,
    OrganizationalUnitType Type,
    string Name,
    string Code,
    string Path,
    int Level,
    bool IsActive,
    int AssetCount,
    decimal MonthlyCost,
    IReadOnlyList<OrganizationalUnitTreeDto> Children);

/// <summary>
/// Detalhe de uma unidade organizacional.
///
/// Reune o que a arvore nao mostra: a cadeia de unidades acima, os
/// responsaveis, os numeros consolidados e — o que interessa a quem pensa em
/// excluir — a lista do que depende dela.
/// </summary>
public sealed record OrganizationalUnitDetailDto(
    Guid Id,
    Guid? ParentId,
    string? ParentName,
    OrganizationalUnitType Type,
    string Name,
    string Code,
    string Path,
    string? Description,
    int Level,
    bool IsActive,
    int AssetCount,
    int LicenseCount,
    int ServerCount,
    int ContractCount,
    int ChildrenCount,
    decimal MonthlyCost,
    decimal AnnualCost,
    IReadOnlyList<OrganizationalUnitBreadcrumbDto> Ancestors,
    IReadOnlyList<OrganizationalUnitTreeDto> Children,
    IReadOnlyList<ManagementAssignmentDto> Assignments,
    IReadOnlyList<string> DeletionBlockers);

/// <summary>Elo da cadeia hierarquica, para navegacao.</summary>
public sealed record OrganizationalUnitBreadcrumbDto(Guid Id, string Name, string Code);

public sealed record ManagementAssignmentDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string UserEmail,
    Guid OrganizationalUnitId,
    string OrganizationalUnitName,
    string OrganizationalUnitPath,
    OrganizationalUnitType OrganizationalUnitType,
    ManagementRole Role,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsPrimary,
    bool IncludesDescendants,
    bool IsActive);

public sealed record CreateOrganizationalUnitCommand
{
    public required OrganizationalUnitType Type { get; init; }

    public required string Name { get; init; }

    public required string Code { get; init; }

    public Guid? ParentId { get; init; }

    public string? Description { get; init; }
}

/// <summary>
/// Cria um vinculo de gestao. Um mesmo usuario pode receber varios vinculos,
/// em unidades diferentes, sem que os anteriores sejam encerrados.
/// </summary>
public sealed record CreateManagementAssignmentCommand
{
    public required Guid UserId { get; init; }

    public required Guid OrganizationalUnitId { get; init; }

    public required ManagementRole Role { get; init; }

    public required DateOnly StartDate { get; init; }

    public bool IsPrimary { get; init; }

    /// <summary>Quando falso, o escopo fica restrito a propria unidade.</summary>
    public bool IncludesDescendants { get; init; } = true;
}

public sealed record FinishManagementAssignmentCommand
{
    public required DateOnly EndDate { get; init; }

    public string? Notes { get; init; }
}

/// <summary>Correcao do cadastro de uma unidade organizacional.</summary>
public sealed record UpdateOrganizationalUnitCommand
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}
