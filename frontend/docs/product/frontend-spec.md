# Especificacao Frontend

## Base

Este frontend foi criado a partir do template institucional:

`frontend/`

Stack:

- Angular 21.
- Standalone components.
- Signals.
- Zoneless.
- PrimeNG.
- TailwindCSS.
- SCSS.
- Keycloak/OIDC ja suportado pelo template.

## Decisao Angular Vs Vite

Usar Angular.

Motivos:

- O template institucional ja e Angular e ja possui Keycloak.
- O produto e administrativo, grande, com muitas telas, formularios, tabelas e permissoes.
- Angular oferece padrao mais previsivel para equipes corporativas.
- Vite nao deve ser usado como base principal neste projeto, pois substituiria a fundacao existente.

## Paginas Obrigatorias

- Dashboard.
- Licencas.
- Detalhe de licenca.
- Servidores.
- Detalhe de servidor.
- Contratos.
- Detalhe de contrato.
- Fornecedores.
- Custos e reajustes.
- Timeline geral.
- Alertas.
- Relatorios.
- Configuracoes.
- Organizacao.

## Estrutura De Rotas Desejada

```text
/dashboard
/licenses
/licenses/:id
/servers
/servers/:id
/contracts
/contracts/:id
/suppliers
/suppliers/:id
/costs
/timeline
/alerts
/reports
/organization
/settings
```

## Componentes De UI

- Cards de metricas.
- Tabelas com filtros.
- Badges de status.
- Badges de prioridade.
- Timeline vertical.
- Drawer de detalhes rapidos.
- Modal de reajuste.
- Modal de alteracao de quantidade.
- Upload visual de documentos.
- Empty states.
- Skeleton loading.
- Graficos de evolucao de custos.

## Integracao Com API

Use `ApiService` do template.

Nao chamar `fetch` diretamente em componentes.

Crie services por dominio:

- `LicenseService`
- `ServerService`
- `ContractService`
- `SupplierService`
- `CostService`
- `TimelineService`
- `AlertService`
- `ReportService`
- `OrganizationService`

## Autenticacao

O template suporta Authorization Code + PKCE com Keycloak.

Recomendacao de seguranca para a versao final:

- Evoluir para BFF no backend.
- O frontend nao deve acessar tokens diretamente.
- O navegador deve manter apenas cookie HttpOnly de sessao.
- Enquanto usar SPA PKCE, nao usar `localStorage` para tokens.

## Autorizacao Visual

O frontend pode esconder botoes por role/permission, mas isso nao e seguranca.

Permissoes esperadas:

- `assets.read`
- `assets.write`
- `costs.read`
- `costs.write`
- `financial.approve`
- `contracts.read`
- `contracts.write`
- `reports.read`
- `audit.read`
- `tenant.admin`

## UX Financeira

- Sempre exibir valor mensal e anual.
- Sempre exibir impacto de reajuste em valor absoluto e percentual.
- Alteracao de valor deve abrir modal exigindo motivo.
- Alteracao de quantidade deve recalcular total antes de salvar.
- Alertas financeiros devem ter destaque visual.

## Padrao De Dados De Tela

Listagens devem ter:

- Busca textual.
- Filtros avancados.
- Paginacao.
- Ordenacao.
- Colunas principais.
- Acao de detalhe.
- Acao de edicao quando permitido.

Detalhes devem ter:

- Header com status e acoes.
- Resumo financeiro.
- Informacoes principais.
- Vinculos.
- Timeline.
- Documentos.
- Alertas relacionados.
