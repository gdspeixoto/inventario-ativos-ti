import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MoneyComponent } from '@shared/components/money/money.component';

@Component({
  selector: 'app-stat-card',
  imports: [DecimalPipe, MoneyComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="stat-card">
      <div class="stat-card__icon"><i class="{{ icon() }}" aria-hidden="true"></i></div>
      <div>
        <p class="stat-card__label">{{ label() }}</p>
        @if (money()) {
          <app-money [amount]="value()" [currency]="currency()" />
        } @else {
          <strong>{{ value() | number: '1.0-2' : 'pt-BR' }}</strong>
        }
        @if (hint()) {
          <small>{{ hint() }}</small>
        }
      </div>
    </article>
  `,
  styles: `
    .stat-card {
      display: flex;
      gap: 1rem;
      align-items: center;
      padding: 1rem;
      border: 1px solid var(--app-color-border);
      border-radius: var(--app-radius-lg);
      background: var(--app-color-surface);
    }
    .stat-card__icon {
      width: 2.75rem;
      height: 2.75rem;
      display: grid;
      place-items: center;
      border-radius: 999px;
      background: var(--app-color-primary-subtle);
      color: var(--app-color-primary);
      font-size: 1.25rem;
    }
    .stat-card__label {
      margin: 0;
      color: var(--app-color-text-secondary);
      font-size: 0.875rem;
    }
    strong {
      display: block;
      margin-top: 0.1rem;
      font-size: 1.35rem;
    }
    small {
      display: block;
      margin-top: 0.2rem;
      color: var(--app-color-text-muted);
    }
  `,
})
export class StatCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number>();
  readonly icon = input('pi pi-chart-line');
  readonly hint = input<string | null>(null);
  readonly money = input(false);
  readonly currency = input('BRL');
}
