# Estado Atual Do Projeto

## Resumo

Backend .NET com DDD funcional: dominio, persistencia, casos de uso e 20 endpoints REST no ar, com banco PostgreSQL dedicado em container, migration aplicada e 48 testes passando (38 de dominio, 10 de integracao contra PostgreSQL real). Frontend Angular implementado com telas principais, serviços de API, modelos TypeScript, componentes compartilhados e fallback de dados demonstrativos quando a API não responde.

## Stack Decidida

- Frontend: Angular 21, baseado em `frontend/`.
- Backend: C#/.NET 10, DDD, API REST.
- Banco: PostgreSQL 16.10 em container dedicado.
- Autenticacao: Keycloak/OIDC configuravel por ambiente.
- Seguranca recomendada: BFF com cookies HttpOnly para evitar tokens acessiveis por JavaScript.
- Multitenancy: `tenant_id` nas tabelas de negocio + filtro global no EF Core.

## Infraestrutura

Banco dedicado configurado em `infra/docker-compose.yml`.

| Item | Valor |
| --- | --- |
| Container | `controle-ativos-postgres` |
| Imagem | `postgres:16.10-alpine` |
| Porta | `127.0.0.1:5433` (somente loopback) |
| Database | `controle_ativos` |
| Usuario | `controle_ativos_app` |
| Senha | `infra/.env` (nao versionado) |
| Volume | `controle-ativos-postgres-data` |
| Extensoes | pgcrypto, citext, unaccent, pg_trgm |
| Schema | `controle_ativos` |

Comandos:

```bash
cd infra
docker compose up -d postgres         # sobe o banco
docker compose --profile tools up -d  # sobe tambem o pgAdmin em 127.0.0.1:5050
docker compose down                   # para (mantem o volume)
```

## Backend Implementado

### Estrutura

```text
backend/
├── ControleAtivos.sln
├── Directory.Build.props          # net10.0, nullable, warnings as errors
├── src/
│   ├── ControleAtivos.Domain/     # entidades, value objects, enums, eventos
│   ├── ControleAtivos.Application/# abstracoes, permissoes, papeis
│   ├── ControleAtivos.Persistence/# DbContext, mappings, migrations
│   └── ControleAtivos.Api/        # Program, seguranca, middlewares
└── tests/
    ├── ControleAtivos.UnitTests/       # 38 testes passando
    └── ControleAtivos.IntegrationTests/# ainda vazio
```

### Domain

- `Entity`, `TenantEntity`, `Guard`, `DomainException`, `IDomainEvent`.
- Value objects: `Money` (com moeda e bloqueio de soma entre moedas), `Percentage`.
- Organizacao: `Tenant`, `OrganizationalUnit` (adjacency list + materialized path), `AppUser`, `ManagementAssignment`.
- Ativos: `Asset` (base), `LicenseAsset`, `ServerAsset`.
- Comercial: `Supplier`, `Contract`, `CostCenter`.
- Financeiro: `CostEntry`, `PriceAdjustment`.
- Auditoria: `TimelineEvent`, `AuditLog`, `Alert`, `Document`.
- Evento `TimelineEntryRequested` emitido pelas proprias entidades.

### Persistence

- `ControleAtivosDbContext` com tres responsabilidades transversais:
  1. filtro global por tenant (aplicado apenas em tipos raiz por causa do TPH);
  2. carimbo de auditoria em `SaveChanges`;
  3. conversao de eventos de dominio em `timeline_events` na mesma transacao.
- Mappings em `IEntityTypeConfiguration`, com `ComplexProperty` para `Money`.
- Convencao snake_case aplicada a tabelas, colunas, chaves e indices.
- Migration `20260818201100_InitialCreate` aplicada; 15 tabelas criadas.

### Application

- Abstracoes: `ICurrentUser`, `ITenantContext`, `IOrganizationalScope` + `IOrganizationalScopeResolver`, `IUnitOfWork`, repositorios e consultas.
- `Common`: `PagedResult<T>`, `PageRequest` (com teto de 100 itens), `OrganizationalScope`, excecoes `NotFound`/`Forbidden`/`Conflict`.
- `LicenseService`: listar, detalhar, criar, reajustar, alterar quantidade, atualizar uso, simular.
- `ServerService`: listar, detalhar, criar, atualizar custos, redimensionar, desativar.
- `OrganizationService`: arvore, criar unidade, criar/encerrar atribuicao gerencial.
- `TimelineService` e `DashboardService`.

Regras aplicadas em todo caso de uso:

- escopo organizacional verificado antes de qualquer escrita;
- item fora do escopo responde 404, e nao 403, para nao revelar sua existencia;
- reajuste registrado por quem nao tem `assets.approve-financial` exige indicar o aprovador.

### Api

- `Program.cs` com Serilog, forwarded headers, HSTS, HTTPS redirect, CORS restrito, rate limiting particionado por usuario/IP, health checks `/health` e `/ready`, Swagger em desenvolvimento.
- `CurrentUser`: le claims e deriva permissoes a partir dos papeis.
- `TenantContext` + `TenantResolutionMiddleware`: resolve tenant por claim (header apenas via BFF) e provisiona o usuario local.
- Policies por permissao, com fallback exigindo autenticacao.
- `SecurityHeadersMiddleware` e `ExceptionHandlingMiddleware` (ProblemDetails, sem stack trace em producao).

### Endpoints disponiveis

```text
GET    /health · /ready

GET    /api/v1/dashboard/summary

GET    /api/v1/licenses
POST   /api/v1/licenses
GET    /api/v1/licenses/{id}
POST   /api/v1/licenses/{id}/price-adjustments
POST   /api/v1/licenses/{id}/quantity-changes
PUT    /api/v1/licenses/{id}/usage
POST   /api/v1/licenses/simulate-adjustment

GET    /api/v1/servers
POST   /api/v1/servers
GET    /api/v1/servers/{id}
PUT    /api/v1/servers/{id}/costs
POST   /api/v1/servers/{id}/resize
POST   /api/v1/servers/{id}/decommission

GET    /api/v1/organization/units
POST   /api/v1/organization/units
GET    /api/v1/organization/management-assignments
POST   /api/v1/organization/management-assignments
POST   /api/v1/organization/management-assignments/{id}/finish

GET    /api/v1/timeline
```

## Ultima Validacao Conhecida

| Comando | Resultado |
| --- | --- |
| `docker compose up -d postgres` | container healthy |
| `dotnet build` | sucesso, 0 warnings |
| `dotnet ef database update` | migration aplicada |
| `dotnet test` | 48/48 aprovados (38 unitarios + 10 integrados) |
| API em execucao | `/health` 200; rotas protegidas retornam 401 sem token |
| `npm run build` (frontend) | sucesso |

### Testes de integracao

Rodam contra o PostgreSQL de desenvolvimento, em um database separado
(`controle_ativos_tests`), criado uma vez com:

```bash
docker exec controle-ativos-postgres psql -U controle_ativos_app -d postgres \
  -c "CREATE DATABASE controle_ativos_tests OWNER controle_ativos_app;"
```

Em CI, defina `CONTROLE_ATIVOS_TEST_CONNECTION` apontando para o servico do pipeline.

Cobrem: isolamento entre tenants (listagem, busca por id, contagem, timeline),
reajuste com historico e timeline na mesma transacao, alteracao de quantidade,
codigo duplicado e acesso negado a usuario fora do escopo organizacional.

> Testcontainers foi avaliado e descartado: o container auxiliar (Ryuk) sobe mas
> nao fica alcancavel neste host, fazendo a bateria inteira expirar por timeout.

## Proxima Acao Recomendada

Configurar os clients reais do Keycloak (`controle-ativos-api` e `controle-ativos-frontend`), definir a claim de tenant e validar login fim a fim. Depois disso, evoluir os CRUDs completos de edição/cadastro nas telas que hoje possuem listagem e detalhe demonstrativo.

## Decisoes Tomadas Nesta Sessao

- .NET 10 (SDK disponivel na maquina).
- PostgreSQL exposto apenas em `127.0.0.1:5433`, para nao conflitar com os demais bancos do host e nao ficar acessivel pela rede.
- TPH para a hierarquia de ativos: licencas e servidores compartilham a maioria dos campos e sao consultados juntos.
- `Money` como struct mapeada via `ComplexProperty` (EF Core exige tipo de referencia em `OwnsOne`).
- Permissoes derivadas dos papeis no backend, e nao lidas do token.
- Formatacao monetaria brasileira explicita, ja que a aplicacao roda com `InvariantGlobalization`.
- Leitura e escrita separadas: repositorios carregam agregados para mutacao;
  as consultas projetam direto para DTO, sem rastreamento.
- Escopo organizacional resolvido uma vez por requisicao e mantido em cache no
  `OrganizationalScopeResolver`.
- Itens fora do escopo respondem 404 em vez de 403.
- Testes de integracao contra PostgreSQL real, e nao provider InMemory.

## Decisoes Pendentes

- Confirmar o client Keycloak `controle-ativos-api` e o `controle-ativos-frontend` no realm institucional.
- Confirmar como o tenant sera emitido no token (claim `tenant_id` ou `tenant`).
- Definir se o BFF sera implementado antes ou depois do MVP.
- Confirmar nomes oficiais dos negocios, areas tecnologicas, unidades operacionais e nucleos.
- Definir storage dos documentos (disco, S3 ou MinIO).

## Como Atualizar Este Arquivo

Ao final de cada tarefa, registre data, tarefa executada, arquivos alterados, comandos de validacao e pendencias.

## Historico

### 2026-08-18 — Estrutura inicial

- Criadas as pastas `frontend`, `backend`, `docs`, `.agent` e `specs`.
- Frontend copiado do template institucional Angular e renomeado.
- Documentacao de produto, dominio, multitenancy, seguranca, BFF, API e banco.

### 2026-08-18 — Fundacao do backend

- Subido PostgreSQL dedicado (`infra/docker-compose.yml`).
- Criada a solution .NET com quatro projetos e dois de teste.
- Implementado o dominio completo com regras financeiras e de hierarquia.
- Implementados DbContext, mappings, filtro de tenant e timeline automatica.
- Migration inicial gerada e aplicada.
- Configurados autenticacao Keycloak, autorizacao por permissao, rate limiting e headers de seguranca.
- 38 testes de dominio escritos e aprovados.
- Corrigida formatacao monetaria: as descricoes da timeline saiam em padrao americano.

### 2026-08-18 — Casos de uso e API

- Criada a camada Application: abstracoes, `Common`, DTOs e servicos de
  licencas, servidores, organizacao, timeline e dashboard.
- Criadas as consultas de leitura na Persistence, com projecao para DTO,
  paginacao, filtros, ordenacao e aplicacao do escopo organizacional.
- Criados os repositorios de escrita e o registro de DI das duas camadas.
- Movido o `OrganizationalScopeResolver` da Api para a Persistence: ele
  consultava o banco direto da camada HTTP, violando as dependencias.
- Criados quatro controllers com 20 endpoints, policies por permissao e cota
  de escrita no rate limiting.
- Ampliado o tratamento de erros: `NotFound` 404, `Conflict` 409, `Forbidden` 403.
- Criados 10 testes de integracao contra PostgreSQL real.
- Corrigidos dois defeitos encontrados na subida da API: handler de autorizacao
  registrado como singleton consumindo servico scoped, e `AddControllers` ausente.


### 2026-08-18 — Backend completo e frontend implementado

- Backend ampliado com contratos, fornecedores, custos, alertas, relatórios e seed de desenvolvimento.
- Adicionados controllers: `ContractsController`, `SuppliersController`, `CostsController`, `AlertsController`, `ReportsController`.
- Frontend implementado com modelos TypeScript, services de API e páginas: dashboard, licenças, detalhe de licença, servidores, detalhe de servidor, contratos, fornecedores, custos, timeline, alertas, relatórios e organização.
- Componentes compartilhados criados: `app-money`, `app-status-badge`, `app-stat-card`, `app-timeline-list`.
- As telas usam API real e fallback demonstrativo local para continuar apresentáveis enquanto o Keycloak/API não estiverem conectados no navegador.
- Validação final: `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` aprovados.

---

## Estado atual

**Testes:** 69 unitarios + 36 integracao (backend) e 84 (frontend). Build e
lint aprovados nos dois lados.

**Pendencias conhecidas:**

| Item | Situacao |
| --- | --- |
| S3-006 (BFF) | Nao implementado; o frontend fala direto com a API |
| S3-008 (RLS PostgreSQL) | O isolamento por tenant vem do filtro global do EF |
| A4-011 (upload de documentos) | Entidade `Document` pronta no dominio; falta endpoint e storage |
| Q6-005 (checklist de aceite) | Pendente |
| Autocomplete de usuarios | Bloqueado: exige service account no Keycloak (`view-users`) |

**Decisoes que valem registro:**

Excluir e desativar respondem a situacoes diferentes e nao foram unificados.
Desativar encerra algo que existiu e preserva o historico; excluir desfaz um
cadastro equivocado. Por isso a exclusao so e aceita quando nada depende do
registro, e a recusa (409) explica o que impede em vez de devolver erro de
chave estrangeira.

A verificacao de dependencias vive no repositorio, consultando o banco. Uma
checagem em memoria responderia "sem dependencias" para agregados nao
carregados pelo EF — e alguem apagaria uma subarvore inteira.

A marca institucional foi removida da interface, mas as URLs do Keycloak e o
slug do tenant permanecem: troca-los quebraria autenticacao e resolucao de
tenant sem uma migracao correspondente no servidor SSO e no banco.
