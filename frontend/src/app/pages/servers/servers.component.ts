import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ServerService } from '@app/features/services/asset.service';
import { OrganizationApiService } from '@app/features/services/business.service';
import { ServerFormComponent } from '@app/features/forms/server-form.component';
import { ReasonFormComponent } from '@app/features/forms/reason-form.component';
import { servers as demoServers } from '@app/features/demo/demo-data';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import type { OrganizationalUnitTree } from '@app/features/models';
import {
  ENVIRONMENT_LABELS,
  SERVER_TYPE_LABELS,
  type PagedResult,
  type ServerEnvironment,
  type ServerListItem,
  type ServerType,
} from '@app/features/models';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '@shared/components/status-badge/status-badge.component';
import { asyncSignal } from '@shared/models/async-state.model';

@Component({
  selector: 'app-servers',
  imports: [
    DatePipe, RouterLink, FormsModule, Button, InputText, TableModule, AppCardComponent,
    PageHeaderComponent, MoneyComponent, StatusBadgeComponent, LoadingComponent,
    ErrorStateComponent, DemoNoticeComponent, ServerFormComponent, ReasonFormComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './servers.component.html',
  styleUrl: './servers.component.scss',
})
export class ServersComponent implements OnInit {
  private readonly api = inject(ServerService);
  private readonly organization = inject(OrganizationApiService);

  protected readonly demoMode = inject(DEMO_MODE);

  protected readonly records = asyncSignal<PagedResult<ServerListItem>>();
  protected readonly search = signal('');
  protected readonly units = signal<readonly OrganizationalUnitTree[]>([]);

  protected readonly dialog = signal<'none' | 'create' | 'decommission'>('none');
  protected readonly selected = signal<ServerListItem | null>(null);

  /**
   * A ação é montada aqui e passada ao modal genérico: assim o formulário de
   * justificativa não precisa conhecer nenhum service.
   */
  protected readonly decommissionAction = (reason: string): Promise<void> =>
    this.api.decommission(this.selected()!.id, reason);

  protected readonly items = computed(() =>
    this.demoMode ? [...demoServers] : [...(this.records.data()?.items ?? [])],
  );

  ngOnInit(): void {
    void this.load();
    void this.loadUnits();
  }

  protected open(dialog: 'create' | 'decommission', item?: ServerListItem): void {
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

  private async loadUnits(): Promise<void> {
    if (this.demoMode) {
      return;
    }

    try {
      this.units.set(await this.organization.tree());
    } catch {
      this.units.set([]);
    }
  }

  protected load(): Promise<PagedResult<ServerListItem> | null> {
    if (this.demoMode) {
      return Promise.resolve(null);
    }

    return this.records.run(() => this.api.list({ search: this.search(), pageSize: 50 }));
  }

  protected typeLabel(value: ServerType): string {
    return SERVER_TYPE_LABELS[value];
  }

  protected environmentLabel(value: ServerEnvironment): string {
    return ENVIRONMENT_LABELS[value];
  }
}
