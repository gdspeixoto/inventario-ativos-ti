import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatCardComponent } from '@shared/components/stat-card/stat-card.component';
import { TimelineListComponent } from '@shared/components/timeline-list/timeline-list.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { asyncSignal } from '@shared/models/async-state.model';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import { DashboardApiService } from '@app/features/services/business.service';
import { dashboardSummary } from '@app/features/demo/demo-data';

@Component({
  selector: 'app-dashboard',
  imports: [DemoNoticeComponent, 
    Button,
    TableModule,
    AppCardComponent,
    PageHeaderComponent,
    StatCardComponent,
    TimelineListComponent,
    MoneyComponent,
    LoadingComponent,
    ErrorStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  protected readonly demoMode = inject(DEMO_MODE);
  private readonly api = inject(DashboardApiService);
  readonly summary = asyncSignal<typeof dashboardSummary>();
  readonly data = computed(() => (this.demoMode ? dashboardSummary : this.summary.data()));
  readonly topCosts = computed(() => [...(this.data()?.topCosts ?? [])]);

  ngOnInit(): void {
    void this.load();
  }
  load(): Promise<typeof dashboardSummary | null> {
    if (this.demoMode) {
      return Promise.resolve(null);
    }

    return this.summary.run(() => this.api.summary());
  }
}
