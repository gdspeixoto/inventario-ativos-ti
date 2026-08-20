import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { APP_SETTINGS } from '@core/app-settings';

/**
 * Product mark.
 *
 * Replacing the logo means swapping `public/logo.svg` and, if the proportions
 * differ, adjusting the sizes here — no other file references the asset.
 */
@Component({
  selector: 'app-brand-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="app-brand" [class]="'app-brand--' + size()">
      <img [src]="settings.logoUrl" [alt]="settings.name" class="app-brand__mark" />
      @if (showName()) {
        <span class="app-brand__name">{{ label() }}</span>
      }
    </span>
  `,
  styles: `
    .app-brand {
      display: inline-flex;
      align-items: center;
      gap: var(--app-spacing-xs);
      color: var(--app-color-text);
    }

    .app-brand__name {
      font-weight: var(--app-font-weight-semibold);
      letter-spacing: var(--app-letter-spacing-tight);
    }

    .app-brand--sm .app-brand__mark {
      width: 1.5rem;
      height: 1.5rem;
    }

    .app-brand--sm .app-brand__name {
      font-size: var(--app-font-size-md);
    }

    .app-brand--md .app-brand__mark {
      width: 2rem;
      height: 2rem;
    }

    .app-brand--md .app-brand__name {
      font-size: var(--app-font-size-lg);
    }

    .app-brand--lg .app-brand__mark {
      width: 3rem;
      height: 3rem;
    }

    .app-brand--lg .app-brand__name {
      font-size: var(--app-font-size-xl);
    }
  `,
})
export class BrandLogoComponent {
  protected readonly settings = inject(APP_SETTINGS);

  readonly size = input<'sm' | 'md' | 'lg'>('sm');
  readonly showName = input(true);
  /** Uses the short name by default; pass `full` on the login screen. */
  readonly nameVariant = input<'short' | 'full'>('short');

  label(): string {
    return this.nameVariant() === 'full' ? this.settings.name : this.settings.shortName;
  }
}
