import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * Surface container with an optional header and footer.
 *
 * A deliberately thinner alternative to `p-card`: no elevation by default, a
 * proper `<section>`/`<h2>` structure for the document outline, and named slots
 * for header actions. Reach for `p-card` only when a PrimeNG feature is needed.
 */
@Component({
  selector: 'app-card',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="app-card" [class.app-card--flush]="flush()" [class.app-card--raised]="raised()">
      @if (title() || hasHeaderActions()) {
        <header class="app-card__header">
          <div>
            @if (title()) {
              <h2 class="app-card__title">
                @if (icon()) {
                  <i class="{{ icon() }}" aria-hidden="true"></i>
                }
                {{ title()! | translate }}
              </h2>
            }
            @if (subtitle()) {
              <p class="app-card__subtitle">{{ subtitle()! | translate }}</p>
            }
          </div>
          <div class="app-card__actions">
            <ng-content select="[card-actions]" />
          </div>
        </header>
      }

      <div class="app-card__body">
        <ng-content />
      </div>

      <ng-content select="[card-footer]" />
    </section>
  `,
  styles: `
    .app-card {
      display: flex;
      flex-direction: column;
      overflow: hidden;
      border: 1px solid var(--app-color-border);
      border-radius: var(--app-radius-lg);
      background-color: var(--app-color-card);
    }

    .app-card--raised {
      box-shadow: var(--app-shadow-sm);
    }

    .app-card__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: var(--app-spacing-md);
      padding: var(--app-spacing-md) var(--app-spacing-lg);
      border-bottom: 1px solid var(--app-color-border);
    }

    .app-card__title {
      display: flex;
      align-items: center;
      gap: var(--app-spacing-xs);
      font-size: var(--app-font-size-md);
      font-weight: var(--app-font-weight-semibold);
    }

    .app-card__title i {
      color: var(--app-color-text-muted);
      font-size: 0.9rem;
    }

    .app-card__subtitle {
      margin-top: 0.125rem;
      color: var(--app-color-text-secondary);
      font-size: var(--app-font-size-xs);
    }

    .app-card__actions {
      display: flex;
      align-items: center;
      gap: var(--app-spacing-2xs);
    }

    .app-card__body {
      padding: var(--app-spacing-lg);
    }

    .app-card--flush .app-card__body {
      padding: 0;
    }
  `,
})
export class AppCardComponent {
  /** i18n key. Omit to render a card with no header. */
  readonly title = input<string | null>(null);
  readonly subtitle = input<string | null>(null);
  readonly icon = input<string | null>(null);
  /** Removes the body padding — use for tables that own their spacing. */
  readonly flush = input(false);
  readonly raised = input(false);
  /** Renders the header even without a title, to host `[card-actions]`. */
  readonly hasHeaderActions = input(false);
}
