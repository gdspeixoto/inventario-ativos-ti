import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BreadcrumbService } from '@core/layout/breadcrumb.service';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * Breadcrumb trail built from route metadata by `BreadcrumbService`.
 *
 * Hand-rolled rather than `p-breadcrumb` so the markup can be a real
 * `<nav><ol>` with `aria-current="page"` on the last crumb — the structure
 * assistive technology expects (WCAG 2.4.8).
 */
@Component({
  selector: 'app-breadcrumb',
  imports: [RouterLink, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (items().length > 1) {
      <nav class="app-breadcrumb" [attr.aria-label]="'nav.breadcrumb' | translate">
        <ol>
          @for (item of items(); track item.route; let last = $last) {
            <li>
              @if (last) {
                <span aria-current="page">{{ item.label | translate }}</span>
              } @else {
                <a [routerLink]="item.route">{{ item.label | translate }}</a>
                <i class="pi pi-angle-right" aria-hidden="true"></i>
              }
            </li>
          }
        </ol>
      </nav>
    }
  `,
  styles: `
    .app-breadcrumb ol {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--app-spacing-2xs);
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .app-breadcrumb li {
      display: flex;
      align-items: center;
      gap: var(--app-spacing-2xs);
      font-size: var(--app-font-size-xs);
    }

    .app-breadcrumb a {
      color: var(--app-color-text-muted);
      text-decoration: none;
    }

    .app-breadcrumb a:hover {
      color: var(--app-color-text);
      text-decoration: underline;
    }

    .app-breadcrumb i {
      color: var(--app-color-border-strong);
      font-size: 0.65rem;
    }

    .app-breadcrumb [aria-current='page'] {
      color: var(--app-color-text-secondary);
      font-weight: var(--app-font-weight-medium);
    }
  `,
})
export class BreadcrumbComponent {
  readonly items = inject(BreadcrumbService).items;
}
