using ControleAtivos.Domain.Common;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Instancia logica isolada do sistema. Nao deriva de <see cref="TenantEntity"/>
/// porque e a propria fronteira de isolamento.
/// </summary>
public sealed class Tenant : Entity
{
    private Tenant()
    {
    }

    public Tenant(Guid id, string name, string slug)
        : base(id)
    {
        Name = Guard.NotEmpty(name, nameof(name));
        Slug = NormalizeSlug(slug);
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Identificador estavel usado em URLs, claims e logs.</summary>
    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void Rename(string name, Guid? userId = null)
    {
        Name = Guard.NotEmpty(name, nameof(name));
        Touch(userId);
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        Touch(userId);
    }

    public void Activate(Guid? userId = null)
    {
        IsActive = true;
        Touch(userId);
    }

    private static string NormalizeSlug(string slug)
    {
        var normalized = Guard.NotEmpty(slug, nameof(slug)).Trim().ToLowerInvariant();
        DomainException.ThrowIf(
            normalized.Any(c => !char.IsLetterOrDigit(c) && c != '-'),
            "Slug do tenant aceita apenas letras, numeros e hifen.");
        return normalized;
    }
}
