import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Menu } from 'primeng/menu';
import type { MenuItem } from 'primeng/api';
import { ServerService } from '@app/features/services/asset.service';
import { TimelineApiService } from '@app/features/services/business.service';
import {
  ResizeFormComponent,
  ServerCostsFormComponent,
} from '@app/features/forms/asset-action-forms.component';
import { ReasonFormComponent } from '@app/features/forms/reason-form.component';
import { ServerEditFormComponent } from '@app/features/forms/server-edit-form.component';
import { DeleteDialogComponent } from '@app/features/forms/delete-dialog.component';
import { DocumentsPanelComponent } from '@app/features/documents/documents-panel.component';
import { servers, timelineEvents } from '@app/features/demo/demo-data';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import {
  ENVIRONMENT_LABELS,
  SERVER_TYPE_LABELS,
  type PagedResult,
  type ServerDetail,
  type ServerEnvironment,
  type ServerType,
  type TimelineEvent,
} from '@app/features/models';
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
  selector: 'app-server-detail',
  imports: [
    RouterLink, Button, AppCardComponent, PageHeaderComponent, StatCardComponent,
    StatusBadgeComponent, TimelineListComponent, DemoNoticeComponent, LoadingComponent,
    ErrorStateComponent, ServerCostsFormComponent, ResizeFormComponent, ReasonFormComponent,
    Menu, ServerEditFormComponent, DeleteDialogComponent, DocumentsPanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './server-detail.component.html',
  styleUrl: './server-detail.component.scss',
})
export class ServerDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ServerService);
  protected readonly demoMode = inject(DEMO_MODE);

  private readonly timelineApi = inject(TimelineApiService);

  protected readonly detail = asyncSignal<ServerDetail>();
  protected readonly timeline = asyncSignal<PagedResult<TimelineEvent>>();

  protected readonly data = computed<ServerDetail | null>(() => {
    if (!this.demoMode) {
      return this.detail.data();
    }

    const id = this.route.snapshot.paramMap.get('id');
    const item = servers.find((server) => server.id === id) ?? servers[0];

    return {
      ...item,
      description: 'Servidor gerenciado pelo núcleo de infraestrutura.',
      operatingSystemVersion: null,
      regionOrDatacenter: 'Brazil South',
      primaryIp: '10.0.0.10',
      backupPolicy: item.lastBackupAt ? 'Diário às 02:00' : null,
      sla: '99,5% de disponibilidade',
      maintenanceWindow: 'Domingos, 02:00 às 06:00',
      infrastructureMonthlyCost: item.monthlyAmount,
      licenseMonthlyCost: 0,
      supportMonthlyCost: 0,
      backupMonthlyCost: 0,
      organizationalUnitId: 'uo-2',
      organizationalUnitPath: '/CONTOSO/NEG-TEC/AT-DIGITAL/NS-INFRA/',
      supplierId: 'sup-2',
      supplierName: item.provider,
      contractId: 'ct-2',
      contractNumber: 'CT-2025-002',
      costCenterId: 'cc-2',
      costCenterName: 'Infraestrutura e Cloud',
      technicalResponsibleUserId: item.hasResponsible ? 'usr-2' : null,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };
  });

  protected readonly events = computed(() =>
    this.demoMode
      ? timelineEvents.filter((event) => event.entityType === 'Server')
      : (this.timeline.data()?.items ?? []),
  );

  private readonly router = inject(Router);

  protected readonly dialog = signal<
    'none' | 'costs' | 'resize' | 'decommission' | 'edit' | 'delete'
  >('none');

  /**
   * Desativar vem antes de excluir porque é quase sempre a ação correta: um
   * servidor que saiu de operação existiu, consumiu orçamento, e isso precisa
   * continuar visível nos relatórios.
   */
  protected readonly recordActions: MenuItem[] = [
    {
      label: 'Editar dados cadastrais',
      icon: 'pi pi-pencil',
      command: () => this.open('edit'),
    },
    {
      label: 'Desativar servidor',
      icon: 'pi pi-power-off',
      command: () => this.open('decommission'),
    },
    {
      separator: true,
    },
    {
      label: 'Excluir definitivamente',
      icon: 'pi pi-trash',
      command: () => this.open('delete'),
    },
  ];

  protected readonly deleteAction = (): Promise<void> => {
    const id = this.detail.data()?.id;

    return id ? this.api.delete(id) : Promise.resolve();
  };

  /** Custos atuais, para o formulário abrir preenchido — ele faz substituição total. */
  protected readonly currentCosts = computed(() => {
    const server = this.data();

    return server
      ? {
          infrastructure: server.infrastructureMonthlyCost,
          license: server.licenseMonthlyCost,
          support: server.supportMonthlyCost,
          backup: server.backupMonthlyCost,
        }
      : null;
  });

  protected readonly currentSpecs = computed(() => {
    const server = this.data();

    return server
      ? { cpu: server.cpuCores, memory: server.memoryGb, storage: server.storageGb }
      : null;
  });

  protected readonly decommissionAction = (reason: string): Promise<void> =>
    this.api.decommission(this.data()!.id, reason);

  ngOnInit(): void {
    void this.load();
    void this.loadTimeline();
  }

  /**
   * Excluído o servidor, não há detalhe a recarregar: a rota volta à lista.
   */
  protected async afterDelete(): Promise<void> {
    this.close();
    await this.router.navigate(['/servers']);
  }

  protected open(dialog: 'costs' | 'resize' | 'decommission' | 'edit' | 'delete'): void {
    this.dialog.set(dialog);
  }

  protected close(): void {
    this.dialog.set('none');
  }

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
      this.timelineApi.list({ entityType: 'Server', entityId: id, pageSize: 30 }),
    );
  }

  protected load(): Promise<ServerDetail | null> {
    const id = this.route.snapshot.paramMap.get('id');

    if (this.demoMode || !id) {
      return Promise.resolve(null);
    }

    return this.detail.run(() => this.api.get(id));
  }

  protected typeLabel(value: ServerType): string {
    return SERVER_TYPE_LABELS[value];
  }

  protected environmentLabel(value: ServerEnvironment): string {
    return ENVIRONMENT_LABELS[value];
  }
}
