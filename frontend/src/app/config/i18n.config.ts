import type { I18nConfig } from '@core/i18n/i18n.model';

/**
 * Internationalization configuration.
 *
 * Adding a language: drop `public/i18n/<code>.json` next to the existing files
 * and append an entry to `locales`. Nothing else changes.
 */
export const i18nConfig: I18nConfig = {
  defaultLocale: 'pt-BR',
  fallbackLocale: 'pt-BR',
  storageKey: 'i18n.locale',
  assetPath: 'i18n/{locale}.json',
  detectBrowserLocale: true,
  locales: [
    { code: 'pt-BR', label: 'Português (Brasil)', flag: 'br', intlLocale: 'pt-BR' },
    { code: 'en-US', label: 'English (US)', flag: 'us', intlLocale: 'en-US' },
  ],
};
