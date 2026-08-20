# Master Task List

Use este arquivo como quadro principal de execucao.

## Fase 0: Preparacao

| ID | Status | Tarefa | Spec |
| --- | --- | --- | --- |
| F0-001 | DONE | Criar estrutura `/frontend` baseada no template institucional | `03-frontend-foundation.md` |
| F0-002 | DONE | Criar estrutura `/backend` para DDD | `02-backend-foundation.md` |
| F0-003 | DONE | Criar documentacao transversal de produto, seguranca e dominio | `06-security-hardening.md` |
| F0-004 | DONE | Criar pasta `/specs` para continuidade entre agentes | `README.md` |

## Fase 1: Fundacao Backend

| ID | Status | Tarefa | Spec |
| --- | --- | --- | --- |
| B1-000 | DONE | Subir PostgreSQL dedicado em container (`infra/docker-compose.yml`) | `02-backend-foundation.md` |
| B1-001 | DONE | Criar solution .NET e projetos reais | `02-backend-foundation.md` |
| B1-002 | DONE | Configurar referencias entre camadas | `02-backend-foundation.md` |
| B1-003 | DONE | Instalar pacotes base | `02-backend-foundation.md` |
| B1-004 | DONE | Criar `Program.cs` com health checks, OpenAPI, auth e middlewares iniciais | `02-backend-foundation.md` |
| B1-005 | DONE | Criar `ControleAtivosDbContext` | `02-backend-foundation.md` |
| B1-006 | DONE | Configurar PostgreSQL e connection string segura | `02-backend-foundation.md` |

## Fase 2: Dominio E Banco

| ID | Status | Tarefa | Spec |
| --- | --- | --- | --- |
| D2-001 | DONE | Implementar entidades organizacionais | `04-domain-and-database.md` |
| D2-002 | DONE | Implementar entidades de ativos | `04-domain-and-database.md` |
| D2-003 | DONE | Implementar contratos e fornecedores | `04-domain-and-database.md` |
| D2-004 | DONE | Implementar custos e reajustes | `04-domain-and-database.md` |
| D2-005 | DONE | Implementar timeline e audit log | `04-domain-and-database.md` |
| D2-006 | DONE | Criar mappings EF Core | `04-domain-and-database.md` |
| D2-007 | DONE | Criar migrations iniciais | `04-domain-and-database.md` |

## Fase 3: Multitenancy E Seguranca

| ID | Status | Tarefa | Spec |
| --- | --- | --- | --- |
| S3-001 | DONE | Implementar tenant context | `05-multitenancy.md` |
| S3-002 | DONE | Implementar filtros globais por tenant | `05-multitenancy.md` |
| S3-003 | DONE | Implementar escopo organizacional | `05-multitenancy.md` |
| S3-004 | DONE | Implementar policies de autorizacao | `06-security-hardening.md` |
| S3-005 | DONE | Implementar rate limiting, CORS restrito e headers seguros | `06-security-hardening.md` |
| S3-006 | PENDING | Implementar BFF ou preparar adaptacao segura do auth | `06-security-hardening.md` |
| S3-007 | DONE | Testar isolamento entre tenants com teste integrado | `05-multitenancy.md` |
| S3-008 | PENDING | Avaliar RLS no PostgreSQL | `05-multitenancy.md` |

## Fase 4: API De Negocio

| ID | Status | Tarefa | Spec |
| --- | --- | --- | --- |
| A4-001 | DONE | Implementar endpoint de dashboard summary | `07-api-use-cases.md` |
| A4-002 | DONE | Implementar endpoints de licencas (CRUD, reajuste, quantidade, uso, simulacao) | `07-api-use-cases.md` |
| A4-003 | DONE | Implementar endpoints de servidores (CRUD, custos, resize, desativacao) | `07-api-use-cases.md` |
| A4-004 | DONE | Implementar endpoints de organizacao (arvore e atribuicoes gerenciais) | `07-api-use-cases.md` |
| A4-005 | DONE | Implementar endpoint de timeline | `07-api-use-cases.md` |
| A4-006 | DONE | Implementar endpoints de contratos | `07-api-use-cases.md` |
| A4-007 | DONE | Implementar endpoints de fornecedores | `07-api-use-cases.md` |
| A4-008 | DONE | Implementar endpoints de custos e reajustes consolidados | `07-api-use-cases.md` |
| A4-009 | DONE | Implementar endpoints de alertas e rotina de deteccao | `07-api-use-cases.md` |
| A4-010 | DONE | Implementar endpoints de relatorios | `07-api-use-cases.md` |
| A4-011 | PENDING | Implementar upload de documentos | `07-api-use-cases.md` |

## Fase 5: Frontend

| ID | Status | Tarefa | Spec |
| --- | --- | --- | --- |
| F5-001 | DONE | Ajustar rotas reais e remover tela example quando substituida | `03-frontend-foundation.md` |
| F5-002 | DONE | Criar modelos TypeScript de dominio | `08-frontend-features.md` |
| F5-003 | DONE | Criar services de API por modulo | `08-frontend-features.md` |
| F5-004 | DONE | Implementar dashboard | `08-frontend-features.md` |
| F5-005 | DONE | Implementar licencas | `08-frontend-features.md` |
| F5-006 | DONE | Implementar servidores | `08-frontend-features.md` |
| F5-007 | DONE | Implementar contratos e fornecedores | `08-frontend-features.md` |
| F5-008 | DONE | Implementar custos, reajustes e timeline | `08-frontend-features.md` |
| F5-009 | DONE | Implementar alertas, relatorios e configuracoes | `08-frontend-features.md` |

## Fase 6: Qualidade

| ID | Status | Tarefa | Spec |
| --- | --- | --- | --- |
| Q6-001 | DONE | Criar testes unitarios de dominio (69 aprovados) | `09-testing-and-validation.md` |
| Q6-002 | DONE | Criar testes integrados backend com PostgreSQL (36 aprovados) | `09-testing-and-validation.md` |
| Q6-003 | DONE | Testes frontend automatizados (84 aprovados, 11 arquivos) | `09-testing-and-validation.md` |
| Q6-004 | DONE | Criar validacao de build e lint | `09-testing-and-validation.md` |
| Q6-005 | PENDING | Criar checklist de aceite do MVP | `09-testing-and-validation.md` |

## Entregas posteriores ao escopo inicial

| ID | Status | Entrega | Observacao |
| --- | --- | --- | --- |
| X7-001 | DONE | Controle de acesso local + login break-glass | Autorizacao em `user_role_assignments`; SSO segue disponivel |
| X7-002 | DONE | Formularios de escrita em modal (16 componentes) | `FormDialog`, `FormField`, `CommandRunner` |
| X7-003 | DONE | Correcao do bug de modal em respostas 204 | `CommandOutcome<T>`; 9 endpoints afetados |
| X7-004 | DONE | Lancamento de custos avulsos (backend + tela) | `POST/GET /costs/entries`, competencia normalizada |
| X7-005 | DONE | Edicao cadastral separada de desativacao | `PUT` em licencas, servidores, unidades e fornecedores |
| X7-006 | DONE | Exclusao definitiva com verificacao de dependencias | `DELETE` com 409 explicando o impedimento |
| X7-007 | DONE | Cancelamento de licenca | Alternativa oferecida quando a exclusao e recusada |
| X7-008 | DONE | Detalhe de unidade organizacional | `GET /organization/units/{id}` com hierarquia e impedimentos |
| X7-009 | DONE | Arvore organizacional recursiva | Antes exibia 2 dos 5 niveis da hierarquia |
| X7-010 | DONE | Remocao da marca institucional da interface | SSO e slug de tenant preservados |
