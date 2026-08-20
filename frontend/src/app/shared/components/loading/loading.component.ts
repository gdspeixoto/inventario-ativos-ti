import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ProgressSpinner } from 'primeng/progressspinner';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * Loading indicator for a region of the page.
 *
 * Announced through `role="status"` with `aria-live="polite"`, so screen-reader
 * users learn that content is on its way instead of hearing silence.
 */
@Component({
  selector: 'app-loading',
  imports: [ProgressSpinner, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="app-loading"
      [class.app-loading--overlay]="overlay()"
      role="status"
      aria-live="polite"
    >
      <p-progress-spinner
        [style]="{ width: spinnerSize(), height: spinnerSize() }"
        strokeWidth="4"
        animationDuration="1s"
        ariaLabel="{{ 'common.loading' | translate }}"
      />
      @if (message()) {
        <p class="app-loading__message">{{ message()! | translate }}</p>
      }
    </div>
  `,
  styles: `
    .app-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: var(--app-spacing-sm);
      padding: var(--app-spacing-2xl) var(--app-spacing-md);
    }

    .app-loading--overlay {
      position: absolute;
      inset: 0;
      z-index: var(--app-z-overlay);
      background-color: color-mix(in srgb, var(--app-color-surface) 78%, transparent);
      backdrop-filter: blur(1px);
    }

    .app-loading__message {
      color: var(--app-color-text-secondary);
      font-size: var(--app-font-size-sm);
    }
  `,
})
export class LoadingComponent {
  /** i18n key shown under the spinner. */
  readonly message = input<string | null>(null);
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  /** Covers the nearest positioned ancestor instead of taking up layout space. */
  readonly overlay = input(false);

  spinnerSize(): string {
    return { sm: '1.5rem', md: '2.5rem', lg: '3.5rem' }[this.size()];
  }
}
