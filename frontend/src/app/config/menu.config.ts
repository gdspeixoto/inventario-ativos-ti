import type { AppMenuSection } from '@core/layout/menu.model';

/**
 * Sidebar navigation.
 *
 * Labels are i18n keys (`public/i18n/*.json`). Items declaring `roles` or
 * `permissions` are hidden from users who lack them — a convenience, not a
 * security boundary: the route guards and the backend do the enforcing.
 */
export const menuSections: AppMenuSection[] = [
  {
    id: 'main',
    label: 'nav.sections.main',
    items: [
      {
        id: 'dashboard',
        label: 'nav.dashboard',
        icon: 'pi pi-home',
        route: '/dashboard',
        exact: true,
      },
      {
        id: 'licenses',
        label: 'nav.licenses',
        icon: 'pi pi-id-card',
        route: '/licenses',
      },
      {
        id: 'servers',
        label: 'nav.servers',
        icon: 'pi pi-server',
        route: '/servers',
      },
      {
        id: 'contracts',
        label: 'nav.contracts',
        icon: 'pi pi-file-edit',
        route: '/contracts',
      },
      {
        id: 'suppliers',
        label: 'nav.suppliers',
        icon: 'pi pi-building',
        route: '/suppliers',
      },
      {
        id: 'costs',
        label: 'nav.costs',
        icon: 'pi pi-dollar',
        route: '/costs',
      },
      {
        id: 'timeline',
        label: 'nav.timeline',
        icon: 'pi pi-history',
        route: '/timeline',
      },
      {
        id: 'alerts',
        label: 'nav.alerts',
        icon: 'pi pi-bell',
        route: '/alerts',
      },
      {
        id: 'reports',
        label: 'nav.reports',
        icon: 'pi pi-chart-line',
        route: '/reports',
      },
    ],
  },
  {
    id: 'system',
    label: 'nav.sections.system',
    items: [
      {
        id: 'organization',
        label: 'nav.organization',
        icon: 'pi pi-sitemap',
        route: '/organization',
      },
      {
        id: 'settings',
        label: 'nav.settings',
        icon: 'pi pi-cog',
        route: '/settings',
      },
    ],
  },
];
