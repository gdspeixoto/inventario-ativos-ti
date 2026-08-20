# Multitenancy

## Objetivo

Garantir isolamento logico de dados entre tenants e permitir autorizacao por escopo organizacional.

## Estrategia Recomendada

Use banco PostgreSQL compartilhado com coluna `TenantId` em todas as tabelas de negocio, filtro global no EF Core e, quando possivel, Row-Level Security no PostgreSQL.

Motivo:

- Menor complexidade operacional que banco por tenant.
- Melhor para relatorios consolidados controlados.
- Permite isolamento forte quando combinado com RLS.

## Resolucao De Tenant

Ordem recomendada:

1. Claim emitida pelo Keycloak, exemplo `tenant_id` ou `organization`.
2. Header interno definido pelo BFF/API Gateway apos validar sessao, exemplo `X-Tenant-Id`.
3. Subdominio, se o produto evoluir para tenants por host.

Nunca aceitar `tenantId` vindo livremente do corpo da requisicao para decidir isolamento.

## Escopo Organizacional

Alem do tenant, o usuario possui acesso a um ou mais nos organizacionais.

Exemplo:

- Usuario A pode ver toda a Matriz.
- Usuario B pode ver apenas um Negocio.
- Usuario C pode gerenciar duas Areas Tecnologicas.
- Usuario D pode auditar todos os Nucleos de Suporte.

O backend deve calcular escopo efetivo com base em:

- Claims do usuario.
- Perfis aplicados no Keycloak.
- Tabela `ManagementAssignments`.
- Tabela local de permissoes complementares, se necessaria.

## Regras Obrigatorias

- Todas as queries devem filtrar por `TenantId`.
- Listagens devem filtrar por escopo organizacional.
- Detalhes devem validar acesso ao item antes de retornar dados.
- Comandos devem validar permissao no tenant e no no organizacional afetado.
- Relatorios consolidados exigem permissao explicita.
- IDs globais nao devem permitir enumeracao de dados entre tenants.

## PostgreSQL RLS

Quando implementar RLS:

- Ativar RLS em tabelas sensiveis.
- Definir uma variavel de sessao por conexao, exemplo `app.current_tenant_id`.
- Criar policies que comparem `tenant_id` com `current_setting('app.current_tenant_id')`.
- Garantir que a aplicacao seta a variavel em toda conexao antes de executar queries.
