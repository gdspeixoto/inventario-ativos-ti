import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import {
  ContractApiService,
  OrganizationApiService,
  SupplierApiService,
} from '@app/features/services/business.service';
import { ContractFormComponent } from '@app/features/forms/contract-form.component';
import { PriceAdjustmentFormComponent } from '@app/features/forms/price-adjustment-form.component';
import {
  RenewContractFormComponent,
  TerminateContractFormComponent,
} from '@app/features/forms/asset-action-forms.component';
import { contracts } from '@app/features/demo/demo-data';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import type {
  ContractListItem,
  OrganizationalUnitTree,
  PagedResult,
  SupplierListItem,
} from '@app/features/models';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '@shared/components/status-badge/status-badge.component';
import { asyncSignal } from '@shared/models/async-state.model';
import { DeleteDialogComponent } from '@app/features/forms/delete-dialog.component';

@Component({
  selector: 'app-contracts',
  imports: [
    DatePipe, Button, TableModule, AppCardComponent, PageHeaderComponent, MoneyComponent,
    StatusBadgeComponent, LoadingComponent, ErrorStateComponent, DemoNoticeComponent,
    ContractFormComponent, PriceAdjustmentFormComponent, RenewContractFormComponent,
    TerminateContractFormComponent, DeleteDialogComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './contracts.component.html',
  styleUrl: './contracts.component.scss',
})
export class ContractsComponent implements OnInit {
  private readonly api = inject(ContractApiService);
  private readonly organization = inject(OrganizationApiService);
  private readonly suppliersApi = inject(SupplierApiService);

  protected readonly demoMode = inject(DEMO_MODE);
  protected readonly records = asyncSignal<PagedResult<ContractListItem>>();

  protected readonly units = signal<readonly OrganizationalUnitTree[]>([]);
  protected readonly suppliers = signal<readonly SupplierListItem[]>([]);

  protected readonly deleteAction = (): Promise<void> => {
    const id = this.selected()?.id;

    return id ? this.api.delete(id) : Promise.resolve();
  };

  protected readonly dialog = signal<
    'none' | 'create' | 'renew' | 'terminate' | 'adjustment' | 'delete'
  >(
    'none',
  );
  protected readonly selected = signal<ContractListItem | null>(null);

  /** Em demonstração usa dados fictícios; fora dela, só o que a API devolveu. */
  protected readonly items = computed(() =>
    this.demoMode ? [...contracts] : [...(this.records.data()?.items ?? [])],
  );

  ngOnInit(): void {
    void this.load();
    void this.loadReferences();
  }

  protected open(
    dialog: 'create' | 'renew' | 'terminate' | 'adjustment' | 'delete',
    item?: ContractListItem,
  ): void {
    this.selected.set(item ?? null);
    this.dialog.set(dialog);
  }

  protected close(): void {
    this.dialog.set('none');
  }

  protected async afterSave(): Promise<void> {
    this.close();
    await this.load();
  }

  /** Fornecedores e unidades alimentam apenas o cadastro; falha não trava a lista. */
  private async loadReferences(): Promise<void> {
    if (this.demoMode) {
      return;
    }

    try {
      const [units, suppliers] = await Promise.all([
        this.organization.tree(),
        this.suppliersApi.list({ pageSize: 100 }),
      ]);

      this.units.set(units);
      this.suppliers.set(suppliers.items);
    } catch {
      this.units.set([]);
      this.suppliers.set([]);
    }
  }

  protected load(): Promise<PagedResult<ContractListItem> | null> {
    if (this.demoMode) {
      return Promise.resolve(null);
    }

    return this.records.run(() => this.api.list({ pageSize: 50 }));
  }
}
