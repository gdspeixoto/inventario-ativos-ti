import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TreeTableModule } from 'primeng/treetable';
import { Button } from 'primeng/button';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { UnitTreeNodeComponent } from '@app/features/organization/unit-tree-node.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { StatusBadgeComponent } from '@shared/components/status-badge/status-badge.component';
import { OrganizationApiService } from '@app/features/services/business.service';
import { assignments, orgTree } from '@app/features/demo/demo-data';
import { MANAGEMENT_ROLE_LABELS, UNIT_TYPE_LABELS, type ManagementAssignment, type ManagementRole, type OrganizationalUnitTree, type OrganizationalUnitType } from '@app/features/models';
import { asyncSignal } from '@shared/models/async-state.model';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import {
  AssignmentFormComponent,
  FinishAssignmentFormComponent,
  UnitFormComponent,
} from '@app/features/forms/organization-forms.component';
@Component({
  selector: 'app-organization',
  imports: [DemoNoticeComponent,
    UnitTreeNodeComponent,
    TableModule,
    TreeTableModule,
    Button,
    AppCardComponent,
    PageHeaderComponent,
    MoneyComponent,
    StatusBadgeComponent,
    UnitFormComponent,
    AssignmentFormComponent,
    FinishAssignmentFormComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './organization.component.html',
  styleUrl: './organization.component.scss',
})
export class OrganizationComponent implements OnInit {
  protected readonly demoMode = inject(DEMO_MODE);
  private readonly api = inject(OrganizationApiService);
  readonly tree = asyncSignal<readonly OrganizationalUnitTree[]>();
  readonly assign = asyncSignal<readonly ManagementAssignment[]>();
  readonly nodes = computed(() =>
    this.demoMode ? [...orgTree] : [...(this.tree.data() ?? [])],
  );
  readonly assignments = computed(() =>
    this.demoMode ? [...assignments] : [...(this.assign.data() ?? [])],
  );

  protected readonly dialog = signal<'none' | 'unit' | 'assignment' | 'finish'>('none');
  protected readonly selectedAssignment = signal<ManagementAssignment | null>(null);

  protected open(dialog: 'unit' | 'assignment' | 'finish', item?: ManagementAssignment): void {
    this.selectedAssignment.set(item ?? null);
    this.dialog.set(dialog);
  }

  protected close(): void {
    this.dialog.set('none');
  }

  protected async afterSave(): Promise<void> {
    this.close();
    await this.load();
  }
  typeLabel(value: OrganizationalUnitType): string {
    return UNIT_TYPE_LABELS[value];
  }
  roleLabel(value: ManagementRole): string {
    return MANAGEMENT_ROLE_LABELS[value];
  }
  ngOnInit(): void {
    void this.load();
  }
  async load(): Promise<void> {
    if (this.demoMode) {
      return;
    }

    await Promise.all([
      this.tree.run(() => this.api.tree()),
      this.assign.run(() => this.api.assignments()),
    ]);
  }
}
