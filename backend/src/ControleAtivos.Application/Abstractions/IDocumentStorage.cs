namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Guarda o conteudo binario dos anexos.
///
/// A interface existe para que o conteudo nunca passe pelo banco: o
/// PostgreSQL guarda apenas o metadado e a chave, enquanto os bytes ficam em
/// disco (ou, em producao, em um bucket). Um PDF de 20 MB por contrato
/// inflaria o banco, os backups e cada restauracao, sem nenhum ganho.
///
/// A chave e opaca e gerada aqui — nunca derivada do nome enviado pelo
/// usuario, que e dado nao confiavel.
/// </summary>
public interface IDocumentStorage
{
    /// <summary>Grava o conteudo e devolve a chave para recupera-lo.</summary>
    Task<string> SaveAsync(
        Guid tenantId,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre o conteudo para leitura. Devolve <c>null</c> quando a chave nao
    /// existe — o arquivo pode ter sido removido fora da aplicacao, e a API
    /// precisa responder 404 em vez de estourar.
    /// </summary>
    Task<Stream?> OpenAsync(
        Guid tenantId,
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o conteudo. Nao falha se a chave ja nao existir: o objetivo e
    /// que o arquivo deixe de existir, e ele ja nao existe.
    /// </summary>
    Task DeleteAsync(
        Guid tenantId,
        string storageKey,
        CancellationToken cancellationToken = default);
}
