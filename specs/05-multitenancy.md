# Spec 05: Multitenancy

## Objetivo

Implementar isolamento por tenant e autorizacao por escopo organizacional.

## Tarefas

| ID | Status | Descricao | Validacao |
| --- | --- | --- | --- |
| S3-001 | PENDING | Criar `ITenantContext` | Testes unitarios |
| S3-002 | PENDING | Resolver tenant por claim/header interno confiavel | Testes integrados |
| S3-003 | PENDING | Aplicar filtros globais EF Core por tenant | Teste tentando acessar outro tenant |
| S3-004 | PENDING | Criar servico de escopo organizacional | Testes com arvore organizacional |
| S3-005 | PENDING | Implementar validacao de acesso por unidade descendente | Testes |
| S3-006 | PENDING | Preparar suporte a RLS PostgreSQL | Script SQL/documentacao |

## Criterios De Aceite

- Um usuario de um tenant nao acessa dados de outro tenant.
- Um gerente com duas atribuicoes consegue acessar ambas.
- Um usuario de nucleo nao acessa area irma sem permissao.
- Relatorio consolidado exige permissao explicita.
