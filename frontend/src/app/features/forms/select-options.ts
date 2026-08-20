import {
  ADJUSTMENT_INDEX_LABELS,
  CONTRACT_CATEGORY_LABELS,
  ENVIRONMENT_LABELS,
  MANAGEMENT_ROLE_LABELS,
  SERVER_TYPE_LABELS,
  COST_CATEGORY_LABELS,
  COST_TYPE_LABELS,
  UNIT_TYPE_LABELS,
  type AdjustmentIndex,
  type BillingType,
  type ContractCategory,
  type CostCategory,
  type CostType,
  type ContractStatus,
  type ManagementRole,
  type OrganizationalUnitType,
  type ServerEnvironment,
  type ServerType,
  type SupplierStatus,
} from '../models';

/** Opção de um `p-select`. */
export interface SelectOption<T> {
  readonly label: string;
  readonly value: T;
}

/**
 * Converte o mapa de rótulos em opções, preservando a ordem de declaração.
 *
 * Os rótulos já existem para exibir valores em tabelas; reutilizá-los evita
 * que a lista do formulário e a leitura da tela discordem sobre o nome da
 * mesma coisa.
 */
function optionsFrom<T extends string>(labels: Record<T, string>): SelectOption<T>[] {
  return (Object.entries(labels) as [T, string][]).map(([value, label]) =>
    Object.freeze({ value, label }),
  );
}

export const SERVER_TYPE_OPTIONS = optionsFrom<ServerType>(SERVER_TYPE_LABELS);
export const ENVIRONMENT_OPTIONS = optionsFrom<ServerEnvironment>(ENVIRONMENT_LABELS);
export const CONTRACT_CATEGORY_OPTIONS = optionsFrom<ContractCategory>(CONTRACT_CATEGORY_LABELS);
export const ADJUSTMENT_INDEX_OPTIONS = optionsFrom<AdjustmentIndex>(ADJUSTMENT_INDEX_LABELS);
export const UNIT_TYPE_OPTIONS = optionsFrom<OrganizationalUnitType>(UNIT_TYPE_LABELS);
export const MANAGEMENT_ROLE_OPTIONS = optionsFrom<ManagementRole>(MANAGEMENT_ROLE_LABELS);

export const BILLING_TYPE_OPTIONS: SelectOption<BillingType>[] = [
  { label: 'Mensal', value: 'Mensal' },
  { label: 'Anual', value: 'Anual' },
  { label: 'Único', value: 'Unico' },
  { label: 'Sob demanda', value: 'SobDemanda' },
];

export const SUPPLIER_STATUS_OPTIONS: SelectOption<SupplierStatus>[] = [
  { label: 'Ativo', value: 'Ativo' },
  { label: 'Inativo', value: 'Inativo' },
  { label: 'Bloqueado', value: 'Bloqueado' },
];

/**
 * Situações aceitas no encerramento de contrato.
 *
 * Deliberadamente menor que `ContractStatus`: o domínio recusa qualquer outro
 * valor, e oferecer na tela uma opção que o servidor rejeita seria um convite
 * ao erro.
 */
export const CONTRACT_TERMINATION_OPTIONS: SelectOption<ContractStatus>[] = [
  { label: 'Encerrado', value: 'Encerrado' },
  { label: 'Cancelado', value: 'Cancelado' },
  { label: 'Suspenso', value: 'Suspenso' },
];

export const CURRENCY_OPTIONS: SelectOption<string>[] = [
  { label: 'BRL — Real', value: 'BRL' },
  { label: 'USD — Dólar', value: 'USD' },
  { label: 'EUR — Euro', value: 'EUR' },
];

export const COST_TYPE_OPTIONS = optionsFrom<CostType>(COST_TYPE_LABELS);
export const COST_CATEGORY_OPTIONS = optionsFrom<CostCategory>(COST_CATEGORY_LABELS);
