# Especificacao De API

## Convencoes

- Base path: `/api/v1`.
- JSON em `camelCase`.
- Erros em `application/problem+json`.
- Paginacao por `page`, `pageSize`, `sort`, `direction`.
- Filtros por query string.
- Toda resposta deve conter `traceId` direta ou indiretamente.

## Endpoints Iniciais

### Dashboard

- `GET /api/v1/dashboard/summary`
- `GET /api/v1/dashboard/cost-evolution`
- `GET /api/v1/dashboard/latest-events`

### Licencas

- `GET /api/v1/licenses`
- `POST /api/v1/licenses`
- `GET /api/v1/licenses/{id}`
- `PUT /api/v1/licenses/{id}`
- `POST /api/v1/licenses/{id}/quantity-changes`
- `POST /api/v1/licenses/{id}/price-adjustments`
- `GET /api/v1/licenses/{id}/timeline`

### Servidores

- `GET /api/v1/servers`
- `POST /api/v1/servers`
- `GET /api/v1/servers/{id}`
- `PUT /api/v1/servers/{id}`
- `POST /api/v1/servers/{id}/costs`
- `POST /api/v1/servers/{id}/decommission`
- `GET /api/v1/servers/{id}/timeline`

### Contratos

- `GET /api/v1/contracts`
- `POST /api/v1/contracts`
- `GET /api/v1/contracts/{id}`
- `PUT /api/v1/contracts/{id}`
- `POST /api/v1/contracts/{id}/renew`
- `POST /api/v1/contracts/{id}/price-adjustments`
- `GET /api/v1/contracts/{id}/timeline`

### Fornecedores

- `GET /api/v1/suppliers`
- `POST /api/v1/suppliers`
- `GET /api/v1/suppliers/{id}`
- `PUT /api/v1/suppliers/{id}`
- `GET /api/v1/suppliers/{id}/timeline`

### Custos E Reajustes

- `GET /api/v1/costs`
- `GET /api/v1/price-adjustments`
- `POST /api/v1/price-adjustments/simulate`

### Timeline

- `GET /api/v1/timeline`

### Alertas

- `GET /api/v1/alerts`
- `POST /api/v1/alerts/{id}/resolve`
- `POST /api/v1/alerts/{id}/ignore`

### Organizacao

- `GET /api/v1/organization-units`
- `POST /api/v1/organization-units`
- `PUT /api/v1/organization-units/{id}`
- `GET /api/v1/management-assignments`
- `POST /api/v1/management-assignments`

## Exemplo De Reajuste

`POST /api/v1/licenses/{id}/price-adjustments`

```json
{
  "newAmount": 105.00,
  "effectiveDate": "2026-03-15",
  "reason": "Reajuste anual do fornecedor apos negociacao comercial",
  "indexApplied": "Negociacao manual",
  "approvedByUserId": "00000000-0000-0000-0000-000000000000",
  "documentId": "00000000-0000-0000-0000-000000000000"
}
```

O backend deve calcular `previousAmount`, diferencas e percentual. O cliente nao deve enviar esses campos como verdade.
