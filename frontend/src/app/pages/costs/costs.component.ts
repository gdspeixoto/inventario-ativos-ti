import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { CostEntryFormComponent } from '@app/features/forms/cost-entry-form.component';
import { OrganizationApiService } from '@app/features/services/business.service';
import type { OrganizationalUnitTree } from '@app/features/models';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatCardComponent } from '@shared/components/stat-card/stat-card.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { MoneyComponent } from '@shared/components/money/money.component';

import { CostApiService } from '@app/features/services/business.service';
import { adjustments, costSummary } from '@app/features/demo/demo-data';
import { asyncSignal } from '@shared/models/async-state.model';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import type { PriceAdjustmentListItem, PagedResult } from '@app/features/models';
@Component({
  selector: 'app-costs',
  imports: [DemoNoticeComponent,
    CostEntryFormComponent,
    DatePipe,
    DecimalPipe,
    Button,
    TableModule,
    AppCardComponent,
    PageHeaderComponent,
    StatCardComponent,
    MoneyComponent,
    LoadingComponent,
    ErrorStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './costs.component.html',
  styleUrl: './costs.component.scss',
})
export class CostsComponent implements OnInit {
  protected readonly demoMode = inject(DEMO_MODE);
  private api = inject(CostApiService);
  summary = asyncSignal<typeof costSummary>();
  records = asyncSignal<PagedResult<PriceAdjustmentListItem>>();
  data = computed(() => (this.demoMode ? costSummary : this.summary.data()));
  items = computed(() =>
    this.demoMode ? [...adjustments] : [...(this.records.data()?.items ?? [])],
  );
  private readonly organization = inject(OrganizationApiService);

  protected readonly dialogOpen = signal(false);
  protected readonly units = signal<readonly OrganizationalUnitTree[]>([]);

  ngOnInit(): void {
    void this.load();
  }

  protected openEntry(): void {
    this.dialogOpen.set(true);
  }

  protected closeEntry(): void {
    this.dialogOpen.set(false);
  }

  protected async afterEntry(): Promise<void> {
    this.closeEntry();
    await this.load();
  }
  async load(): Promise<void> {
    if (this.demoMode) {
      return;
    }

    await Promise.all([
      this.summary.run(() => this.api.summary()),
      this.records.run(() => this.api.adjustments({ pageSize: 50 })),
      this.loadUnits(),
    ]);
  }

  /**
   * As unidades alimentam o seletor do lançamento. A falha é engolida de
   * propósito: sem elas o formulário abre com a lista vazia, mas o resto da
   * tela de custos continua utilizável.
   */
  private async loadUnits(): Promise<void> {
    try {
      this.units.set(await this.organization.tree());
    } catch {
      this.units.set([]);
    }
  }
}
