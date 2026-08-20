using ControleAtivos.Application.Abstractions;

namespace ControleAtivos.Application.Common;

/// <summary>Implementacao imutavel de <see cref="IOrganizationalScope"/>.</summary>
public sealed class OrganizationalScope : IOrganizationalScope
{
    private OrganizationalScope(
        bool hasGlobalScope,
        IReadOnlySet<Guid> unitIds,
        IReadOnlyCollection<string> pathPrefixes)
    {
        HasGlobalScope = hasGlobalScope;
        UnitIds = unitIds;
        PathPrefixes = pathPrefixes;
    }

    public bool HasGlobalScope { get; }

    public IReadOnlySet<Guid> UnitIds { get; }

    public IReadOnlyCollection<string> PathPrefixes { get; }

    public bool IsEmpty => !HasGlobalScope && UnitIds.Count == 0;

    public static OrganizationalScope Global() => new(true, new HashSet<Guid>(), []);

    public static OrganizationalScope Restricted(
        IReadOnlySet<Guid> unitIds,
        IReadOnlyCollection<string> pathPrefixes) => new(false, unitIds, pathPrefixes);

    public static OrganizationalScope Empty() => new(false, new HashSet<Guid>(), []);

    public bool CanAccess(Guid organizationalUnitId) =>
        HasGlobalScope || UnitIds.Contains(organizationalUnitId);

    public bool CanAccessPath(string organizationalUnitPath) =>
        HasGlobalScope ||
        PathPrefixes.Any(prefix => organizationalUnitPath.StartsWith(prefix, StringComparison.Ordinal));
}
