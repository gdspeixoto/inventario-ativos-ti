import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Button } from 'primeng/button';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { isAppHttpError } from '@core/errors/http-error.model';
import type { AsyncError } from '@shared/models/async-state.model';

/**
 * Inline failure state with a retry affordance.
 *
 * Accepts the raw error so a page can pass whatever it caught: an
 * `AppHttpError` contributes its status and trace id, anything else falls back
 * to the generic copy. The trace id is shown because it is the first thing
 * support will ask for.
 */
@Component({
  selector: 'app-error-state',
  imports: [Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="app-error-state" role="alert">
      <i class="app-error-state__icon pi pi-exclamation-triangle" aria-hidden="true"></i>
      <h3 class="app-error-state__title">{{ title() | translate }}</h3>
      <p class="app-error-state__message">{{ resolvedMessage() | translate }}</p>

      @if (traceId(); as trace) {
        <p class="app-error-state__trace">
          <span class="text-overline">trace</span>
          <code>{{ trace }}</code>
        </p>
      }

      @if (showRetry()) {
        <p-button
          [label]="'common.retry' | translate"
          icon="pi pi-refresh"
          severity="secondary"
          size="small"
          [outlined]="true"
          (onClick)="retry.emit()"
        />
      }
    </div>
  `,
  styles: `
    .app-error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--app-spacing-sm);
      padding: var(--app-spacing-2xl) var(--app-spacing-lg);
      text-align: center;
    }

    .app-error-state__icon {
      display: grid;
      place-items: center;
      width: 3.5rem;
      height: 3.5rem;
      border-radius: var(--app-radius-pill);
      background-color: var(--app-color-danger-subtle);
      color: var(--app-color-danger);
      font-size: 1.5rem;
    }

    .app-error-state__title {
      font-size: var(--app-font-size-lg);
    }

    .app-error-state__message {
      max-width: 36rem;
      color: var(--app-color-text-secondary);
      font-size: var(--app-font-size-sm);
    }

    .app-error-state__trace {
      display: flex;
      align-items: center;
      gap: var(--app-spacing-2xs);
      font-size: var(--app-font-size-xs);
    }
  `,
})
export class ErrorStateComponent {
  readonly title = input('states.errorTitle');
  /** Overrides the message derived from `error`. */
  readonly message = input<string | null>(null);
  readonly error = input<AsyncError | null>(null);
  readonly showRetry = input(true);

  readonly retry = output<void>();

  readonly resolvedMessage = computed(() => {
    const explicit = this.message();
    if (explicit) {
      return explicit;
    }
    const error = this.error();
    if (isAppHttpError(error)) {
      return error.translationKey;
    }
    return 'states.errorMessage';
  });

  readonly traceId = computed(() => {
    const error = this.error();
    return isAppHttpError(error) ? error.traceId : null;
  });
}
