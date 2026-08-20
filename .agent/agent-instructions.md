# Instrucoes Para Agentes De IA

Voce esta desenvolvendo o sistema Inventario de Ativos de TI.

## Regras Obrigatorias

- Leia primeiro `README.md`, `docs/product-spec.md`, `docs/domain-model.md`, `docs/security-architecture.md` e `docs/multitenancy.md`.
- Nao implemente autenticacao ou autorizacao apenas no frontend.
- Nao armazene access token, refresh token ou id token em `localStorage`.
- Nao exponha secrets no frontend, em arquivos de ambiente ou no repositorio.
- Todo endpoint do backend deve validar tenant, escopo organizacional e permissao.
- Toda alteracao financeira deve criar evento de timeline e registro historico.
- Toda alteracao sensivel deve registrar auditoria.
- Use PostgreSQL como banco principal.
- Use C#/.NET com DDD no backend.
- Use Angular do template institucional no frontend.
- Prefira mudancas pequenas, rastreaveis e testaveis.

## Frontend

- Base: `frontend/`, copiada do template institucional Angular 21.
- Preserve a arquitetura do template: `core`, `shared`, `pages`, `config`.
- Use componentes standalone, signals e lazy routes.
- Use PrimeNG e Tailwind ja presentes no template.
- Use Keycloak institucional via configuracao existente.
- Considere migrar o fluxo de auth para BFF se o backend implementar cookies HttpOnly.

## Backend

- Base: `backend/`.
- Camadas obrigatorias:
  - `ControleAtivos.Api`
  - `ControleAtivos.Application`
  - `ControleAtivos.Domain`
  - `ControleAtivos.Persistence`
- Domain nao depende de nenhuma outra camada.
- Application depende apenas de Domain e abstracoes.
- Persistence implementa repositorios, mapeamentos EF Core e consultas PostgreSQL.
- Api expoe controllers/endpoints, middlewares, auth, rate limiting e OpenAPI.

## Seguranca

- Use HTTPS obrigatorio.
- Use headers seguros.
- Use validacao de entrada em todos os comandos.
- Use queries parametrizadas via EF Core.
- Use Row-Level Security no PostgreSQL quando viavel.
- Use logs estruturados sem dados sensiveis.
- Aplique rate limiting e protecao contra brute force.
- Bloqueie CORS amplo.
- Valide `issuer`, `audience`, assinatura, expiracao e roles dos tokens caso API receba Bearer.
- Preferencia arquitetural: BFF com cookies `HttpOnly`, `Secure`, `SameSite=Lax/Strict`.

## Commits E Qualidade

- Nao faça commit sem pedido explicito.
- Antes de finalizar qualquer feature, rode build/testes quando existirem.
- Atualize documentacao quando regra de negocio mudar.
