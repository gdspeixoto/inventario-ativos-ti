# Checklist De Seguranca Backend

## Autenticacao

- Validar emissor (`iss`).
- Validar audiencia (`aud`).
- Validar assinatura.
- Validar expiracao (`exp`).
- Rejeitar tokens sem tenant/organizacao quando endpoint exigir tenant.
- Preferir BFF com cookies HttpOnly para nao expor tokens ao JavaScript.

## Cookies BFF

- `HttpOnly=true`.
- `Secure=true`.
- `SameSite=Lax` ou `Strict`.
- Nome com prefixo `__Host-` quando possivel.
- Sessao curta e renovacao controlada.
- CSRF token para metodos mutaveis.

## API

- Rate limiting por rota.
- Request body size limit.
- CORS apenas para origens conhecidas.
- Validacao de entrada com FluentValidation.
- DTOs especificos por operacao.
- Nunca aceitar campos de auditoria do cliente.
- Nunca aceitar `TenantId` do corpo como fonte de verdade.
- Retornar `ProblemDetails` sem stack trace em producao.

## Banco

- Usuario da aplicacao com menor privilegio possivel.
- Migrations controladas.
- Queries parametrizadas.
- RLS por tenant quando possivel.
- Indices por `tenant_id` e chaves de busca.
- Backup criptografado.

## Logs

- Logar `CorrelationId`.
- Nao logar tokens.
- Nao logar cookies.
- Nao logar senhas.
- Mascarar documentos e dados pessoais quando necessario.
- Registrar alteracoes sensiveis em `AuditLog`.

## Headers

- `Strict-Transport-Security`.
- `X-Content-Type-Options: nosniff`.
- `Referrer-Policy: no-referrer` ou `strict-origin-when-cross-origin`.
- `Content-Security-Policy`.
- `Permissions-Policy`.
- Remover headers que revelem tecnologia quando possivel.
