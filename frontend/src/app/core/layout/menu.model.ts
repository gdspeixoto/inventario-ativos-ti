/**
 * Navigation model.
 *
 * Menus are data, not markup: the sidebar renders whatever `menu.config.ts`
 * declares, and filters it against the current user's roles and permissions.
 */

export interface AppMenuItem {
  /** Stable id, used as the expansion-state key and for testing hooks. */
  id: string;
  /** i18n key resolved at render time. */
  label: string;
  /** PrimeIcons class, e.g. `pi pi-home`. */
  icon?: string;
  /** Internal route. Mutually exclusive with `url`. */
  route?: string;
  /** External link — opened in a new tab. */
  url?: string;
  /** Nested items; renders as an expandable group. */
  children?: AppMenuItem[];
  /** Hidden unless the user has at least one of these roles. */
  roles?: string[];
  /** Hidden unless the user has at least one of these permissions. */
  permissions?: string[];
  /** Small counter or tag rendered on the right. */
  badge?: string;
  /** Hidden entirely when false. Useful with feature flags. */
  visible?: boolean;
  /** Exact route matching for the active state. Defaults to prefix matching. */
  exact?: boolean;
}

export interface AppMenuSection {
  id: string;
  /** i18n key for the section heading; omit for an unlabelled group. */
  label?: string;
  items: AppMenuItem[];
}

/** Breadcrumb entry produced by `BreadcrumbService` from route data. */
export interface BreadcrumbItem {
  label: string;
  route?: string;
  icon?: string;
}
