import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { VERSION } from '@angular/core';
import { APP_SETTINGS } from '@core/app-settings';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * Slim footer carrying the version numbers.
 *
 * Trivial-looking, genuinely useful: when a user reports a bug, the first
 * question is always "which version are you on".
 */
@Component({
  selector: 'app-footer',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer class="app-footer">
      <span>&copy; {{ year }} {{ settings.name }}</span>
      <span class="app-footer__meta">
        {{ 'common.version' | translate }} {{ settings.version }} · Angular {{ angularVersion }}
      </span>
    </footer>
  `,
  styles: `
    .app-footer {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--app-spacing-xs);
      padding: var(--app-spacing-sm) var(--app-spacing-lg);
      border-top: 1px solid var(--app-color-border);
      color: var(--app-color-text-muted);
      font-size: var(--app-font-size-xs);
    }

    .app-footer__meta {
      font-variant-numeric: tabular-nums;
    }
  `,
})
export class FooterComponent {
  protected readonly settings = inject(APP_SETTINGS);
  protected readonly year = new Date().getFullYear();
  protected readonly angularVersion = VERSION.full;
}
