import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { Button } from 'primeng/button';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { TimelineListComponent } from '@shared/components/timeline-list/timeline-list.component';
import { TimelineApiService } from '@app/features/services/business.service';
import { timelineEvents } from '@app/features/demo/demo-data';
import { asyncSignal } from '@shared/models/async-state.model';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import type { TimelineEvent, PagedResult } from '@app/features/models';
@Component({
  selector: 'app-timeline',
  imports: [DemoNoticeComponent, Button, AppCardComponent, PageHeaderComponent, TimelineListComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<app-page-header
      title="timeline.title"
      description="timeline.subtitle"
      icon="pi pi-history"
      ><p-button
        label="Atualizar"
        icon="pi pi-refresh"
        severity="secondary"
        [outlined]="true"
        size="small"
        (onClick)="load()" /></app-page-header
    >@if (demoMode) { <app-demo-notice /> }<app-card title="Eventos recentes" icon="pi pi-history"
      ><app-timeline-list [events]="items()"
    /></app-card>`,
})
export class TimelineComponent implements OnInit {
  protected readonly demoMode = inject(DEMO_MODE);
  private api = inject(TimelineApiService);
  records = asyncSignal<PagedResult<TimelineEvent>>();
  items = computed(() =>
    this.demoMode ? [...timelineEvents] : [...(this.records.data()?.items ?? [])],
  );
  ngOnInit(): void {
    void this.load();
  }
  load(): Promise<PagedResult<TimelineEvent> | null> {
    if (this.demoMode) {
      return Promise.resolve(null);
    }

    return this.records.run(() => this.api.list({ pageSize: 50 }));
  }
}
