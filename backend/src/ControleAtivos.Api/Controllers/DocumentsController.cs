using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Features.Documents;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControleAtivos.Api.Controllers;

/// <summary>
/// Anexos de contratos, ativos e fornecedores.
///
/// O conteudo trafega como multipart e nunca entra no banco: o PostgreSQL
/// guarda o metadado, o storage guarda os bytes.
/// </summary>
[ApiController]
[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentsController(DocumentService documents) : ControllerBase
{
    /// <summary>
    /// Anexa um arquivo a uma entidade.
    ///
    /// O limite de tamanho e declarado aqui alem de validado no dominio: sem
    /// ele o ASP.NET leria os bytes inteiros antes de qualquer verificacao, e
    /// um envio de 500 MB consumiria memoria ate a validacao que o recusaria.
    /// </summary>
    [HttpPost]
    [Authorize(Permissions.DocumentsWrite)]
    [EnableRateLimiting("writes")]
    [RequestSizeLimit(Document.MaxSizeInBytes + 4096)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromForm] TimelineEntityType entityType,
        [FromForm] Guid entityId,
        IFormFile file,
        [FromForm] string? description,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Arquivo obrigatorio",
                Detail = "Envie um arquivo com conteudo.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        await using var content = file.OpenReadStream();

        var id = await documents.UploadAsync(
            new UploadDocumentCommand
            {
                EntityType = entityType,
                EntityId = entityId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Content = content,
                SizeInBytes = file.Length,
                Description = description,
            },
            cancellationToken);

        return CreatedAtAction(nameof(Download), new { id }, new { id });
    }

    [HttpGet]
    [Authorize(Permissions.DocumentsRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<DocumentDto>> List(
        [FromQuery] TimelineEntityType entityType,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken) =>
        documents.ListAsync(entityType, entityId, cancellationToken);

    /// <summary>
    /// Baixa o conteudo.
    ///
    /// O nome original volta no cabecalho, mas o arquivo e sempre servido como
    /// anexo: deixar o navegador renderizar um HTML ou SVG enviado por um
    /// usuario permitiria executar script no dominio da aplicacao.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Permissions.DocumentsRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var document = await documents.DownloadAsync(id, cancellationToken);

        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.DocumentsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await documents.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
