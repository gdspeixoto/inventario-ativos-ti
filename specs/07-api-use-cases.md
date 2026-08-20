# Spec 07: API Use Cases

## Objetivo

Implementar endpoints e casos de uso da API.

## Tarefas

| ID | Status | Descricao | Validacao |
| --- | --- | --- | --- |
| A4-001 | PENDING | Dashboard summary | Teste integrado |
| A4-002 | PENDING | CRUD de licencas | Teste integrado |
| A4-003 | PENDING | Reajuste de licenca com timeline | Teste integrado |
| A4-004 | PENDING | Alteracao de quantidade de licenca | Teste integrado |
| A4-005 | PENDING | CRUD de servidores | Teste integrado |
| A4-006 | PENDING | Custos de servidor | Teste integrado |
| A4-007 | PENDING | CRUD de contratos | Teste integrado |
| A4-008 | PENDING | Renovacao e reajuste de contrato | Teste integrado |
| A4-009 | PENDING | CRUD de fornecedores | Teste integrado |
| A4-010 | PENDING | Timeline geral e por item | Teste integrado |
| A4-011 | PENDING | Alertas | Teste integrado |
| A4-012 | PENDING | Relatorios | Teste integrado |

## Regras

- Toda mutacao cria audit log.
- Toda alteracao financeira cria timeline.
- Todo endpoint valida tenant e permissao.
- Todo endpoint retorna DTO, nunca entidade diretamente.
