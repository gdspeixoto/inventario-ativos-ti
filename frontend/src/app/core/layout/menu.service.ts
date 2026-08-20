import { Injectable, computed, inject } from '@angular/core';
import { AuthService } from '../authentication/auth.service';
import { MENU_SECTIONS } from './menu.tokens';
import type { AppMenuItem, AppMenuSection } from './menu.model';

/**
 * Filters the configured navigation against the current user.
 *
 * An item is visible when it is not explicitly hidden and the user holds at
 * least one of its roles/permissions. Groups whose children all disappear are
 * dropped too, so no empty expander is left behind.
 *
 * Hiding a link is a usability decision, not a security one — the guards on the
 * route and the API still decide what may actually be reached.
 */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly sections = inject(MENU_SECTIONS);
  private readonly auth = inject(AuthService);

  readonly visibleSections = computed<AppMenuSection[]>(() => {
    // Read the user so the menu recomputes when roles change (login/refresh).
    this.auth.user();

    return this.sections
      .map((section) => ({ ...section, items: this.filterItems(section.items) }))
      .filter((section) => section.items.length > 0);
  });

  private filterItems(items: readonly AppMenuItem[]): AppMenuItem[] {
    return items.reduce<AppMenuItem[]>((visible, item) => {
      if (!this.isAllowed(item)) {
        return visible;
      }

      if (item.children?.length) {
        const children = this.filterItems(item.children);
        if (children.length === 0 && !item.route) {
          return visible;
        }
        return [...visible, { ...item, children }];
      }

      return [...visible, item];
    }, []);
  }

  private isAllowed(item: AppMenuItem): boolean {
    if (item.visible === false) {
      return false;
    }
    if (item.roles?.length && !this.auth.hasAnyRole(item.roles)) {
      return false;
    }
    if (item.permissions?.length && !this.auth.hasAnyPermission(item.permissions)) {
      return false;
    }
    return true;
  }
}
