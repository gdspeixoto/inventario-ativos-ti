import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';
import { guestGuard } from '@core/guards/guest.guard';

/**
 * Application routes.
 *
 * Structure:
 *  - public routes (login, OIDC callback, error pages) render standalone;
 *  - everything else lives under `ShellComponent`, guarded once by `authGuard`,
 *    so a new page cannot accidentally be left unprotected.
 *
 * Every feature is lazy-loaded with `loadComponent`, which keeps the initial
 * bundle to the shell plus the login screen. `data.breadcrumb` feeds
 * `BreadcrumbService`; `title` feeds the page-title strategy.
 */
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'auth.signIn',
    loadComponent: () => import('@pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'auth/callback',
    loadComponent: () =>
      import('@pages/auth-callback/auth-callback.component').then((m) => m.AuthCallbackComponent),
  },

  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('@core/layout/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'dashboard.title',
        data: { breadcrumb: 'nav.dashboard' },
        loadComponent: () =>
          import('@pages/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'licenses',
        title: 'licenses.title',
        data: { breadcrumb: 'nav.licenses' },
        loadComponent: () =>
          import('@pages/licenses/licenses.component').then((m) => m.LicensesComponent),
      },
      {
        path: 'licenses/:id',
        title: 'licenses.detailTitle',
        data: { breadcrumb: 'nav.licenses' },
        loadComponent: () =>
          import('@pages/licenses/license-detail.component').then((m) => m.LicenseDetailComponent),
      },
      {
        path: 'servers',
        title: 'servers.title',
        data: { breadcrumb: 'nav.servers' },
        loadComponent: () =>
          import('@pages/servers/servers.component').then((m) => m.ServersComponent),
      },
      {
        path: 'servers/:id',
        title: 'servers.detailTitle',
        data: { breadcrumb: 'nav.servers' },
        loadComponent: () =>
          import('@pages/servers/server-detail.component').then((m) => m.ServerDetailComponent),
      },
      {
        path: 'contracts',
        title: 'contracts.title',
        data: { breadcrumb: 'nav.contracts' },
        loadComponent: () =>
          import('@pages/contracts/contracts.component').then((m) => m.ContractsComponent),
      },
      {
        path: 'suppliers',
        title: 'suppliers.title',
        data: { breadcrumb: 'nav.suppliers' },
        loadComponent: () =>
          import('@pages/suppliers/suppliers.component').then((m) => m.SuppliersComponent),
      },
      {
        path: 'costs',
        title: 'costs.title',
        data: { breadcrumb: 'nav.costs' },
        loadComponent: () => import('@pages/costs/costs.component').then((m) => m.CostsComponent),
      },
      {
        path: 'timeline',
        title: 'timeline.title',
        data: { breadcrumb: 'nav.timeline' },
        loadComponent: () =>
          import('@pages/timeline/timeline.component').then((m) => m.TimelineComponent),
      },
      {
        path: 'alerts',
        title: 'alerts.title',
        data: { breadcrumb: 'nav.alerts' },
        loadComponent: () =>
          import('@pages/alerts/alerts.component').then((m) => m.AlertsComponent),
      },
      {
        path: 'reports',
        title: 'reports.title',
        data: { breadcrumb: 'nav.reports' },
        loadComponent: () =>
          import('@pages/reports/reports.component').then((m) => m.ReportsComponent),
      },
      {
        path: 'organization',
        title: 'organization.title',
        data: { breadcrumb: 'nav.organization' },
        loadComponent: () =>
          import('@pages/organization/organization.component').then((m) => m.OrganizationComponent),
      },
      {
        path: 'organization/units/:id',
        title: 'organization.detailTitle',
        data: { breadcrumb: 'nav.organization' },
        loadComponent: () =>
          import('@pages/organization/unit-detail.component').then((m) => m.UnitDetailComponent),
      },
      {
        path: 'settings',
        title: 'settings.title',
        data: { breadcrumb: 'nav.settings' },
        loadComponent: () =>
          import('@pages/settings/settings.component').then((m) => m.SettingsComponent),
      },
    ],
  },

  // Error pages share one component; the status arrives through `data.code`.
  ...(['401', '403', '404', '500'] as const).map((code) => ({
    path: code,
    data: { code },
    title: `errors.${code}.title`,
    loadComponent: () =>
      import('@pages/errors/error-page.component').then((m) => m.ErrorPageComponent),
  })),

  { path: '**', redirectTo: '404' },
];
