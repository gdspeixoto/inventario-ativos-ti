import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Menu } from 'primeng/menu';
import type { MenuItem } from 'primeng/api';
import { LicenseService } from '@app/features/services/asset.service';
import { TimelineApiService } from '@app/features/services/business.service';
import { PriceAdjustmentFormComponent } from '@app/features/forms/price-adjustment-form.component';
import { QuantityFormComponent } from '@app/features/forms/asset-action-forms.component';
import { UsageFormComponent } from '@app/features/forms/organization-forms.component';
import { LicenseEditFormComponent } from '@app/features/forms/license-edit-form.component';
import { ReasonFormComponent } from '@app/features/forms/reason-form.component';
import { DeleteDialogComponent } from '@app/features/forms/delete-dialog.component';
import { DocumentsPanelComponent } from '@app/features/documents/documents-panel.component';
import { licenses, timelineEvents } from '@app/features/demo/demo-data';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import type { LicenseDetail, PagedResult, TimelineEvent } from '@app/features/models';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatCardComponent } from '@shared/components/stat-card/stat-card.component';
import { StatusBadgeComponent } from '@shared/components/status-badge/status-badge.component';
import { TimelineListComponent } from '@shared/components/timeline-list/timeline-list.component';
import { asyncSignal } from '@shared/models/async-state.model';

@Component({
  selector: 'app-license-detail',
  imports: [
    RouterLink, Button, AppCardComponent, PageHeaderComponent, StatCardComponent,
    StatusBadgeComponent, TimelineListComponent, DemoNoticeComponent, LoadingComponent,
    ErrorStateComponent, PriceAdjustmentFormComponent, QuantityFormComponent, UsageFormComponent,
    Menu, LicenseEditFormComponent, ReasonFormComponent, DeleteDialogComponent,
    DocumentsPanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './license-detail.component.html',
  styleUrl: './license-detail.component.scss',
})
export class LicenseDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(LicenseService);
  protected readonly demoMode = inject(DEMO_MODE);

  private readonly timelineApi = inject(TimelineApiService);

  protected readonly detail = asyncSignal<LicenseDetail>();
  protected readonly timeline = asyncSignal<PagedResult<TimelineEvent>>();

  private readonly router = inject(Router);

  protected readonly dialog = signal<
    'none' | 'adjustment' | 'quantity' | 'usage' | 'edit' | 'cancel' | 'delete'
  >('none');

  /**
   * Ações sobre o registro, separadas das ações de negócio do cabeçalho.
   *
   * Cancelar aparece antes de Excluir porque é o caminho certo na maioria dos
   * casos: a licença existiu, foi paga, e o histórico precisa continuar de pé.
   */
  protected readonly recordActions: MenuItem[] = [
    {
      label: 'Editar dados cadastrais',
      icon: 'pi pi-pencil',
      command: () => this.open('edit'),
    },
    {
      label: 'Cancelar licença',
      icon: 'pi pi-ban',
      command: () => this.open('cancel'),
    },
    {
      separator: true,
    },
    {
      label: 'Excluir definitivamente',
      icon: 'pi pi-trash',
      styleClass: 'menu-item--danger',
      command: () => this.open('delete'),
    },
  ];

  /**
   * Entregue pronta ao diálogo de exclusão, que não conhece a API. A referência
   * é estável para não recriar a função a cada verificação do template.
   */
  protected readonly deleteAction = (): Promise<void> => {
    const id = this.detail.data()?.id;

    return id ? this.api.delete(id) : Promise.resolve();
  };

  protected readonly cancelAction = (reason: string): Promise<void> => {
    const id = this.detail.data()?.id;

    return id ? this.api.cancel(id, reason) : Promise.resolve();
  };

  /**
   * Em demonstração, monta o detalhe a partir da lista fictícia. Fora dela,
   * exibe apenas o que a API devolveu — sem completar campos ausentes com
   * valores inventados, que dariam ao usuário a impressão de dado cadastrado.
   */
  protected readonly data = computed<LicenseDetail | null>(() => {
    if (!this.demoMode) {
      return this.detail.data();
    }

    const id = this.route.snapshot.paramMap.get('id');
    const item = licenses.find((license) => license.id === id) ?? licenses[0];

    return {
      ...item,
      description: 'Licença corporativa gerenciada pelo setor de TI.',
      organizationalUnitId: 'uo-1',
      organizationalUnitPath: '/CONTOSO/NEG-TEC/AT-DIGITAL/',
      supplierId: 'sup-1',
      contractId: 'ct-1',
      contractNumber: 'CT-2025-001',
      costCenterId: 'cc-1',
      costCenterName: 'Tecnologia da Informação',
      technicalResponsibleUserId: 'usr-1',
      technicalResponsibleName: 'Ana Souza',
      financialResponsibleUserId: 'usr-4',
      financialResponsibleName: 'Paulo Dias',
      startDate: null,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };
  });

  /**
   * Histórico do item.
   *
   * Fora da demonstração vem da API filtrada por esta licença — é o registro
   * que explica cada mudança de valor e quantidade, e o motivo de as ações
   * exigirem justificativa.
   */
  protected readonly events = computed(() =>
    this.demoMode
      ? timelineEvents.filter((event) => event.entityType === 'License')
      : (this.timeline.data()?.items ?? []),
  );

  ngOnInit(): void {
    void this.load();
    void this.loadTimeline();
  }

  protected open(
    dialog: 'adjustment' | 'quantity' | 'usage' | 'edit' | 'cancel' | 'delete',
  ): void {
    this.dialog.set(dialog);
  }

  protected close(): void {
    this.dialog.set('none');
  }

  /**
   * Excluída a licença, não há detalhe para recarregar: a rota volta para a
   * lista. Permanecer na página renderizaria um 404 logo após uma operação
   * bem-sucedida.
   */
  protected async afterDelete(): Promise<void> {
    this.close();
    await this.router.navigate(['/licenses']);
  }

  /** Após escrever, detalhe e timeline precisam refletir o novo estado. */
  protected async afterSave(): Promise<void> {
    this.close();
    await Promise.all([this.load(), this.loadTimeline()]);
  }

  protected loadTimeline(): Promise<PagedResult<TimelineEvent> | null> {
    const id = this.route.snapshot.paramMap.get('id');

    if (this.demoMode || !id) {
      return Promise.resolve(null);
    }

    return this.timeline.run(() =>
      this.timelineApi.list({ entityType: 'License', entityId: id, pageSize: 30 }),
    );
  }

  protected load(): Promise<LicenseDetail | null> {
    const id = this.route.snapshot.paramMap.get('id');

    if (this.demoMode || !id) {
      return Promise.resolve(null);
    }

    return this.detail.run(() => this.api.get(id));
  }
}
