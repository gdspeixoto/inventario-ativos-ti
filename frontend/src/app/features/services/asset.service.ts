import { Injectable, inject } from '@angular/core';
import { ApiService, type QueryParams } from '@core/services/api.service';
import type {
  UpdateLicenseCommand,
  UpdateServerCommand,
  ApplyPriceAdjustmentCommand,
  ChangeLicenseQuantityCommand,
  CreateLicenseCommand,
  CreateServerCommand,
  LicenseDetail,
  LicenseListItem,
  ListLicensesQuery,
  ListServersQuery,
  PagedResult,
  PriceAdjustmentResult,
  QuantityChangeResult,
  ResizeServerCommand,
  ServerCostChangeResult,
  ServerDetail,
  ServerListItem,
  SimulationResult,
  UpdateServerCostsCommand,
} from '../models';

/**
 * Remove chaves nulas/vazias antes de montar a query string.
 *
 * Sem isso, filtros não preenchidos viriam como `status=` e o backend trataria
 * a string vazia como valor, retornando lista vazia em vez de todos os itens.
 */
export function toQueryParams(source: Record<string, unknown>): QueryParams {
  const params: QueryParams = {};

  for (const [key, value] of Object.entries(source)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }

    params[key] = value as QueryParams[string];
  }

  return params;
}

@Injectable({ providedIn: 'root' })
export class LicenseService {
  private readonly api = inject(ApiService);

  list(query: ListLicensesQuery = {}): Promise<PagedResult<LicenseListItem>> {
    return this.api.get<PagedResult<LicenseListItem>>('/licenses', {
      params: toQueryParams({ ...query }),
    });
  }

  get(id: string): Promise<LicenseDetail> {
    return this.api.get<LicenseDetail>(`/licenses/${id}`);
  }

  create(command: CreateLicenseCommand): Promise<{ id: string }> {
    return this.api.post<{ id: string }>('/licenses', command);
  }

  /**
   * Corrige o cadastro. Não altera valor, quantidade nem situação — para isso
   * existem o reajuste, a mudança de quantidade e o cancelamento.
   */
  update(id: string, command: UpdateLicenseCommand): Promise<void> {
    return this.api.put<void>(`/licenses/${id}`, command);
  }

  /**
   * Cancela a licença preservando o registro e o histórico. É o caminho
   * correto quando a licença de fato existiu — e o que o 409 da exclusão
   * orienta a fazer.
   */
  cancel(id: string, reason: string): Promise<void> {
    return this.api.post<void>(`/licenses/${id}/cancel`, { reason });
  }

  /**
   * Remove em definitivo. O servidor recusa com 409 quando já existe histórico
   * financeiro; nesse caso o caminho é cancelar.
   */
  delete(id: string): Promise<void> {
    return this.api.delete<void>(`/licenses/${id}`);
  }

  applyPriceAdjustment(
    id: string,
    command: ApplyPriceAdjustmentCommand,
  ): Promise<PriceAdjustmentResult> {
    return this.api.post<PriceAdjustmentResult>(`/licenses/${id}/price-adjustments`, command);
  }

  changeQuantity(id: string, command: ChangeLicenseQuantityCommand): Promise<QuantityChangeResult> {
    return this.api.post<QuantityChangeResult>(`/licenses/${id}/quantity-changes`, command);
  }

  updateUsage(id: string, usedQuantity: number): Promise<void> {
    return this.api.put<void>(`/licenses/${id}/usage`, { usedQuantity });
  }

  /** Calcula o impacto de um reajuste sem persistir nada. */
  simulate(currentAmount: number, newAmount: number, currency = 'BRL'): Promise<SimulationResult> {
    return this.api.post<SimulationResult>('/licenses/simulate-adjustment', {
      currentAmount,
      newAmount,
      currency,
    });
  }
}

@Injectable({ providedIn: 'root' })
export class ServerService {
  private readonly api = inject(ApiService);

  list(query: ListServersQuery = {}): Promise<PagedResult<ServerListItem>> {
    return this.api.get<PagedResult<ServerListItem>>('/servers', {
      params: toQueryParams({ ...query }),
    });
  }

  get(id: string): Promise<ServerDetail> {
    return this.api.get<ServerDetail>(`/servers/${id}`);
  }

  create(command: CreateServerCommand): Promise<{ id: string }> {
    return this.api.post<{ id: string }>('/servers', command);
  }

  /** Corrige o cadastro. Custos e recursos não mudam aqui. */
  update(id: string, command: UpdateServerCommand): Promise<void> {
    return this.api.put<void>(`/servers/${id}`, command);
  }

  /**
   * Remove em definitivo. Para registrar que uma máquina real saiu de operação
   * use a desativação, que preserva o histórico.
   */
  delete(id: string): Promise<void> {
    return this.api.delete<void>(`/servers/${id}`);
  }

  updateCosts(id: string, command: UpdateServerCostsCommand): Promise<ServerCostChangeResult> {
    return this.api.put<ServerCostChangeResult>(`/servers/${id}/costs`, command);
  }

  resize(id: string, command: ResizeServerCommand): Promise<void> {
    return this.api.post<void>(`/servers/${id}/resize`, command);
  }

  decommission(id: string, reason: string): Promise<void> {
    return this.api.post<void>(`/servers/${id}/decommission`, { reason });
  }
}
