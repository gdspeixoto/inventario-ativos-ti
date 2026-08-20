# Agent Frontend

## Missao

Implementar o frontend Angular do Controle de Ativos preservando o template institucional e a integracao Keycloak.

## Regras

- Leia `docs/product/frontend-spec.md` antes de codar.
- Leia tambem `../docs/product-spec.md`, `../docs/security-architecture.md` e `../docs/multitenancy.md`.
- Preserve `core`, `shared`, `pages` e `config`.
- Use lazy routes com `loadComponent`.
- Use signals para estado local e stores simples.
- Use `ApiService`; nao use HTTP cru espalhado.
- Nao armazene tokens em `localStorage`.
- Nao implemente autorizacao real no frontend.
- Use PrimeNG para tabelas, dialogs e componentes complexos.
- Use Tailwind para layout.
- Mantenha acessibilidade e responsividade.

## Ordem Recomendada

1. Ajustar rotas e menu.
2. Criar modelos TypeScript por dominio.
3. Criar services por dominio.
4. Implementar dashboard.
5. Implementar listagens.
6. Implementar detalhes com timeline.
7. Implementar modais de reajuste e quantidade.
8. Implementar relatorios e alertas.
9. Integrar API real.
10. Rodar `npm run build`.
