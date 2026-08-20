import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Tooltip } from 'primeng/tooltip';
import { LayoutStore } from '../layout.store';
import { MenuService } from '../menu.service';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import type { AppMenuItem } from '../menu.model';

/**
 * Primary navigation.
 *
 * Three responsive shapes from one template:
 *  - desktop expanded — icon + label;
 *  - desktop rail (collapsed) — icon only, label in a tooltip;
 *  - mobile — overlay drawer, closed on navigation.
 *
 * Groups expand in place; the expansion state is per session and keyed by item
 * id. `routerLinkActive` drives both the highlight and `aria-current`.
 */
@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, Tooltip, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent {
  protected readonly layout = inject(LayoutStore);
  protected readonly menu = inject(MenuService);

  private readonly expandedIds = signal<ReadonlySet<string>>(new Set());

  isExpanded(item: AppMenuItem): boolean {
    return this.expandedIds().has(item.id);
  }

  toggleGroup(item: AppMenuItem): void {
    this.expandedIds.update((current) => {
      const next = new Set(current);
      if (next.has(item.id)) {
        next.delete(item.id);
      } else {
        next.add(item.id);
      }
      return next;
    });
  }

  onNavigate(): void {
    if (this.layout.isMobile()) {
      this.layout.closeMobileSidebar();
    }
  }
}
