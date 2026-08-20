using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using FluentAssertions;

namespace ControleAtivos.UnitTests;

/// <summary>
/// Validacoes do metadado de anexo.
///
/// Limite de tamanho, tipo permitido e saneamento do nome ja vivem na
/// entidade; estes testes fixam esse comportamento.
/// </summary>
public sealed class DocumentTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Aceita_pdf_dentro_do_limite()
    {
        var document = Build("contrato.pdf", "application/pdf", 1024);

        document.FileName.Should().Be("contrato.pdf");
        document.SizeInBytes.Should().Be(1024);
    }

    [Fact]
    public void Recusa_arquivo_vazio()
    {
        var act = () => Build("vazio.pdf", "application/pdf", 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Recusa_arquivo_acima_do_limite()
    {
        var act = () => Build("grande.pdf", "application/pdf", Document.MaxSizeInBytes + 1);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("application/x-msdownload")]
    [InlineData("text/html")]
    [InlineData("image/svg+xml")]
    public void Recusa_tipo_nao_permitido(string contentType)
    {
        // Executáveis e formatos que o navegador renderiza ficam de fora: um
        // HTML ou SVG servido do domínio da aplicação executaria script nele.
        var act = () => Build("arquivo", contentType, 512);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("../../../etc/passwd", "passwd")]
    [InlineData("/var/log/syslog", "syslog")]
    [InlineData("pasta/documento.pdf", "documento.pdf")]
    public void Remove_diretorios_do_nome_informado(string sent, string expected)
    {
        var document = Build(sent, "application/pdf", 128);

        document.FileName.Should().Be(expected);
    }

    private static Document Build(string fileName, string contentType, long size) =>
        new(
            Guid.NewGuid(),
            TenantId,
            TimelineEntityType.Contract,
            Guid.NewGuid(),
            fileName,
            contentType,
            size,
            storageKey: Guid.NewGuid().ToString("N"),
            sha256: new string('a', 64),
            uploadedByUserId: Guid.NewGuid());
}
