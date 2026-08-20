import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { BreadcrumbComponent } from '@shared/components/breadcrumb/breadcrumb.component';

/**
 * Standard page heading: breadcrumb, title, description and an action slot.
 *
 * Every routed page starts with this component, which is what makes different
 * modules — written by different squads — still feel like one product.
 *
 * ```html
 * <app-page-header title="users.title" description="users.subtitle">
 *   <p-button label="New user" icon="pi pi-plus" />
 * </app-page-header>
 * ```
 */
@Component({
  selector: 'app-page-header',
  imports: [TranslatePipe, BreadcrumbComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="app-page-header">
      @if (showBreadcrumb()) {
        <app-breadcrumb />
      }

      <div class="app-page-header__row">
        <div class="app-page-header__text">
          <h1 class="app-page-header__title">
            @if (icon()) {
              <i class="{{ icon() }}" aria-hidden="true"></i>
            }
            {{ title() | translate }}
          </h1>

          @if (description()) {
            <p class="app-page-header__description">{{ description()! | translate }}</p>
          }
        </div>

        <div class="app-page-header__actions">
          <ng-content />
        </div>
      </div>
    </header>
  `,
  styles: `
    .app-page-header {
      display: flex;
      flex-direction: column;
      gap: var(--app-spacing-xs);
      margin-bottom: var(--app-spacing-lg);
    }

    .app-page-header__row {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-start;
      justify-content: space-between;
      gap: var(--app-spacing-md);
    }

    .app-page-header__title {
      display: flex;
      align-items: center;
      gap: var(--app-spacing-xs);
      font-size: var(--app-font-size-2xl);
    }

    .app-page-header__title i {
      color: var(--app-color-text-muted);
      font-size: 1.1rem;
    }

    .app-page-header__description {
      margin-top: var(--app-spacing-2xs);
      max-width: 60ch;
      color: var(--app-color-text-secondary);
      font-size: var(--app-font-size-sm);
    }

    .app-page-header__actions {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--app-spacing-xs);
    }
  `,
})
export class PageHeaderComponent {
  /** i18n key. */
  readonly title = input.required<string>();
  /** i18n key rendered below the title. */
  readonly description = input<string | null>(null);
  readonly icon = input<string | null>(null);
  readonly showBreadcrumb = input(true);
}
