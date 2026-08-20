# Spec 09: Testing And Validation

## Objetivo

Garantir qualidade minima antes de considerar tarefas concluidas.

## Tarefas

| ID | Status | Descricao | Validacao |
| --- | --- | --- | --- |
| Q6-001 | PENDING | Testes unitarios de dominio | `dotnet test` |
| Q6-002 | PENDING | Testes de regras financeiras | `dotnet test` |
| Q6-003 | PENDING | Testes de multitenancy | `dotnet test` |
| Q6-004 | PENDING | Testes integrados PostgreSQL | `dotnet test` |
| Q6-005 | PENDING | Testes de autorizacao 401/403 | `dotnet test` |
| Q6-006 | PENDING | Build frontend | `npm run build` |
| Q6-007 | PENDING | Lint frontend | `npm run lint` |
| Q6-008 | PENDING | Checklist manual responsivo | Registro no project state |

## Regra Para Marcar DONE

Uma tarefa so pode virar `DONE` quando:

- Codigo foi implementado.
- Build/teste relacionado foi executado.
- Erros foram corrigidos ou documentados como bloqueio.
- `00-project-state.md` foi atualizado.
