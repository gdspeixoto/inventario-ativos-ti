import { InjectionToken } from '@angular/core';

/**
 * Application identity and feature switches.
 *
 * The one place a new project must edit to stop looking like the template:
 * name, logo, support contact and the flags that toggle optional UI.
 */
export interface AppSettings {
  /** Full product name, shown on the login screen and in the page title. */
  name: string;
  /** Compact name for the navbar next to the logo. */
  shortName: string;
  /** One-line description under the login card. */
  description: string;
  /** Template version reported on the dashboard. */
  version: string;
  /** Path to the logo asset, relative to `public/`. */
  logoUrl: string;
  /** Suffix appended to every page title: "Dashboard · Template". */
  titleSeparator: string;
  /** Team or mailbox shown on error pages. */
  supportEmail: string;
  /** External documentation link in the user menu; hidden when empty. */
  documentationUrl: string;
  features: {
    globalSearch: boolean;
    languageSwitcher: boolean;
    breadcrumbs: boolean;
    collapsibleSidebar: boolean;
  };
}

/** Provided in `app.config.ts` from `config/app.settings.ts`. */
export const APP_SETTINGS = new InjectionToken<AppSettings>('APP_SETTINGS');
