import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';

import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { SupplierApiService } from '@app/features/services/business.service';
import { DeleteDialogComponent } from '@app/features/forms/delete-dialog.component';
import { SupplierFormComponent } from '@app/features/forms/supplier-form.component';
import { suppliers } from '@app/features/demo/demo-data';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import type { SupplierDetail, SupplierListItem, PagedResult } from '@app/features/models';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '@shared/components/status-badge/status-badge.component';
import { asyncSignal } from '@shared/models/async-state.model';

@Component({
  selector: 'app-suppliers',
  imports: [
    Button, TableModule, AppCardComponent, PageHeaderComponent, MoneyComponent,
    StatusBadgeComponent, LoadingComponent, ErrorStateComponent, DemoNoticeComponent,
    SupplierFormComponent, DeleteDialogComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './suppliers.component.html',
  styleUrl: './suppliers.component.scss',
})
export class SuppliersComponent implements OnInit {
  private readonly api = inject(SupplierApiService);
  protected readonly demoMode = inject(DEMO_MODE);
  protected readonly records = asyncSignal<PagedResult<SupplierListItem>>();

  protected readonly dialogOpen = signal(false);
  protected readonly editing = signal<SupplierDetail | null>(null);

  /** Em demonstração usa dados fictícios; fora dela, só o que a API devolveu. */
  protected readonly items = computed(() =>
    this.demoMode ? [...suppliers] : [...(this.records.data()?.items ?? [])],
  );

  ngOnInit(): void {
    void this.load();
  }

  protected load(): Promise<PagedResult<SupplierListItem> | null> {
    if (this.demoMode) {
      return Promise.resolve(null);
    }

    return this.records.run(() => this.api.list({ pageSize: 50 }));
  }

  protected readonly deleting = signal<SupplierListItem | null>(null);

  protected readonly deleteAction = (): Promise<void> => {
    const id = this.deleting()?.id;

    return id ? this.api.delete(id) : Promise.resolve();
  };

  protected openDelete(item: SupplierListItem): void {
    this.deleting.set(item);
  }

  protected closeDelete(): void {
    this.deleting.set(null);
  }

  protected async afterDelete(): Promise<void> {
    this.closeDelete();
    await this.load();
  }

  /**
   * Alternativa oferecida quando a exclusão é recusada: o fornecedor sai das
   * seleções sem desaparecer dos registros que o referenciam.
   *
   * O detalhe é buscado antes de salvar porque a atualização substitui os
   * campos por inteiro — enviar apenas o status apagaria e-mail, telefone e
   * site já cadastrados.
   */
  protected async deactivate(item: SupplierListItem): Promise<void> {
    try {
      const detail = await this.api.get(item.id);

      await this.api.update(item.id, {
        mainContactName: detail.mainContactName,
        email: detail.email,
        phone: detail.phone,
        website: detail.website,
        category: detail.category,
        slaDescription: detail.slaDescription,
        status: 'Inativo',
      });

      this.closeDelete();
      await this.load();
    } catch {
      // O interceptor já notificou; o diálogo permanece aberto.
    }
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.dialogOpen.set(true);
  }

  /**
   * Carrega o detalhe antes de abrir a edição.
   *
   * A listagem não traz e-mail, telefone e site — e o backend substitui esses
   * campos integralmente. Abrir o formulário sem eles apagaria os dados
   * gravados no primeiro salvamento.
   */
  protected async openEdit(item: SupplierListItem): Promise<void> {
    try {
      this.editing.set(await this.api.get(item.id));
      this.dialogOpen.set(true);
    } catch {
      // O interceptor já notificou a falha; sem o detalhe, não abrimos o modal.
    }
  }

  protected close(): void {
    this.dialogOpen.set(false);
    this.editing.set(null);
  }

  protected async afterSave(): Promise<void> {
    this.close();
    await this.load();
  }
}
