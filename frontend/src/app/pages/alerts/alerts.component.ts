import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '@shared/components/status-badge/status-badge.component';
import { AlertApiService } from '@app/features/services/business.service';
import { alerts } from '@app/features/demo/demo-data';
import {
  ALERT_TYPE_LABELS,
  type Alert,
  type AlertType,
  type PagedResult,
} from '@app/features/models';
import { asyncSignal } from '@shared/models/async-state.model';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import { ReasonFormComponent } from '@app/features/forms/reason-form.component';

@Component({
  selector: 'app-alerts',
  imports: [DemoNoticeComponent, 
    DatePipe,
    Button,
    TableModule,
    AppCardComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    ReasonFormComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './alerts.component.html',
})
export class AlertsComponent implements OnInit {
  protected readonly demoMode = inject(DEMO_MODE);
  private readonly api = inject(AlertApiService);
  protected readonly records = asyncSignal<PagedResult<Alert>>();

  protected readonly dialog = signal<'none' | 'resolve' | 'ignore'>('none');
  protected readonly selected = signal<Alert | null>(null);

  /*
   * As ações são montadas aqui e entregues prontas ao modal genérico. Ele fica
   * responsável só pelo formulário; quem conhece a API é esta página.
   */
  protected readonly resolveAction = (notes: string): Promise<void> =>
    this.api.resolve(this.selected()!.id, { notes });

  protected readonly ignoreAction = (justification: string): Promise<void> =>
    this.api.ignore(this.selected()!.id, { justification });

  /** Demonstração usa dados fictícios; fora dela, apenas o retorno da API. */
  protected readonly items = computed(() =>
    this.demoMode ? [...alerts] : [...(this.records.data()?.items ?? [])],
  );

  ngOnInit(): void {
    void this.load();
  }

  protected open(dialog: 'resolve' | 'ignore', item: Alert): void {
    this.selected.set(item);
    this.dialog.set(dialog);
  }

  protected close(): void {
    this.dialog.set('none');
  }

  protected async afterSave(): Promise<void> {
    this.close();
    await this.load();
  }

  protected load(): Promise<PagedResult<Alert> | null> {
    if (this.demoMode) {
      return Promise.resolve(null);
    }

    return this.records.run(() => this.api.list({ onlyOpen: true, pageSize: 50 }));
  }

  protected scan(): void {
    void this.api.scan().then(() => this.load());
  }

  protected typeLabel(value: AlertType): string {
    return ALERT_TYPE_LABELS[value];
  }
}
