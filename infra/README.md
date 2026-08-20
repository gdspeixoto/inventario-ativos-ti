# Infraestrutura Local

PostgreSQL dedicado ao Controle de Ativos.

## Subir

```bash
cp .env.example .env      # ajuste a senha antes de usar em outro ambiente
docker compose up -d postgres
```

O banco fica exposto em `127.0.0.1:5433`. A porta 5432 do host ja e usada por
outros projetos, e a escuta em loopback impede acesso pela rede.

## pgAdmin (opcional)

```bash
docker compose --profile tools up -d
```

Acesse `http://127.0.0.1:5050`. Ao cadastrar o servidor, use o host `postgres`
e a porta `5432` — dentro da rede do compose vale o nome do servico.

## Conferir

```bash
docker compose ps
docker exec controle-ativos-postgres psql -U controle_ativos_app -d controle_ativos -c "\dt controle_ativos.*"
```

## Migrations

```bash
cd ../backend
export PATH="$PATH:$HOME/.dotnet/tools"
export CONTROLE_ATIVOS_CONNECTION="Host=localhost;Port=5433;Database=controle_ativos;Username=controle_ativos_app;Password=<senha do .env>"

dotnet ef migrations add <Nome> --project src/ControleAtivos.Persistence --startup-project src/ControleAtivos.Persistence
dotnet ef database update      --project src/ControleAtivos.Persistence --startup-project src/ControleAtivos.Persistence
```

## Parar

```bash
docker compose down          # mantem os dados
docker compose down -v       # apaga o volume; use com cuidado
```

## Observacoes

- `infra/.env` guarda a senha e nao deve ser versionado.
- A senha tambem aparece em `backend/src/ControleAtivos.Api/appsettings.Development.json`.
  Em outros ambientes, use variavel de ambiente `ConnectionStrings__Postgres` ou
  `dotnet user-secrets` em vez do arquivo.
- Os scripts em `postgres/init/` rodam apenas na primeira criacao do volume.
