# Especificacao Do Banco PostgreSQL

## Convencoes

- Tabelas em `snake_case`.
- Colunas em `snake_case`.
- Chave primaria `id` como UUID.
- Coluna `tenant_id` obrigatoria em tabelas de negocio.
- Datas em UTC usando `timestamptz`.
- Valores monetarios usando `numeric(18,2)`.
- Percentuais usando `numeric(9,4)`.
- JSON estruturado usando `jsonb`.

## Tabelas Principais

- `tenants`
- `organizational_units`
- `users`
- `management_assignments`
- `cost_centers`
- `categories`
- `suppliers`
- `contracts`
- `assets`
- `license_assets`
- `server_assets`
- `cost_entries`
- `price_adjustments`
- `timeline_events`
- `documents`
- `alerts`
- `audit_logs`
- `settings`

## Indices Obrigatorios

- `organizational_units(tenant_id, parent_id)`
- `organizational_units(tenant_id, path)`
- `assets(tenant_id, organizational_unit_id)`
- `assets(tenant_id, status)`
- `license_assets(asset_id)`
- `server_assets(asset_id)`
- `contracts(tenant_id, supplier_id)`
- `contracts(tenant_id, end_date)`
- `cost_entries(tenant_id, competence_month)`
- `price_adjustments(tenant_id, effective_date)`
- `timeline_events(tenant_id, entity_type, entity_id, occurred_at)`
- `alerts(tenant_id, status, priority)`
- `audit_logs(tenant_id, occurred_at)`

## Regras De Integridade

- `license_assets.asset_id` deve referenciar `assets.id`.
- `server_assets.asset_id` deve referenciar `assets.id`.
- `assets.tenant_id` deve ser igual ao tenant das entidades vinculadas.
- `used_quantity` nao deve ser maior que `contracted_quantity`, exceto se regra de negocio permitir estouro controlado.
- `available_quantity` deve ser calculada ou mantida consistente.
- `price_adjustments.previous_amount` e `new_amount` devem ser positivos.
- `timeline_events` nao devem ser apagados fisicamente em operacao normal.

## RLS Exemplo Conceitual

```sql
ALTER TABLE assets ENABLE ROW LEVEL SECURITY;

CREATE POLICY assets_tenant_isolation ON assets
USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
```
