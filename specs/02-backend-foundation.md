# Spec 02: Backend Foundation

## Objetivo

Criar a solution .NET real com projetos DDD, dependencias, configuracao base, PostgreSQL e estrutura inicial segura.

## Tarefas

| ID | Status | Descricao | Validacao |
| --- | --- | --- | --- |
| B1-001 | PENDING | Criar `ControleAtivos.sln` | `dotnet sln list` |
| B1-002 | PENDING | Criar projetos `Api`, `Application`, `Domain`, `Persistence` | Pastas com `.csproj` |
| B1-003 | PENDING | Configurar referencias entre projetos | `dotnet build` |
| B1-004 | PENDING | Instalar EF Core, Npgsql, FluentValidation, OpenAPI, Serilog/OpenTelemetry | `dotnet list package` |
| B1-005 | PENDING | Criar estrutura interna de pastas por camada | Inspecao de arquivos |
| B1-006 | PENDING | Criar `Program.cs` com pipeline seguro inicial | `dotnet build` |
| B1-007 | PENDING | Criar health checks `/health` e `/ready` | Rodar API local |
| B1-008 | PENDING | Criar arquivo `appsettings.Development.json.example` sem secrets | Inspecao |

## Estrutura Esperada

```text
backend/
├── ControleAtivos.sln
├── src/
│   ├── ControleAtivos.Api/
│   ├── ControleAtivos.Application/
│   ├── ControleAtivos.Domain/
│   └── ControleAtivos.Persistence/
└── tests/
    ├── ControleAtivos.UnitTests/
    └── ControleAtivos.IntegrationTests/
```

## Regras

- `Domain` nao pode depender de nenhum projeto.
- `Application` depende de `Domain`.
- `Persistence` depende de `Application` e `Domain`.
- `Api` depende de `Application` e `Persistence`.
- Nao criar secrets reais.
- Nao habilitar CORS aberto.

## Ao Concluir

- Atualizar `01-master-task-list.md`.
- Atualizar `00-project-state.md`.
- Informar comandos executados.
