using ControleAtivos.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace ControleAtivos.Persistence.Storage;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>Raiz onde os anexos sao gravados.</summary>
    public string RootPath { get; init; } = "storage/documents";
}

/// <summary>
/// Armazenamento em disco.
///
/// Suficiente para uma instalacao unica e substituivel por S3 ou Azure Blob
/// sem tocar no restante do sistema — foi para isso que a interface existe.
///
/// Duas decisoes de seguranca vivem aqui:
///
/// A chave e um GUID gerado pelo servidor, nao o nome do arquivo. Nome enviado
/// por usuario e dado nao confiavel: usa-lo como caminho abriria path traversal
/// e permitiria que dois envios se sobrescrevessem.
///
/// Os arquivos ficam sob um diretorio por tenant, e a chave e validada antes
/// de compor o caminho. Assim uma chave adulterada nao consegue escapar da
/// pasta do proprio tenant nem alcancar a de outro.
/// </summary>
public sealed class FileSystemDocumentStorage : IDocumentStorage
{
    private readonly string _rootPath;

    public FileSystemDocumentStorage(IOptions<DocumentStorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Guid tenantId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var storageKey = Guid.NewGuid().ToString("N");
        var path = ResolvePath(tenantId, storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);

        return storageKey;
    }

    public Task<Stream?> OpenAsync(
        Guid tenantId,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(tenantId, storageKey);

        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    public Task DeleteAsync(
        Guid tenantId,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(tenantId, storageKey);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Compoe o caminho a partir do tenant e da chave, recusando qualquer
    /// chave que nao seja hexadecimal.
    ///
    /// A validacao vem antes da composicao de proposito: uma chave contendo
    /// <c>../</c> escaparia do diretorio do tenant e alcancaria arquivos de
    /// outro — ou do proprio sistema operacional.
    /// </summary>
    private string ResolvePath(Guid tenantId, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey.Length != 32
            || !storageKey.All(Uri.IsHexDigit))
        {
            throw new InvalidOperationException("Chave de armazenamento invalida.");
        }

        var path = Path.GetFullPath(Path.Combine(_rootPath, tenantId.ToString("N"), storageKey));

        // Rede de seguranca: mesmo com a chave validada, o resultado precisa
        // continuar dentro da raiz configurada.
        if (!path.StartsWith(_rootPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Chave de armazenamento invalida.");
        }

        return path;
    }
}
