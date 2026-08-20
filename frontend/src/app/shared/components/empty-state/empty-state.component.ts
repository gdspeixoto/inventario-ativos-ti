import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Button } from 'primeng/button';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * "There is nothing here yet" placeholder.
 *
 * An empty list is a design state, not an absence of one: it explains what the
 * area is for and offers the action that fills it.
 */
@Component({
  selector: 'app-empty-state',
  imports: [Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="app-empty">
      <i class="app-empty__icon {{ icon() }}" aria-hidden="true"></i>
      <h3 class="app-empty__title">{{ title() | translate }}</h3>
      <p class="app-empty__message">{{ message() | translate }}</p>

      @if (actionLabel()) {
        <p-button
          [label]="actionLabel()! | translate"
          [icon]="actionIcon()"
          severity="primary"
          size="small"
          (onClick)="action.emit()"
        />
      }

      <ng-content />
    </div>
  `,
  styles: `
    .app-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--app-spacing-sm);
      padding: var(--app-spacing-2xl) var(--app-spacing-lg);
      text-align: center;
    }

    .app-empty__icon {
      display: grid;
      place-items: center;
      width: 3.5rem;
      height: 3.5rem;
      margin-bottom: var(--app-spacing-2xs);
      border-radius: var(--app-radius-pill);
      background-color: var(--app-color-surface-sunken);
      color: var(--app-color-text-muted);
      font-size: 1.5rem;
    }

    .app-empty__title {
      font-size: var(--app-font-size-lg);
    }

    .app-empty__message {
      max-width: 36rem;
      color: var(--app-color-text-secondary);
      font-size: var(--app-font-size-sm);
    }
  `,
})
export class EmptyStateComponent {
  readonly icon = input('pi pi-inbox');
  readonly title = input('states.emptyTitle');
  readonly message = input('states.emptyMessage');
  readonly actionLabel = input<string | null>(null);
  readonly actionIcon = input('pi pi-plus');

  readonly action = output<void>();
}
