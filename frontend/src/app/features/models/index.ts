import type {
  AdjustmentIndex,
  AdjustmentPeriodicity,
  AlertPriority,
  AlertStatus,
  AlertType,
  AssetKind,
  AssetStatus,
  BillingType,
  ContractCategory,
  ContractStatus,
  CostCategory,
  CostType,
  ManagementRole,
  OrganizationalUnitType,
  PriceAdjustmentTargetType,
  ServerEnvironment,
  ServerType,
  SupplierStatus,
  TimelineEntityType,
  TimelineEventType,
} from './enums';

export * from './enums';

/** Página de resultados devolvida pelas listagens da API. */
export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly hasPrevious: boolean;
  readonly hasNext: boolean;
}

// ------------------------------------------------------------------ Licenças

export interface LicenseListItem {
  readonly id: string;
  readonly name: string;
  readonly code: string;
  readonly manufacturer: string;
  readonly product: string;
  readonly plan: string | null;
  readonly status: AssetStatus;
  readonly billingType: BillingType;
  readonly contractedQuantity: number;
  readonly usedQuantity: number;
  readonly availableQuantity: number;
  readonly utilizationRate: number;
  readonly unitPrice: number;
  readonly monthlyAmount: number;
  readonly annualAmount: number;
  readonly currency: string;
  readonly supplierName: string | null;
  readonly organizationalUnitName: string;
  readonly renewalDate: string | null;
  readonly lastAdjustmentDate: string | null;
  readonly nextAdjustmentDate: string | null;
}

export interface LicenseDetail extends LicenseListItem {
  readonly description: string | null;
  readonly organizationalUnitId: string;
  readonly organizationalUnitPath: string;
  readonly supplierId: string | null;
  readonly contractId: string | null;
  readonly contractNumber: string | null;
  readonly costCenterId: string | null;
  readonly costCenterName: string | null;
  readonly technicalResponsibleUserId: string | null;
  readonly technicalResponsibleName: string | null;
  readonly financialResponsibleUserId: string | null;
  readonly financialResponsibleName: string | null;
  readonly startDate: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface ListLicensesQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: AssetStatus;
  manufacturer?: string;
  supplierId?: string;
  organizationalUnitId?: string;
  costCenterId?: string;
  renewalWithinDays?: number;
  utilizationBelow?: number;
  utilizationAbove?: number;
  sortBy?: 'Name' | 'MonthlyAmount' | 'RenewalDate' | 'Utilization' | 'CreatedAt';
  descending?: boolean;
}

export interface CreateLicenseCommand {
  name: string;
  code: string;
  manufacturer: string;
  product: string;
  plan?: string | null;
  description?: string | null;
  organizationalUnitId: string;
  contractedQuantity: number;
  unitPrice: number;
  currency?: string;
  billingType?: BillingType;
  supplierId?: string | null;
  contractId?: string | null;
  costCenterId?: string | null;
  technicalResponsibleUserId?: string | null;
  financialResponsibleUserId?: string | null;
  startDate?: string | null;
  renewalDate?: string | null;
}

/** O cliente informa só o valor novo; o backend calcula diferença e percentual. */
export interface ApplyPriceAdjustmentCommand {
  newMonthlyAmount: number;
  effectiveDate: string;
  reason: string;
  indexApplied?: AdjustmentIndex;
  approvedByUserId?: string | null;
}

export interface PriceAdjustmentResult {
  readonly id: string;
  readonly previousAmount: number;
  readonly newAmount: number;
  readonly absoluteDifference: number;
  readonly percentageDifference: number;
  readonly annualImpact: number;
  readonly currency: string;
  readonly effectiveDate: string;
  readonly reason: string;
  readonly indexApplied: AdjustmentIndex;
  readonly exceedsAlertThreshold: boolean;
}

export interface ChangeLicenseQuantityCommand {
  newQuantity: number;
  reason: string;
}

export interface QuantityChangeResult {
  readonly licenseId: string;
  readonly previousQuantity: number;
  readonly newQuantity: number;
  readonly previousMonthlyAmount: number;
  readonly newMonthlyAmount: number;
  readonly monthlyImpact: number;
  readonly annualImpact: number;
  readonly currency: string;
}

export interface SimulationResult {
  readonly currentAmount: number;
  readonly newAmount: number;
  readonly absoluteDifference: number;
  readonly percentageDifference: number;
  readonly monthlyImpact: number;
  readonly annualImpact: number;
  readonly currency: string;
}

// ---------------------------------------------------------------- Servidores

export interface ServerListItem {
  readonly id: string;
  readonly name: string;
  readonly code: string;
  readonly hostname: string;
  readonly serverType: ServerType;
  readonly environment: ServerEnvironment;
  readonly status: AssetStatus;
  readonly operatingSystem: string | null;
  readonly provider: string | null;
  readonly cpuCores: number | null;
  readonly memoryGb: number | null;
  readonly storageGb: number | null;
  readonly monthlyAmount: number;
  readonly annualAmount: number;
  readonly currency: string;
  readonly technicalResponsibleName: string | null;
  readonly organizationalUnitName: string;
  readonly lastBackupAt: string | null;
  readonly hasResponsible: boolean;
}

export interface ServerDetail extends ServerListItem {
  readonly description: string | null;
  readonly operatingSystemVersion: string | null;
  readonly regionOrDatacenter: string | null;
  readonly primaryIp: string | null;
  readonly backupPolicy: string | null;
  readonly sla: string | null;
  readonly maintenanceWindow: string | null;
  readonly infrastructureMonthlyCost: number;
  readonly licenseMonthlyCost: number;
  readonly supportMonthlyCost: number;
  readonly backupMonthlyCost: number;
  readonly organizationalUnitId: string;
  readonly organizationalUnitPath: string;
  readonly supplierId: string | null;
  readonly supplierName: string | null;
  readonly contractId: string | null;
  readonly contractNumber: string | null;
  readonly costCenterId: string | null;
  readonly costCenterName: string | null;
  readonly technicalResponsibleUserId: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface ListServersQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: AssetStatus;
  environment?: ServerEnvironment;
  serverType?: ServerType;
  provider?: string;
  organizationalUnitId?: string;
  withoutResponsible?: boolean;
  withoutBackupForDays?: number;
  monthlyCostAbove?: number;
  sortBy?: 'Name' | 'MonthlyAmount' | 'Environment' | 'CreatedAt';
  descending?: boolean;
}

export interface CreateServerCommand {
  name: string;
  code: string;
  hostname: string;
  organizationalUnitId: string;
  serverType: ServerType;
  environment: ServerEnvironment;
  infrastructureMonthlyCost: number;
  currency?: string;
  description?: string | null;
  operatingSystem?: string | null;
  operatingSystemVersion?: string | null;
  provider?: string | null;
  regionOrDatacenter?: string | null;
  primaryIp?: string | null;
  cpuCores?: number | null;
  memoryGb?: number | null;
  storageGb?: number | null;
  supplierId?: string | null;
  contractId?: string | null;
  costCenterId?: string | null;
  technicalResponsibleUserId?: string | null;
}

export interface UpdateServerCostsCommand {
  infrastructureMonthlyCost: number;
  licenseMonthlyCost: number;
  supportMonthlyCost: number;
  backupMonthlyCost: number;
  reason: string;
  currency?: string;
}

export interface ServerCostChangeResult {
  readonly serverId: string;
  readonly previousMonthlyAmount: number;
  readonly newMonthlyAmount: number;
  readonly monthlyImpact: number;
  readonly annualImpact: number;
  readonly currency: string;
}

export interface ResizeServerCommand {
  cpuCores?: number | null;
  memoryGb?: number | null;
  storageGb?: number | null;
  reason: string;
}

// ----------------------------------------------------------------- Contratos

export interface ContractListItem {
  readonly id: string;
  readonly number: string;
  readonly name: string;
  readonly category: ContractCategory;
  readonly status: ContractStatus;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly organizationalUnitName: string;
  readonly monthlyAmount: number;
  readonly annualAmount: number;
  readonly currency: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly daysUntilExpiration: number;
  readonly adjustmentIndex: AdjustmentIndex;
  readonly nextAdjustmentDate: string | null;
  readonly linkedAssetCount: number;
  readonly internalResponsibleName: string | null;
}

export interface LinkedAsset {
  readonly id: string;
  readonly name: string;
  readonly code: string;
  readonly kind: AssetKind;
  readonly status: AssetStatus;
  readonly monthlyAmount: number;
}

export interface ContractDetail extends ContractListItem {
  readonly description: string | null;
  readonly organizationalUnitId: string;
  readonly organizationalUnitPath: string;
  readonly adjustmentPeriodicity: AdjustmentPeriodicity;
  readonly lastAdjustmentDate: string | null;
  readonly internalResponsibleUserId: string | null;
  readonly linkedAssets: readonly LinkedAsset[];
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface ListContractsQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: ContractStatus;
  category?: ContractCategory;
  supplierId?: string;
  organizationalUnitId?: string;
  expiringWithinDays?: number;
  sortBy?: 'Number' | 'Name' | 'MonthlyAmount' | 'EndDate';
  descending?: boolean;
}

export interface CreateContractCommand {
  number: string;
  name: string;
  supplierId: string;
  organizationalUnitId: string;
  category: ContractCategory;
  startDate: string;
  endDate: string;
  monthlyAmount: number;
  currency?: string;
  description?: string | null;
  adjustmentIndex?: AdjustmentIndex;
  adjustmentPeriodicity?: AdjustmentPeriodicity;
  internalResponsibleUserId?: string | null;
}

export interface RenewContractCommand {
  newEndDate: string;
  reason?: string | null;
}

// --------------------------------------------------------------- Fornecedores

export interface SupplierListItem {
  readonly id: string;
  readonly name: string;
  readonly documentNumber: string | null;
  readonly category: ContractCategory | null;
  readonly status: SupplierStatus;
  readonly mainContactName: string | null;
  readonly email: string | null;
  readonly phone: string | null;
  readonly activeContracts: number;
  readonly linkedAssets: number;
  readonly monthlyAmount: number;
  readonly annualAmount: number;
  readonly currency: string;
}

export interface SupplierContract {
  readonly id: string;
  readonly number: string;
  readonly name: string;
  readonly status: ContractStatus;
  readonly monthlyAmount: number;
  readonly endDate: string;
}

export interface SupplierDetail extends SupplierListItem {
  readonly website: string | null;
  readonly slaDescription: string | null;
  readonly contracts: readonly SupplierContract[];
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface CreateSupplierCommand {
  name: string;
  documentNumber?: string | null;
  category?: ContractCategory | null;
  mainContactName?: string | null;
  email?: string | null;
  phone?: string | null;
  website?: string | null;
  slaDescription?: string | null;
}

// --------------------------------------------------------- Custos e reajustes

export interface PriceAdjustmentListItem {
  readonly id: string;
  readonly targetType: PriceAdjustmentTargetType;
  readonly targetId: string;
  readonly targetName: string;
  readonly previousAmount: number;
  readonly newAmount: number;
  readonly absoluteDifference: number;
  readonly percentageDifference: number;
  readonly annualImpact: number;
  readonly currency: string;
  readonly effectiveDate: string;
  readonly reason: string;
  readonly indexApplied: AdjustmentIndex;
  readonly createdByName: string | null;
  readonly approvedByName: string | null;
  readonly organizationalUnitName: string;
  readonly exceedsThreshold: boolean;
}

export interface ListPriceAdjustmentsQuery {
  page?: number;
  pageSize?: number;
  targetType?: PriceAdjustmentTargetType;
  targetId?: string;
  organizationalUnitId?: string;
  from?: string;
  to?: string;
  onlyAboveThreshold?: boolean;
  onlyIncreases?: boolean;
}

export interface CostByGroup {
  readonly label: string;
  readonly id: string | null;
  readonly monthlyAmount: number;
  readonly annualAmount: number;
  readonly itemCount: number;
}

export interface CostSummary {
  readonly currentMonthlyCost: number;
  readonly projectedAnnualCost: number;
  readonly currency: string;
  readonly adjustedThisMonth: number;
  readonly accumulatedMonthlyImpact: number;
  readonly accumulatedAnnualImpact: number;
  readonly highestPercentageIncrease: number | null;
  readonly highestAbsoluteIncrease: number | null;
  readonly assetsWithoutAdjustmentOver12Months: number;
  readonly adjustmentsInPeriod: number;
  readonly byCategory: readonly CostByGroup[];
  readonly bySupplier: readonly CostByGroup[];
  readonly byOrganizationalUnit: readonly CostByGroup[];
}

// ------------------------------------------------------------------ Timeline

export interface TimelineEvent {
  readonly id: string;
  readonly entityType: TimelineEntityType;
  readonly entityId: string;
  readonly eventType: TimelineEventType;
  readonly title: string;
  readonly description: string;
  readonly occurredAt: string;
  readonly userId: string | null;
  readonly userDisplayName: string | null;
  readonly previousAmount: number | null;
  readonly newAmount: number | null;
  readonly financialImpact: number | null;
  readonly organizationalUnitId: string | null;
  readonly organizationalUnitName: string | null;
  readonly correlationId: string | null;
}

export interface ListTimelineQuery {
  page?: number;
  pageSize?: number;
  entityType?: TimelineEntityType;
  entityId?: string;
  eventType?: TimelineEventType;
  userId?: string;
  organizationalUnitId?: string;
  from?: string;
  to?: string;
  onlyFinancial?: boolean;
}

// ------------------------------------------------------------------- Alertas

export interface Alert {
  readonly id: string;
  readonly type: AlertType;
  readonly priority: AlertPriority;
  readonly status: AlertStatus;
  readonly entityType: TimelineEntityType;
  readonly entityId: string;
  readonly title: string;
  readonly description: string;
  readonly recommendedAction: string;
  readonly dueDate: string | null;
  readonly assignedToUserId: string | null;
  readonly assignedToName: string | null;
  readonly organizationalUnitName: string;
  readonly createdAt: string;
  readonly resolvedAt: string | null;
  readonly resolutionNotes: string | null;
}

export interface ListAlertsQuery {
  page?: number;
  pageSize?: number;
  status?: AlertStatus;
  priority?: AlertPriority;
  type?: AlertType;
  organizationalUnitId?: string;
  onlyOpen?: boolean;
}

export interface AlertScanResult {
  readonly created: number;
  readonly alreadyOpen: number;
  readonly autoResolved: number;
  readonly newAlerts: readonly Alert[];
}

// ----------------------------------------------------------------- Dashboard

export interface CostByCategory {
  readonly category: string;
  readonly monthlyAmount: number;
  readonly annualAmount: number;
}

export interface TopCost {
  readonly assetId: string;
  readonly name: string;
  readonly kind: string;
  readonly monthlyAmount: number;
  readonly annualAmount: number;
}

export interface ServersByEnvironment {
  readonly environment: string;
  readonly count: number;
  readonly monthlyAmount: number;
}

export interface DashboardSummary {
  readonly currentMonthlyCost: number;
  readonly projectedAnnualCost: number;
  readonly currency: string;
  readonly activeLicenses: number;
  readonly activeServers: number;
  readonly totalContractedLicenses: number;
  readonly totalUsedLicenses: number;
  readonly idleLicenses: number;
  readonly contractsExpiringIn30Days: number;
  readonly contractsExpiringIn60Days: number;
  readonly contractsExpiringIn90Days: number;
  readonly openAlerts: number;
  readonly criticalAlerts: number;
  readonly costsByCategory: readonly CostByCategory[];
  readonly topCosts: readonly TopCost[];
  readonly serversByEnvironment: readonly ServersByEnvironment[];
  readonly latestEvents: readonly TimelineEvent[];
}

// -------------------------------------------------------------- Organização

export interface OrganizationalUnitTree {
  readonly id: string;
  readonly parentId: string | null;
  readonly type: OrganizationalUnitType;
  readonly name: string;
  readonly code: string;
  readonly path: string;
  readonly level: number;
  readonly isActive: boolean;
  readonly assetCount: number;
  readonly monthlyCost: number;
  readonly children: readonly OrganizationalUnitTree[];
}

export interface ManagementAssignment {
  readonly id: string;
  readonly userId: string;
  readonly userDisplayName: string;
  readonly userEmail: string;
  readonly organizationalUnitId: string;
  readonly organizationalUnitName: string;
  readonly organizationalUnitPath: string;
  readonly organizationalUnitType: OrganizationalUnitType;
  readonly role: ManagementRole;
  readonly startDate: string;
  readonly endDate: string | null;
  readonly isPrimary: boolean;
  readonly includesDescendants: boolean;
  readonly isActive: boolean;
}

// ----------------------------------------------------------------- Relatórios

export interface CostEvolutionPoint {
  readonly year: number;
  readonly month: number;
  readonly label: string;
  readonly amount: number;
  readonly currency: string;
}

export interface CostEvolutionReport {
  readonly history: readonly CostEvolutionPoint[];
  readonly projection: readonly CostEvolutionPoint[];
  readonly currentMonthlyCost: number;
  readonly projectedAnnualCost: number;
  readonly currency: string;
}

export interface LicenseUtilizationReport {
  readonly id: string;
  readonly name: string;
  readonly product: string;
  readonly contractedQuantity: number;
  readonly usedQuantity: number;
  readonly availableQuantity: number;
  readonly utilizationRate: number;
  readonly monthlyAmount: number;
  readonly potentialMonthlySavings: number;
  readonly currency: string;
  readonly organizationalUnitName: string;
}

export interface ContractExpirationReport {
  readonly id: string;
  readonly number: string;
  readonly name: string;
  readonly supplierName: string;
  readonly monthlyAmount: number;
  readonly currency: string;
  readonly endDate: string;
  readonly daysUntilExpiration: number;
  readonly organizationalUnitName: string;
}

// ---------------------------------------------------------------- Comandos de escrita
//
// O backend não expõe update genérico: depois de criado, um ativo só muda pelos
// endpoints de ação abaixo. Cada um registra um evento na timeline, e é por isso
// que quase todos exigem `reason` — o histórico precisa dizer *por que* mudou,
// não apenas o que mudou.

export interface UpdateLicenseUsageCommand {
  usedQuantity: number;
}

export interface DecommissionServerCommand {
  reason: string;
}

/**
 * Encerramento de contrato.
 *
 * `status` aceita apenas `Encerrado`, `Cancelado` ou `Suspenso`; os demais
 * valores de `ContractStatus` são rejeitados pelo domínio.
 */
export interface TerminateContractCommand {
  status: ContractStatus;
  reason: string;
}

/**
 * Atualização de fornecedor.
 *
 * Atenção: contato, e-mail, telefone e site são *substituição total* — omitir
 * um campo apaga o valor gravado. O formulário deve sempre enviar o conjunto
 * completo, e não apenas o que o usuário tocou.
 */
export interface UpdateSupplierCommand {
  mainContactName?: string | null;
  email?: string | null;
  phone?: string | null;
  website?: string | null;
  category?: ContractCategory | null;
  slaDescription?: string | null;
  status?: SupplierStatus | null;
}

export interface ResolveAlertCommand {
  notes: string;
}

export interface IgnoreAlertCommand {
  justification: string;
}

export interface CreateOrganizationalUnitCommand {
  type: OrganizationalUnitType;
  name: string;
  code: string;
  parentId?: string | null;
  description?: string | null;
}

export interface CreateManagementAssignmentCommand {
  userId: string;
  organizationalUnitId: string;
  role: ManagementRole;
  startDate: string;
  isPrimary?: boolean;
  includesDescendants?: boolean;
}

export interface FinishManagementAssignmentCommand {
  endDate: string;
  notes?: string | null;
}

/**
 * Correção cadastral de licença.
 *
 * Separada de `CreateLicenseCommand` de propósito: criar exige quantidade e
 * preço, corrigir não os toca. Um comando único faria a tela de edição
 * carregar e reenviar valores financeiros que ela não deveria alterar.
 */
export interface UpdateLicenseCommand {
  readonly name: string;
  readonly code: string;
  readonly manufacturer: string;
  readonly product: string;
  readonly plan?: string | null;
  readonly description?: string | null;
  readonly billingType: BillingType;
  readonly startDate?: string | null;
  readonly renewalDate?: string | null;
}

/** Correção cadastral de servidor. Não inclui custos nem recursos. */
export interface UpdateServerCommand {
  readonly name: string;
  readonly code: string;
  readonly hostname: string;
  readonly serverType: ServerType;
  readonly environment: ServerEnvironment;
  readonly description?: string | null;
  readonly operatingSystem?: string | null;
  readonly operatingSystemVersion?: string | null;
  readonly provider?: string | null;
  readonly regionOrDatacenter?: string | null;
  readonly primaryIp?: string | null;
}

/** Correção cadastral de unidade organizacional. Código e tipo permanecem. */
export interface UpdateOrganizationalUnitCommand {
  readonly name: string;
  readonly description?: string | null;
}

/** Elo da cadeia hierárquica acima de uma unidade. */
export interface OrganizationalUnitBreadcrumb {
  readonly id: string;
  readonly name: string;
  readonly code: string;
}

/**
 * Detalhe de uma unidade organizacional.
 *
 * `deletionBlockers` chega junto do detalhe de propósito: a tela precisa saber
 * o que impede a exclusão *antes* de oferecer o botão. Descobrir só depois de
 * clicar faria o usuário percorrer o caminho inteiro para chegar a um "não".
 */
export interface OrganizationalUnitDetail {
  readonly id: string;
  readonly parentId: string | null;
  readonly parentName: string | null;
  readonly type: OrganizationalUnitType;
  readonly name: string;
  readonly code: string;
  readonly path: string;
  readonly description: string | null;
  readonly level: number;
  readonly isActive: boolean;
  readonly assetCount: number;
  readonly licenseCount: number;
  readonly serverCount: number;
  readonly contractCount: number;
  readonly childrenCount: number;
  readonly monthlyCost: number;
  readonly annualCost: number;
  readonly ancestors: readonly OrganizationalUnitBreadcrumb[];
  readonly children: readonly OrganizationalUnitTree[];
  readonly assignments: readonly ManagementAssignment[];
  readonly deletionBlockers: readonly string[];
}

/**
 * Lançamento de custo.
 *
 * A competência é o mês a que a despesa se refere; a data efetiva é quando ela
 * ocorreu. Separá-las importa porque uma nota paga em setembro pode se referir
 * a agosto — e é a competência que faz o relatório mensal fechar.
 */
export interface CreateCostEntryCommand {
  readonly organizationalUnitId: string;
  readonly type: CostType;
  readonly category: CostCategory;
  readonly amount: number;
  readonly competenceMonth: string;
  readonly description: string;
  readonly effectiveDate?: string | null;
  readonly currency?: string;
  readonly assetId?: string | null;
  readonly contractId?: string | null;
}

export interface CostEntry {
  readonly id: string;
  readonly organizationalUnitId: string;
  readonly organizationalUnitName: string;
  readonly assetId: string | null;
  readonly assetName: string | null;
  readonly contractId: string | null;
  readonly contractNumber: string | null;
  readonly supplierId: string | null;
  readonly supplierName: string | null;
  readonly type: CostType;
  readonly category: CostCategory;
  readonly amount: number;
  readonly currency: string;
  readonly competenceMonth: string;
  readonly effectiveDate: string;
  readonly description: string;
}

/**
 * Metadado de um anexo. O conteúdo vem por download separado — a listagem
 * carrega apenas o que a tela precisa mostrar.
 */
export interface DocumentItem {
  readonly id: string;
  readonly entityType: TimelineEntityType;
  readonly entityId: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeInBytes: number;
  readonly sha256: string;
  readonly description: string | null;
  readonly uploadedByUserId: string;
  readonly uploadedByName: string | null;
  readonly createdAt: string;
}

/** Limites espelhados do domínio, para recusar antes da viagem ao servidor. */
export const DOCUMENT_MAX_SIZE_BYTES = 25 * 1024 * 1024;

export const DOCUMENT_ALLOWED_TYPES: readonly string[] = [
  'application/pdf',
  'image/png',
  'image/jpeg',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'text/csv',
];
