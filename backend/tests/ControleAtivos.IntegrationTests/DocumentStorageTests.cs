using System.Text;
using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ControleAtivos.IntegrationTests;

/// <summary>
/// Armazenamento de anexos.
///
/// O risco aqui nao e funcional, e de seguranca: o nome do arquivo vem do
/// usuario, e tratar esse dado como caminho permitiria ler ou sobrescrever
/// arquivos fora da pasta do tenant. Estes testes existem para que a
/// proteccao nao seja removida por engano numa refatoracao futura.
/// </summary>
public sealed class DocumentStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"ca-docs-{Guid.NewGuid():N}");

    private readonly FileSystemDocumentStorage _storage;

    public DocumentStorageTests()
    {
        _storage = new FileSystemDocumentStorage(
            Options.Create(new DocumentStorageOptions { RootPath = _root }));
    }

    [Fact]
    public async Task Grava_e_recupera_o_conteudo()
    {
        var tenant = Guid.NewGuid();
        var key = await _storage.SaveAsync(tenant, Content("contrato assinado"));

        await using var stream = await _storage.OpenAsync(tenant, key);
        stream.Should().NotBeNull();

        using var reader = new StreamReader(stream!);
        (await reader.ReadToEndAsync()).Should().Be("contrato assinado");
    }

    [Fact]
    public async Task A_chave_nao_deriva_do_nome_do_arquivo()
    {
        var key = await _storage.SaveAsync(Guid.NewGuid(), Content("x"));

        // Chave opaca: derivá-la do nome permitiria adivinhar o caminho de
        // anexos alheios e faria dois envios homônimos se sobrescreverem.
        key.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public async Task Cada_envio_recebe_uma_chave_distinta()
    {
        var tenant = Guid.NewGuid();

        var first = await _storage.SaveAsync(tenant, Content("primeiro"));
        var second = await _storage.SaveAsync(tenant, Content("segundo"));

        first.Should().NotBe(second);
    }

    [Fact]
    public async Task Um_tenant_nao_alcanca_o_arquivo_de_outro()
    {
        var owner = Guid.NewGuid();
        var intruder = Guid.NewGuid();

        var key = await _storage.SaveAsync(owner, Content("confidencial"));

        // Mesma chave, tenant diferente: o arquivo mora sob o diretório do
        // dono, então o caminho composto simplesmente não existe.
        var stream = await _storage.OpenAsync(intruder, key);

        stream.Should().BeNull();
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..")]
    [InlineData("/etc/passwd")]
    [InlineData("chave com espaco")]
    [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    [InlineData("")]
    public async Task Chave_adulterada_e_recusada(string key)
    {
        var act = async () => await _storage.OpenAsync(Guid.NewGuid(), key);

        // Sem esta recusa, `../` escaparia da pasta do tenant e alcançaria
        // arquivos de outro — ou do próprio sistema operacional.
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Remover_arquivo_inexistente_nao_falha()
    {
        // O objetivo é que o arquivo deixe de existir. Ele já não existe.
        var act = async () => await _storage.DeleteAsync(Guid.NewGuid(), new string('a', 32));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Remove_o_conteudo()
    {
        var tenant = Guid.NewGuid();
        var key = await _storage.SaveAsync(tenant, Content("temporario"));

        await _storage.DeleteAsync(tenant, key);

        (await _storage.OpenAsync(tenant, key)).Should().BeNull();
    }

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
