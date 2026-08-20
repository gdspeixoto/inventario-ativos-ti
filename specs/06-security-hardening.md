# Spec 06: Security Hardening

## Objetivo

Blindar backend e frontend contra ataques comuns e reduzir exposicao de tokens.

## Tarefas

| ID | Status | Descricao | Validacao |
| --- | --- | --- | --- |
| S3-004 | PENDING | Criar policies de autorizacao | Testes de 401/403 |
| S3-005 | PENDING | Configurar CORS restrito | Teste origem negada |
| S3-006 | PENDING | Implementar rate limiting | Teste de limite |
| S3-007 | PENDING | Adicionar security headers | Inspecao HTTP |
| S3-008 | PENDING | Implementar tratamento seguro de erros com ProblemDetails | Testes |
| S3-009 | PENDING | Implementar redacao de logs sensiveis | Inspecao logs |
| S3-010 | PENDING | Implementar ou planejar BFF com cookies HttpOnly | Fluxo documentado/testado |
| S3-011 | PENDING | Implementar CSRF se usar cookies | Testes POST sem token |

## Criterios De Aceite

- Nenhum token em `localStorage`.
- Nenhum token em logs.
- Nenhum endpoint sensivel sem policy.
- Nenhum CORS wildcard em producao.
- Erros de producao nao mostram stack trace.
