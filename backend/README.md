# Backend Controle De Ativos

Backend em C#/.NET para o Inventario de Ativos de TI.

## Stack Recomendada

- .NET 9 ou versao LTS vigente na instituicao.
- ASP.NET Core Web API.
- Entity Framework Core.
- PostgreSQL com Npgsql.
- FluentValidation.
- Mapster ou AutoMapper para mapeamentos de DTOs.
- MediatR ou implementacao simples de use cases por handlers.
- Serilog/OpenTelemetry para observabilidade.
- Keycloak/OIDC para identidade.

## Projetos

```text
src/
├── ControleAtivos.Api             # Controllers, auth, middlewares, OpenAPI
├── ControleAtivos.Application     # Use cases, DTOs, validators, interfaces
├── ControleAtivos.Domain          # Entidades, value objects, regras, eventos
└── ControleAtivos.Persistence     # EF Core, mappings, repositories, migrations
```

## Executar

O banco precisa estar no ar (ver `../infra/README.md`).

```bash
cd src/ControleAtivos.Api
dotnet run
```

Swagger em desenvolvimento: `http://localhost:5169/swagger`.

Health checks: `/health` (liveness) e `/ready` (inclui o PostgreSQL).

## Testar

```bash
dotnet test                                  # unitarios + integrados
dotnet test tests/ControleAtivos.UnitTests   # so dominio, nao exige banco
```

Os testes de integracao usam o database `controle_ativos_tests`. Crie uma vez:

```bash
docker exec controle-ativos-postgres psql -U controle_ativos_app -d postgres \
  -c "CREATE DATABASE controle_ativos_tests OWNER controle_ativos_app;"
```

Em CI, aponte `CONTROLE_ATIVOS_TEST_CONNECTION` para o servico do pipeline.

## Migrations

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
export CONTROLE_ATIVOS_CONNECTION="Host=localhost;Port=5433;Database=controle_ativos;Username=controle_ativos_app;Password=<senha>"

dotnet ef migrations add <Nome> --project src/ControleAtivos.Persistence --startup-project src/ControleAtivos.Persistence
dotnet ef database update      --project src/ControleAtivos.Persistence --startup-project src/ControleAtivos.Persistence
```

## Leitura Obrigatoria

- `docs/backend-architecture.md`
- `docs/security-checklist.md`
- `docs/database/schema-spec.md`
- `docs/api/api-spec.md`
- `../docs/domain-model.md`
- `../docs/multitenancy.md`
