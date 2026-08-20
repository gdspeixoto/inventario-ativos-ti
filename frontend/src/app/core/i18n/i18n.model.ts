/**
 * Internationalization contracts.
 *
 * The template ships a small runtime translation layer instead of Angular's
 * build-time i18n: corporate apps usually need to switch language at runtime
 * without producing one bundle per locale. Swapping in `@ngx-translate` or
 * `@angular/localize` later only touches `TranslationService`.
 */

export type LocaleCode = string;

export interface LocaleDefinition {
  /** BCP 47 tag, e.g. `pt-BR`. */
  code: LocaleCode;
  /** Name shown in the language picker, in its own language. */
  label: string;
  /** Two-letter flag/icon hint, e.g. `br`, `us`. */
  flag?: string;
  /** Used by `DatePipe`/`DecimalPipe` — usually identical to `code`. */
  intlLocale: string;
  direction?: 'ltr' | 'rtl';
}

export interface I18nConfig {
  defaultLocale: LocaleCode;
  fallbackLocale: LocaleCode;
  locales: readonly LocaleDefinition[];
  /** LocalStorage key holding the user's language choice. */
  storageKey: string;
  /** Path template for translation bundles; `{locale}` is substituted. */
  assetPath: string;
  /** Uses `navigator.language` on first visit when it matches a known locale. */
  detectBrowserLocale: boolean;
}

/** Flat key/value bundle; nested JSON is flattened at load time. */
export type TranslationBundle = Readonly<Record<string, string>>;

/** Values interpolated into `{placeholders}` in a translation. */
export type TranslationParams = Readonly<Record<string, string | number>>;
