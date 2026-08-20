import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { ReportApiService } from '@app/features/services/business.service';
import {
  costEvolution,
  licenseUtilizationReport,
  contractExpirationReport,
} from '@app/features/demo/demo-data';
import { asyncSignal } from '@shared/models/async-state.model';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import type { ContractExpirationReport, CostEvolutionReport, LicenseUtilizationReport } from '@app/features/models';
@Component({
  selector: 'app-reports',
  imports: [DemoNoticeComponent, 
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    Button,
    TableModule,
    AppCardComponent,
    PageHeaderComponent,
    MoneyComponent,
    LoadingComponent,
    ErrorStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss',
})
export class ReportsComponent implements OnInit {
  protected readonly demoMode = inject(DEMO_MODE);
  private readonly api = inject(ReportApiService);
  readonly evolution = asyncSignal<CostEvolutionReport>();
  readonly licenses = asyncSignal<readonly LicenseUtilizationReport[]>();
  readonly contracts = asyncSignal<readonly ContractExpirationReport[]>();
  readonly evo = computed(() => (this.demoMode ? costEvolution : this.evolution.data()));
  readonly lic = computed(() =>
    this.demoMode ? [...licenseUtilizationReport] : [...(this.licenses.data() ?? [])],
  );
  readonly ct = computed(() =>
    this.demoMode ? [...contractExpirationReport] : [...(this.contracts.data() ?? [])],
  );
  ngOnInit(): void {
    void this.load();
  }
  async load(): Promise<void> {
    if (this.demoMode) {
      return;
    }

    await Promise.all([
      this.evolution.run(() => this.api.costEvolution()),
      this.licenses.run(() => this.api.licenseUtilization(70)),
      this.contracts.run(() => this.api.contractExpirations(90)),
    ]);
  }
}
