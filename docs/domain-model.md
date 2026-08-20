# Modelo De Dominio

## Hierarquia Organizacional

A modelagem deve suportar a estrutura institucional e relacoes flexiveis de gestao.

Hierarquia principal:

```text
Tenant Matriz/Filial
└── Negocios
    └── Areas Tecnologicas
        └── Unidades Operacionais
            └── Nucleos de Suporte
```

Importante: um gerente pode liderar mais de uma area, unidade ou nucleo. Portanto, nao modele gerente como campo unico fixo na entidade. Use uma tabela de atribuicoes gerenciais.

## Entidades Organizacionais

### Tenant

Representa uma instancia logica isolada do sistema.

Campos:

- `Id`
- `Name`
- `Slug`
- `Status`
- `CreatedAt`
- `UpdatedAt`

### OrganizationalUnit

Representa qualquer no da arvore organizacional.

Campos:

- `Id`
- `TenantId`
- `ParentId`
- `Type`: Matriz, Filial, Negocio, AreaTecnologica, UnidadeOperacional, NucleoSuporte
- `Name`
- `Code`
- `Path`
- `Level`
- `Status`

Modelo recomendado: adjacency list com `ParentId`, `Path` materializado para consultas rapidas e indice por tenant.

### ManagementAssignment

Permite que um gerente lidere multiplos nos organizacionais.

Campos:

- `Id`
- `TenantId`
- `UserId`
- `OrganizationalUnitId`
- `Role`: Gerente, Coordenador, ResponsavelTecnico, ResponsavelFinanceiro
- `StartDate`
- `EndDate`
- `IsPrimary`

Regra: o mesmo usuario pode ter varias atribuicoes ativas em unidades diferentes.

## Entidades De Ativos

### Asset

Entidade base para qualquer item controlado.

Campos:

- `Id`
- `TenantId`
- `OrganizationalUnitId`
- `CategoryId`
- `SupplierId`
- `ContractId`
- `Name`
- `Code`
- `Description`
- `Status`
- `OwnerUserId`
- `TechnicalResponsibleUserId`
- `FinancialResponsibleUserId`
- `CostCenterId`
- `CreatedAt`
- `UpdatedAt`

### LicenseAsset

Especializacao para licencas.

Campos:

- `AssetId`
- `Manufacturer`
- `Product`
- `Plan`
- `BillingType`
- `ContractedQuantity`
- `UsedQuantity`
- `AvailableQuantity`
- `UnitPrice`
- `MonthlyAmount`
- `AnnualAmount`
- `Currency`
- `StartDate`
- `RenewalDate`
- `LastAdjustmentDate`
- `NextAdjustmentDate`

### ServerAsset

Especializacao para servidores.

Campos:

- `AssetId`
- `Hostname`
- `ServerType`
- `Environment`
- `OperatingSystem`
- `OperatingSystemVersion`
- `Provider`
- `RegionOrDatacenter`
- `PrimaryIp`
- `Cpu`
- `MemoryGb`
- `StorageGb`
- `BackupPolicy`
- `LastBackupAt`
- `Sla`
- `MaintenanceWindow`
- `InfrastructureMonthlyCost`
- `LicenseMonthlyCost`
- `SupportMonthlyCost`
- `BackupMonthlyCost`
- `TotalMonthlyCost`

## Contratos E Fornecedores

### Contract

Campos:

- `Id`
- `TenantId`
- `SupplierId`
- `OrganizationalUnitId`
- `Number`
- `Name`
- `Category`
- `StartDate`
- `EndDate`
- `MonthlyAmount`
- `AnnualAmount`
- `Currency`
- `AdjustmentIndex`
- `AdjustmentPeriodicity`
- `Status`
- `InternalResponsibleUserId`

### Supplier

Campos:

- `Id`
- `TenantId`
- `Name`
- `DocumentNumber`
- `Category`
- `MainContactName`
- `Email`
- `Phone`
- `Website`
- `SlaDescription`
- `Status`

## Custos E Reajustes

### CostEntry

Registra custos recorrentes ou pontuais.

Campos:

- `Id`
- `TenantId`
- `AssetId`
- `ContractId`
- `SupplierId`
- `Type`: Mensal, Anual, Pontual, SobDemanda
- `Category`
- `Amount`
- `Currency`
- `CompetenceMonth`
- `EffectiveDate`
- `Description`

### PriceAdjustment

Campos:

- `Id`
- `TenantId`
- `TargetType`: Asset, License, Server, Contract, SupplierService
- `TargetId`
- `PreviousAmount`
- `NewAmount`
- `AbsoluteDifference`
- `PercentageDifference`
- `Currency`
- `EffectiveDate`
- `Reason`
- `IndexApplied`
- `ApprovedByUserId`
- `CreatedByUserId`
- `CreatedAt`

## Auditoria E Timeline

### TimelineEvent

Campos:

- `Id`
- `TenantId`
- `OrganizationalUnitId`
- `EntityType`
- `EntityId`
- `EventType`
- `Title`
- `Description`
- `OccurredAt`
- `UserId`
- `PreviousDataJson`
- `NewDataJson`
- `FinancialImpactJson`
- `CorrelationId`

### AuditLog

Campos:

- `Id`
- `TenantId`
- `UserId`
- `Action`
- `EntityType`
- `EntityId`
- `IpAddress`
- `UserAgent`
- `OccurredAt`
- `CorrelationId`
- `MetadataJson`
