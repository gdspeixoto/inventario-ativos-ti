# Estrategia Para Nao Expor Tokens No Navegador

## Recomendacao

Implementar Backend For Frontend no backend .NET.

## Fluxo De Login

1. Frontend chama `GET /bff/login`.
2. Backend gera `state`, `nonce` e PKCE.
3. Backend redireciona para Keycloak.
4. Keycloak retorna para `GET /bff/callback` com `code`.
5. Backend troca `code` por tokens no token endpoint.
6. Backend armazena tokens em storage servidor ou sessao criptografada.
7. Backend cria cookie de sessao HttpOnly.
8. Frontend passa a chamar `/api/v1/*` com cookie automaticamente.

## Endpoints BFF

- `GET /bff/login`
- `GET /bff/callback`
- `POST /bff/logout`
- `GET /bff/user`
- `POST /bff/refresh`, se necessario
- `GET /bff/csrf`

## Cookies

- Nome: `__Host-controle-ativos-session`.
- `HttpOnly`.
- `Secure`.
- `SameSite=Lax` ou `Strict`.
- Path `/`.
- Sem `Domain` quando usar prefixo `__Host-`.

## CSRF

Para metodos `POST`, `PUT`, `PATCH`, `DELETE`:

- Enviar header `X-CSRF-TOKEN`.
- Validar token vinculado a sessao.
- Validar `Origin` e `Referer` quando presentes.

## Impacto No Frontend

O frontend deixa de precisar anexar Bearer token.

O `authInterceptor` deve ser adaptado para:

- Nao ler access token.
- Enviar requests com credenciais quando API estiver no mesmo dominio ou dominio confiavel.
- Redirecionar para `/bff/login` quando receber 401.

## Quando Usar Bearer Token

Bearer token deve ser usado apenas para:

- Integracoes servidor-servidor.
- Jobs internos.
- Clientes confiaveis fora do browser.
