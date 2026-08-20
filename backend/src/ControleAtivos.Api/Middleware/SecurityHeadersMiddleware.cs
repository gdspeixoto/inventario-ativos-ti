namespace ControleAtivos.Api.Middleware;

/// <summary>
/// Aplica cabecalhos de seguranca e remove os que revelam a stack.
/// A API responde JSON, entao a CSP e restritiva por padrao — nao ha script
/// legitimo a ser carregado a partir de uma resposta desta aplicacao.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
        headers["Cross-Origin-Resource-Policy"] = "same-site";
        headers["Cache-Control"] = "no-store";

        headers["Content-Security-Policy"] =
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        await next(context);
    }
}
