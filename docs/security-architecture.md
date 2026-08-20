# Arquitetura De Seguranca

## Principio Central

O backend e a unica fronteira de seguranca confiavel. O frontend apenas melhora experiencia; nunca decide autorizacao final.

## Autenticacao Recomendada

Preferencia: arquitetura BFF.

Fluxo:

1. Browser acessa frontend.
2. Login redireciona para Keycloak.
3. Backend/BFF troca authorization code por tokens usando canal servidor-servidor.
4. Tokens ficam armazenados apenas no servidor ou em storage seguro backend.
5. Browser recebe apenas cookie de sessao `HttpOnly`, `Secure`, `SameSite=Lax` ou `Strict`.
6. Chamadas para API usam cookie; JS nao acessa tokens.

Vantagem: reduz risco de roubo de token por XSS, extensoes maliciosas e interceptacao no contexto do navegador.

Alternativa aceitavel se BFF nao for implementado inicialmente: Authorization Code + PKCE no SPA, usando `sessionStorage` ou memoria, nunca `localStorage`, com CSP forte. Esta alternativa ainda expoe token ao runtime JavaScript e deve ser tratada como fase temporaria.

## Controles Obrigatorios No Backend

- HTTPS obrigatorio.
- HSTS em producao.
- CORS restrito por origem conhecida.
- CSRF protection quando usar cookies.
- Rate limiting por IP, usuario e rota.
- Validacao forte de entrada.
- Sanitizacao de saida quando retornar conteudo livre.
- Autorizacao por policy.
- Validacao de tenant e escopo organizacional por requisicao.
- Logs sem tokens, senhas ou documentos sensiveis.
- Auditoria de alteracoes sensiveis.
- Protecao contra mass assignment usando DTOs especificos.
- Tamanho maximo de payload.
- Upload com validacao de extensao, MIME, tamanho e antivirus quando disponivel.
- Headers: `Content-Security-Policy`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`.

## Ameacas E Defesas

| Ameaca | Defesa |
| --- | --- |
| XSS roubando token | BFF com cookie HttpOnly, CSP, sanitizacao |
| CSRF | SameSite, anti-forgery token, validacao de origem |
| IDOR | Validacao de tenant e escopo em todo endpoint |
| SQL Injection | EF Core parametrizado, sem SQL concatenado |
| Brute force | Rate limiting, lockout no IdP, logs de seguranca |
| Token replay | Validacao de exp, aud, iss, assinatura e jti quando aplicavel |
| Upload malicioso | Validacao, quarentena, storage isolado, antivirus |
| Exfiltracao via logs | Redacao de dados sensiveis e filtros de logging |
| Enumeracao de IDs | GUID/ULID, respostas 404/403 consistentes |

## Autorizacao

Use policies no backend:

- `Assets.Read`
- `Assets.Write`
- `Assets.ApproveFinancialChange`
- `Contracts.Read`
- `Contracts.Write`
- `Costs.Read`
- `Costs.Write`
- `Reports.Read`
- `Admin.ManageTenant`
- `Audit.Read`

Toda policy deve considerar:

- Perfil global.
- Tenant atual.
- Escopo organizacional.
- Tipo do recurso.
- Status do recurso.
