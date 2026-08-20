import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Tag } from 'primeng/tag';
import {
  STATUS_LABELS,
  prioritySeverity,
  statusSeverity,
  type AlertPriority,
} from '@app/features/models';

@Component({
  selector: 'app-status-badge',
  imports: [Tag],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<p-tag [value]="label()" [severity]="severity()" [rounded]="true" />`,
})
export class StatusBadgeComponent {
  readonly value = input.required<string>();
  readonly priority = input<AlertPriority | null>(null);

  readonly label = computed(() => STATUS_LABELS[this.value()] ?? this.value());
  readonly severity = computed(() =>
    this.priority() ? prioritySeverity(this.priority()!) : statusSeverity(this.value()),
  );
}
