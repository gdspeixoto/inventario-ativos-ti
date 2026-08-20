# Spec 03: Frontend Foundation

## Objetivo

Preparar o frontend Angular baseado no template institucional para desenvolvimento das telas reais.

## Status Atual

O template ja foi copiado para `frontend/`, renomeado em arquivos principais e validado com `npm run build`.

## Tarefas Pendentes

| ID | Status | Descricao | Validacao |
| --- | --- | --- | --- |
| F5-001 | PENDING | Criar rotas reais dos modulos | `npm run build` |
| F5-002 | PENDING | Criar paginas placeholder para modulos principais | Navegacao funcional |
| F5-003 | PENDING | Remover rota `example` quando houver substituto | Busca por `/example` |
| F5-004 | PENDING | Revisar textos i18n pt-BR e en-US | `npm run build` |
| F5-005 | PENDING | Confirmar configuracao Keycloak institucional | Login em ambiente dev |
| F5-006 | PENDING | Decidir adaptacao para BFF/cookies HttpOnly | Documentar decisao |

## Rotas Esperadas

```text
/dashboard
/licenses
/licenses/:id
/servers
/servers/:id
/contracts
/contracts/:id
/suppliers
/suppliers/:id
/costs
/timeline
/alerts
/reports
/organization
/settings
```

## Regras

- Preservar arquitetura do template.
- Usar lazy loading com `loadComponent`.
- Usar PrimeNG e Tailwind existentes.
- Nao armazenar tokens em `localStorage`.
- Nao implementar autorizacao apenas visual.
