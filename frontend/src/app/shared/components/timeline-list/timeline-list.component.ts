import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Tag } from 'primeng/tag';
import { EVENT_TYPE_LABELS, type TimelineEvent } from '@app/features/models';
import { MoneyComponent } from '@shared/components/money/money.component';

@Component({
  selector: 'app-timeline-list',
  imports: [DatePipe, Tag, MoneyComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ol class="timeline">
      @for (event of events(); track event.id) {
        <li>
          <span class="dot"><i class="pi pi-history" aria-hidden="true"></i></span>
          <div class="content">
            <div class="top">
              <strong>{{ event.title }}</strong>
              <p-tag [value]="label(event.eventType)" severity="info" [rounded]="true" />
            </div>
            <p>{{ event.description }}</p>
            <small
              >{{ event.occurredAt | date: 'dd/MM/yyyy HH:mm' }} ·
              {{ event.userDisplayName || 'Sistema' }} ·
              {{ event.organizationalUnitName || 'Sem unidade' }}</small
            >
            @if (event.financialImpact !== null) {
              <div class="impact">
                Impacto mensal: <app-money [amount]="event.financialImpact" />
              </div>
            }
          </div>
        </li>
      } @empty {
        <li class="empty">Nenhum evento registrado.</li>
      }
    </ol>
  `,
  styles: `
    .timeline {
      list-style: none;
      margin: 0;
      padding: 0;
      display: grid;
      gap: 1rem;
    }
    li {
      display: grid;
      grid-template-columns: auto 1fr;
      gap: 0.8rem;
    }
    .dot {
      width: 2rem;
      height: 2rem;
      border-radius: 999px;
      display: grid;
      place-items: center;
      background: var(--app-color-primary-subtle);
      color: var(--app-color-primary);
    }
    .content {
      border: 1px solid var(--app-color-border);
      border-radius: var(--app-radius-md);
      padding: 0.85rem;
      background: var(--app-color-surface);
    }
    .top {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: center;
    }
    p {
      margin: 0.4rem 0;
      color: var(--app-color-text-secondary);
    }
    small {
      color: var(--app-color-text-muted);
    }
    .impact {
      margin-top: 0.5rem;
      font-size: 0.9rem;
    }
    .empty {
      display: block;
      color: var(--app-color-text-muted);
    }
  `,
})
export class TimelineListComponent {
  readonly events = input.required<readonly TimelineEvent[]>();
  label(value: keyof typeof EVENT_TYPE_LABELS): string {
    return EVENT_TYPE_LABELS[value] ?? value;
  }
}
