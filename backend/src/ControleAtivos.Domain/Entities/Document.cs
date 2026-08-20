using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Metadado de um arquivo anexado (contrato, proposta, nota fiscal, aditivo).
/// O conteudo binario fica fora do banco; aqui guarda-se apenas a referencia,
/// o hash para verificacao de integridade e a procedencia.
/// </summary>
public sealed class Document : TenantEntity
{
    private Document()
    {
    }

    public Document(
        Guid id,
        Guid tenantId,
        TimelineEntityType entityType,
        Guid entityId,
        string fileName,
        string contentType,
        long sizeInBytes,
        string storageKey,
        string sha256,
        Guid uploadedByUserId)
        : base(id, tenantId)
    {
        DomainException.ThrowIf(sizeInBytes <= 0, "Arquivo vazio nao pode ser anexado.");
        DomainException.ThrowIf(
            sizeInBytes > MaxSizeInBytes,
            $"Arquivo excede o limite de {MaxSizeInBytes / 1024 / 1024} MB.");
        DomainException.ThrowIf(
            !AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase),
            $"Tipo de arquivo nao permitido: {contentType}.");

        EntityType = entityType;
        EntityId = Guard.NotEmpty(entityId, nameof(entityId));
        FileName = Guard.MaxLength(SanitizeFileName(fileName), 255, nameof(fileName));
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        StorageKey = Guard.NotEmpty(storageKey, nameof(storageKey));
        Sha256 = Guard.NotEmpty(sha256, nameof(sha256));
        UploadedByUserId = uploadedByUserId;
    }

    public const long MaxSizeInBytes = 25 * 1024 * 1024;

    public static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/png",
        "image/jpeg",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/csv",
    ];

    public TimelineEntityType EntityType { get; private set; }

    public Guid EntityId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeInBytes { get; private set; }

    /// <summary>Chave opaca no storage. Nunca derivada do nome enviado pelo usuario.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public string Sha256 { get; private set; } = string.Empty;

    public Guid UploadedByUserId { get; private set; }

    public string? Description { get; private set; }

    public void Describe(string? description)
    {
        Description = Guard.Optional(description);
        Touch();
    }

    /// <summary>
    /// Remove diretorios e caracteres de controle do nome informado pelo
    /// usuario, evitando path traversal na exibicao e no download.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var name = Guard.NotEmpty(fileName, nameof(fileName));
        name = Path.GetFileName(name);

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c) && !char.IsControl(c)).ToArray());

        DomainException.ThrowIf(string.IsNullOrWhiteSpace(sanitized), "Nome de arquivo invalido.");
        return sanitized;
    }
}
