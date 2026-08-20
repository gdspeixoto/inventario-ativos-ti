# Spec 04: Domain And Database

## Objetivo

Implementar modelo de dominio e banco PostgreSQL conforme `../docs/domain-model.md`.

## Tarefas

| ID | Status | Descricao | Validacao |
| --- | --- | --- | --- |
| D2-001 | PENDING | Criar entidades `Tenant`, `OrganizationalUnit`, `ManagementAssignment` | Testes de dominio |
| D2-002 | PENDING | Criar entidades `Asset`, `LicenseAsset`, `ServerAsset` | Testes de dominio |
| D2-003 | PENDING | Criar entidades `Supplier`, `Contract` | Testes de dominio |
| D2-004 | PENDING | Criar `CostEntry` e `PriceAdjustment` | Testes de regras financeiras |
| D2-005 | PENDING | Criar `TimelineEvent`, `AuditLog`, `Alert`, `Document` | Testes |
| D2-006 | PENDING | Criar value objects de dinheiro, percentual e periodo | Testes |
| D2-007 | PENDING | Criar mappings EF Core em `Persistence/Mappings` | `dotnet build` |
| D2-008 | PENDING | Criar indices e constraints | Migration gerada |
| D2-009 | PENDING | Criar migration inicial | `dotnet ef migrations list` |

## Regras Criticas

- Todas as entidades de negocio devem ter `TenantId`.
- Timeline e audit log devem ser append-only na operacao normal.
- Valores financeiros calculados nao devem depender do frontend.
- `ManagementAssignment` deve permitir gerente em multiplas areas/nucleos.
