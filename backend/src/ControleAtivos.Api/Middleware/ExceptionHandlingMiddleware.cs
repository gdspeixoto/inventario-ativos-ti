using System.Diagnostics;
using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ControleAtivos.Api.Middleware;

/// <summary>
/// Converte excecoes em <see cref="ProblemDetails"/>.
///
/// Violacoes de invariante do dominio viram 422 com a mensagem original — ela e
/// escrita para o usuario. Qualquer outra excecao vira 500 com mensagem
/// generica: detalhes internos vao para o log, nunca para a resposta.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            logger.LogInformation(
                "Regra de dominio violada em {Path}: {Message}",
                context.Request.Path,
                exception.Message);

            await WriteProblemAsync(
                context,
                StatusCodes.Status422UnprocessableEntity,
                "Regra de negocio violada",
                exception.Message);
        }
        catch (NotFoundException exception)
        {
            /*
             * Recurso inexistente e recurso fora do escopo respondem igual.
             * Diferenciar revelaria a existencia de itens de outras unidades a
             * quem nao deveria enxerga-los.
             */
            await WriteProblemAsync(
                context,
                StatusCodes.Status404NotFound,
                "Recurso nao encontrado",
                exception.Message);
        }
        catch (ConflictException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Conflito",
                exception.Message);
        }
        catch (ForbiddenException exception)
        {
            logger.LogWarning(
                "Acesso negado em {Path} para o usuario {Subject}: {Message}",
                context.Request.Path,
                context.User.FindFirst("sub")?.Value,
                exception.Message);

            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Acesso negado",
                exception.Message);
        }
        catch (UnauthorizedAccessException)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Acesso negado",
                "Voce nao possui permissao para executar esta operacao.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Falha nao tratada em {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Erro interno",
                environment.IsDevelopment()
                    ? exception.Message
                    : "Ocorreu um erro ao processar a requisicao.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }
}
