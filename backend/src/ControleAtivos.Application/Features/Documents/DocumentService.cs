using System.Security.Cryptography;
using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Documents;

/// <summary>Metadado de um anexo. O conteudo vem por download separado.</summary>
public sealed record DocumentDto(
    Guid Id,
    TimelineEntityType EntityType,
    Guid EntityId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    string Sha256,
    string? Description,
    Guid UploadedByUserId,
    string? UploadedByName,
    DateTimeOffset CreatedAt);

/// <summary>Conteudo pronto para envio ao cliente.</summary>
public sealed record DocumentContent(Stream Content, string ContentType, string FileName);

public sealed record UploadDocumentCommand
{
    public required TimelineEntityType EntityType { get; init; }

    public required Guid EntityId { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required Stream Content { get; init; }

    public required long SizeInBytes { get; init; }

    public string? Description { get; init; }
}

/// <summary>
/// Anexos de contratos, ativos e fornecedores.
///
/// O conteudo binario nao passa pelo banco: fica no storage, e aqui guarda-se
/// o metadado com a chave e o hash. O hash e calculado no servidor, sobre os
/// bytes efetivamente gravados — aceita-lo do cliente tornaria a verificacao de
/// integridade decorativa, porque um upload corrompido chegaria acompanhado do
/// hash do arquivo corrompido.
/// </summary>
public sealed class DocumentService(
    IDocumentRepository documents,
    IDocumentStorage storage,
    IUserRepository users,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
{
    public async Task<Guid> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Usuario nao identificado.");

        // O hash sai dos bytes que serao gravados, e o stream volta ao inicio
        // para que o storage grave o arquivo inteiro.
        var sha256 = await ComputeHashAsync(command.Content, cancellationToken);

        if (command.Content.CanSeek)
        {
            command.Content.Seek(0, SeekOrigin.Begin);
        }

        var storageKey = await storage.SaveAsync(
            tenantContext.TenantId,
            command.Content,
            cancellationToken);

        try
        {
            // A entidade valida tamanho, tipo e nome. Se recusar, o arquivo ja
            // gravado precisa sair do storage — senao acumula lixo orfao.
            var document = new Document(
                Guid.NewGuid(),
                tenantContext.TenantId,
                command.EntityType,
                command.EntityId,
                command.FileName,
                command.ContentType,
                command.SizeInBytes,
                storageKey,
                sha256,
                userId);

            document.Describe(command.Description);

            documents.Add(document);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return document.Id;
        }
        catch
        {
            await storage.DeleteAsync(tenantContext.TenantId, storageKey, CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<DocumentDto>> ListAsync(
        TimelineEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var items = await documents.ListAsync(entityType, entityId, cancellationToken);

        if (items.Count == 0)
        {
            return [];
        }

        var uploaderIds = items.Select(d => d.UploadedByUserId).Distinct().ToList();
        var names = await users.GetDisplayNamesAsync(uploaderIds, cancellationToken);

        return items
            .Select(d => new DocumentDto(
                d.Id,
                d.EntityType,
                d.EntityId,
                d.FileName,
                d.ContentType,
                d.SizeInBytes,
                d.Sha256,
                d.Description,
                d.UploadedByUserId,
                names.GetValueOrDefault(d.UploadedByUserId),
                d.CreatedAt))
            .ToList();
    }

    /// <summary>
    /// Abre o conteudo para download.
    ///
    /// Quando o metadado existe mas o arquivo sumiu do storage, responde como
    /// inexistente: o usuario nao tem o que fazer com a distincao, e insistir
    /// num erro de servidor esconderia que o anexo simplesmente nao esta la.
    /// </summary>
    public async Task<DocumentContent> DownloadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var document = await documents.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException("Documento", id);

        var content = await storage.OpenAsync(
            tenantContext.TenantId,
            document.StorageKey,
            cancellationToken)
            ?? throw new NotFoundException("Documento", id);

        return new DocumentContent(content, document.ContentType, document.FileName);
    }

    /// <summary>
    /// Remove o anexo.
    ///
    /// O metadado sai primeiro; o arquivo depois. Na ordem inversa, uma falha
    /// no meio deixaria um registro apontando para um arquivo inexistente —
    /// e a tela mostraria um anexo que nao abre.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await documents.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException("Documento", id);

        var storageKey = document.StorageKey;

        documents.Remove(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await storage.DeleteAsync(tenantContext.TenantId, storageKey, cancellationToken);
    }

    private static async Task<string> ComputeHashAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(content, cancellationToken);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
