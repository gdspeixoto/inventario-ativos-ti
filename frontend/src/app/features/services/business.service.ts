import { Injectable, inject } from '@angular/core';
import { ApiService } from '@core/services/api.service';
import type {
  CostEntry,
  CreateCostEntryCommand,
  OrganizationalUnitDetail,
  UpdateOrganizationalUnitCommand,
  Alert,
  AlertScanResult,
  ContractDetail,
  ContractExpirationReport,
  ContractListItem,
  CostEvolutionReport,
  CostSummary,
  DashboardSummary,
  LicenseUtilizationReport,
  ListAlertsQuery,
  ListContractsQuery,
  ListPriceAdjustmentsQuery,
  PagedResult,
  PriceAdjustmentListItem,
  SupplierDetail,
  SupplierListItem,
  TimelineEvent,
  ListTimelineQuery,
  OrganizationalUnitTree,
  ManagementAssignment,
  CreateContractCommand,
  RenewContractCommand,
  TerminateContractCommand,
  ApplyPriceAdjustmentCommand,
  PriceAdjustmentResult,
  CreateSupplierCommand,
  UpdateSupplierCommand,
  ResolveAlertCommand,
  IgnoreAlertCommand,
  CreateOrganizationalUnitCommand,
  CreateManagementAssignmentCommand,
  FinishManagementAssignmentCommand,
} from '../models';
import { toQueryParams } from './asset.service';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly api = inject(ApiService);

  summary(): Promise<DashboardSummary> {
    return this.api.get<DashboardSummary>('/dashboard/summary');
  }
}

@Injectable({ providedIn: 'root' })
export class ContractApiService {
  private readonly api = inject(ApiService);

  list(query: ListContractsQuery = {}): Promise<PagedResult<ContractListItem>> {
    return this.api.get<PagedResult<ContractListItem>>('/contracts', {
      params: toQueryParams({ ...query }),
    });
  }

  create(command: CreateContractCommand): Promise<{ id: string }> {
    return this.api.post<{ id: string }>('/contracts', command);
  }

  /** Estende a vigência. Não altera valores — para isso existe o reajuste. */
  renew(id: string, command: RenewContractCommand): Promise<void> {
    return this.api.post<void>(`/contracts/${id}/renew`, command);
  }

  /**
   * Remove o contrato em definitivo. O servidor recusa com 409 havendo ativos
   * ou lançamentos vinculados — nesse caso o caminho é encerrar.
   */
  delete(id: string): Promise<void> {
    return this.api.delete<void>(`/contracts/${id}`);
  }

  terminate(id: string, command: TerminateContractCommand): Promise<void> {
    return this.api.post<void>(`/contracts/${id}/terminate`, command);
  }

  applyPriceAdjustment(
    id: string,
    command: ApplyPriceAdjustmentCommand,
  ): Promise<PriceAdjustmentResult> {
    return this.api.post<PriceAdjustmentResult>(`/contracts/${id}/price-adjustments`, command);
  }

  get(id: string): Promise<ContractDetail> {
    return this.api.get<ContractDetail>(`/contracts/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class SupplierApiService {
  private readonly api = inject(ApiService);

  list(
    query: { page?: number; pageSize?: number; search?: string } = {},
  ): Promise<PagedResult<SupplierListItem>> {
    return this.api.get<PagedResult<SupplierListItem>>('/suppliers', {
      params: toQueryParams({ ...query }),
    });
  }

  create(command: CreateSupplierCommand): Promise<{ id: string }> {
    return this.api.post<{ id: string }>('/suppliers', command);
  }

  /**
   * Atualiza o fornecedor.
   *
   * Contato, e-mail, telefone e site são substituição total: o backend grava o
   * que receber, inclusive vazio. Envie sempre o conjunto completo.
   */
  update(id: string, command: UpdateSupplierCommand): Promise<void> {
    return this.api.put<void>(`/suppliers/${id}`, command);
  }

  get(id: string): Promise<SupplierDetail> {
    return this.api.get<SupplierDetail>(`/suppliers/${id}`);
  }

  /**
   * Remove o fornecedor em definitivo. Havendo ativos, contratos ou
   * lançamentos vinculados, o servidor recusa com 409 — nesse caso o caminho é
   * marcá-lo como inativo.
   */
  delete(id: string): Promise<void> {
    return this.api.delete<void>(`/suppliers/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class CostApiService {
  private readonly api = inject(ApiService);

  summary(): Promise<CostSummary> {
    return this.api.get<CostSummary>('/costs/summary');
  }

  /** Lançamentos de custo já registrados. */
  listEntries(params: { from?: string; to?: string } = {}): Promise<CostEntry[]> {
    return this.api.get<CostEntry[]>('/costs/entries', { params: toQueryParams({ ...params }) });
  }

  /** Registra um custo avulso, fora dos valores recorrentes dos ativos. */
  createEntry(command: CreateCostEntryCommand): Promise<{ id: string }> {
    return this.api.post<{ id: string }>('/costs/entries', command);
  }

  adjustments(
    query: ListPriceAdjustmentsQuery = {},
  ): Promise<PagedResult<PriceAdjustmentListItem>> {
    return this.api.get<PagedResult<PriceAdjustmentListItem>>('/costs/price-adjustments', {
      params: toQueryParams({ ...query }),
    });
  }
}

@Injectable({ providedIn: 'root' })
export class TimelineApiService {
  private readonly api = inject(ApiService);

  list(query: ListTimelineQuery = {}): Promise<PagedResult<TimelineEvent>> {
    return this.api.get<PagedResult<TimelineEvent>>('/timeline', {
      params: toQueryParams({ ...query }),
    });
  }
}

@Injectable({ providedIn: 'root' })
export class AlertApiService {
  private readonly api = inject(ApiService);

  list(query: ListAlertsQuery = {}): Promise<PagedResult<Alert>> {
    return this.api.get<PagedResult<Alert>>('/alerts', {
      params: toQueryParams({ ...query }),
    });
  }

  resolve(id: string, command: ResolveAlertCommand): Promise<void> {
    return this.api.post<void>(`/alerts/${id}/resolve`, command);
  }

  ignore(id: string, command: IgnoreAlertCommand): Promise<void> {
    return this.api.post<void>(`/alerts/${id}/ignore`, command);
  }

  scan(): Promise<AlertScanResult> {
    return this.api.post<AlertScanResult>('/alerts/scan', null);
  }
}

@Injectable({ providedIn: 'root' })
export class ReportApiService {
  private readonly api = inject(ApiService);

  costEvolution(months = 12): Promise<CostEvolutionReport> {
    return this.api.get<CostEvolutionReport>('/reports/cost-evolution', {
      params: { months },
    });
  }

  licenseUtilization(utilizationBelow = 60): Promise<readonly LicenseUtilizationReport[]> {
    return this.api.get<readonly LicenseUtilizationReport[]>('/reports/license-utilization', {
      params: { utilizationBelow },
    });
  }

  contractExpirations(withinDays = 90): Promise<readonly ContractExpirationReport[]> {
    return this.api.get<readonly ContractExpirationReport[]>('/reports/contract-expirations', {
      params: { withinDays },
    });
  }
}

@Injectable({ providedIn: 'root' })
export class OrganizationApiService {
  private readonly api = inject(ApiService);

  tree(): Promise<readonly OrganizationalUnitTree[]> {
    return this.api.get<readonly OrganizationalUnitTree[]>('/organization/units');
  }

  createUnit(command: CreateOrganizationalUnitCommand): Promise<{ id: string }> {
    return this.api.post<{ id: string }>('/organization/units', command);
  }

  /** Detalhe de uma unidade, com hierarquia, números e impedimentos de exclusão. */
  getUnit(id: string): Promise<OrganizationalUnitDetail> {
    return this.api.get<OrganizationalUnitDetail>(`/organization/units/${id}`);
  }

  /** Corrige nome e descrição da unidade. Código e tipo permanecem. */
  updateUnit(id: string, command: UpdateOrganizationalUnitCommand): Promise<void> {
    return this.api.put<void>(`/organization/units/${id}`, command);
  }

  /**
   * Remove a unidade em definitivo.
   *
   * O servidor recusa com 409 e lista o que impede — unidades subordinadas,
   * ativos, contratos — para que o usuário saiba o que precisa mover antes,
   * ou opte por desativar.
   */
  deleteUnit(id: string): Promise<void> {
    return this.api.delete<void>(`/organization/units/${id}`);
  }

  createAssignment(command: CreateManagementAssignmentCommand): Promise<{ id: string }> {
    return this.api.post<{ id: string }>('/organization/management-assignments', command);
  }

  finishAssignment(id: string, command: FinishManagementAssignmentCommand): Promise<void> {
    return this.api.post<void>(`/organization/management-assignments/${id}/finish`, command);
  }

  assignments(onlyActive = true): Promise<readonly ManagementAssignment[]> {
    return this.api.get<readonly ManagementAssignment[]>('/organization/management-assignments', {
      params: { onlyActive },
    });
  }
}
