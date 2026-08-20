# Inventario de Ativos de TI

Sistema web para inventario, controle financeiro e governanca de ativos de TI. A aplicacao centraliza licencas, servidores, contratos, fornecedores, custos, documentos, eventos de timeline e auditoria operacional.

## Visao Geral

O projeto e dividido em backend .NET e frontend Angular, com arquitetura pensada para ambientes corporativos, segregacao por tenant e autorizacao baseada em perfis e escopo organizacional.

Principais capacidades:

- Cadastro e acompanhamento de licencas, servidores, contratos e fornecedores.
- Controle de custos mensais, valores anuais, centros de custo e reajustes.
- Historico auditavel por ativo, contrato e movimentacao relevante.
- Organizacao hierarquica por matriz, negocio, area, unidade e nucleo.
- Autenticacao OIDC/Keycloak e suporte a login local de emergencia em desenvolvimento.
- Isolamento de dados por tenant e politicas de acesso no backend.
- Armazenamento de documentos associado aos ativos e contratos.

## Stack

- Backend: .NET, C#, Entity Framework Core, PostgreSQL e arquitetura em camadas.
- Frontend: Angular, TypeScript, PrimeNG, Tailwind CSS e i18n.
- Banco de dados: PostgreSQL com migrations versionadas.
- Autenticacao: OIDC/Keycloak, com configuracao externalizada por ambiente.
- Infra local: Docker Compose para dependencias de desenvolvimento.

## Estrutura Do Repositorio

```text
.
|-- backend/   # API, dominio, aplicacao, persistencia e testes .NET
|-- frontend/  # Aplicacao Angular
|-- docs/      # Documentacao de produto, arquitetura e seguranca
|-- infra/     # Docker Compose e scripts de banco
|-- specs/     # Planejamento e backlog tecnico
`-- README.md
```

## Como Executar

1. Configure as variaveis locais a partir dos exemplos:

```bash
cp infra/.env.example infra/.env
cp backend/src/ControleAtivos.Api/appsettings.Development.json.example backend/src/ControleAtivos.Api/appsettings.Development.json
```

2. Suba a infraestrutura local:

```bash
docker compose --env-file infra/.env -f infra/docker-compose.yml up -d
```

3. Execute a API:

```bash
dotnet run --project backend/src/ControleAtivos.Api/ControleAtivos.Api.csproj
```

4. Execute o frontend:

```bash
cd frontend
npm install
npm start
```

## Desenvolvimento

- Mantenha secrets fora do Git. Use arquivos `.env` e `appsettings.Development.json` locais.
- Migrations ficam em `backend/src/ControleAtivos.Persistence/Migrations`.
- Dados de demonstracao ficam no seeder de desenvolvimento e nao devem depender de informacoes reais.
- Regras de autorizacao devem ser validadas no backend; o frontend apenas ajusta a experiencia do usuario.

## Testes

Backend:

```bash
dotnet test backend/ControleAtivos.slnx
```

Frontend:

```bash
cd frontend
npm test
```

## Documentacao

- `docs/product-spec.md`
- `docs/domain-model.md`
- `docs/security-architecture.md`
- `docs/multitenancy.md`
- `backend/docs/backend-architecture.md`
- `backend/docs/api/api-spec.md`
- `frontend/docs/architecture.md`
