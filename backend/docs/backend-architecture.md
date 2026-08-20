# Arquitetura Backend

## Dependencias Entre Camadas

```text
Api -> Application -> Domain
Api -> Persistence -> Application -> Domain
Persistence -> Domain
```

Regras:

- `Domain` nao referencia nenhum projeto.
- `Application` referencia `Domain`.
- `Persistence` referencia `Application` e `Domain` para implementar interfaces.
- `Api` referencia `Application` e `Persistence` para compor injecao de dependencia.

## Domain

Conteudo:

- Entidades.
- Value Objects.
- Enums.
- Domain Events.
- Regras invariantes.
- Interfaces de dominio quando necessario.

Nao conter:

- EF Core attributes.
- Controllers.
- DTOs de API.
- Validacao HTTP.
- Dependencias de infraestrutura.

## Application

Conteudo:

- Use cases.
- Commands e Queries.
- DTOs.
- Validators.
- Interfaces de repositories.
- Interfaces de servicos externos.
- Contratos de autorizacao contextual.
- Mapeamentos entre DTOs e dominio, se a equipe optar por Mapster/AutoMapper nesta camada.

Use cases obrigatorios iniciais:

- Criar licenca.
- Atualizar licenca.
- Registrar reajuste de licenca.
- Alterar quantidade contratada de licenca.
- Criar servidor.
- Atualizar servidor.
- Registrar custo de servidor.
- Criar contrato.
- Renovar contrato.
- Registrar reajuste de contrato.
- Criar fornecedor.
- Listar timeline geral.
- Listar timeline por item.
- Gerar alertas.
- Consultar dashboard.

## Persistence

Conteudo:

- `ControleAtivosDbContext`.
- Mapeamentos EF Core por entidade.
- Repositories.
- Unit of Work, se adotado.
- Migrations.
- Configuracao PostgreSQL.
- Filtros globais de tenant.
- Implementacao de auditoria persistente.

Padrao de mapeamento:

```text
Persistence/
├── Context/
├── Mappings/
├── Repositories/
├── Interceptors/
└── Migrations/
```

## Api

Conteudo:

- Controllers ou Minimal APIs organizadas por modulo.
- Middlewares de correlation ID, tenant, erros, auditoria e seguranca.
- Autenticacao Keycloak/OIDC.
- Autorizacao por policies.
- Rate limiting.
- OpenAPI.
- Health checks.

Padrao de rotas:

```text
/api/v1/dashboard
/api/v1/licenses
/api/v1/servers
/api/v1/contracts
/api/v1/suppliers
/api/v1/costs
/api/v1/price-adjustments
/api/v1/timeline
/api/v1/alerts
/api/v1/reports
/api/v1/organization-units
/api/v1/settings
```

## Padrao De Resposta

Use respostas consistentes:

```json
{
  "data": {},
  "traceId": "00-...",
  "message": "Operacao realizada com sucesso"
}
```

Erros devem usar `ProblemDetails`.

## Mapeamentos

Recomendacao:

- EF Core mappings em classes `IEntityTypeConfiguration<T>` dentro de `Persistence/Mappings`.
- DTO mappings com Mapster ou AutoMapper, centralizados por modulo.
- Nunca expor entidade de dominio diretamente na API.
