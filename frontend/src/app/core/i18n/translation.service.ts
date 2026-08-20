import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { identityProviderContext } from '../http/http-context.tokens';
import { LoggerService } from '../services/logger.service';
import { PlatformService } from '../services/platform.service';
import { StorageService } from '../services/storage.service';
import { I18N_CONFIG } from './i18n.tokens';
import type {
  LocaleCode,
  LocaleDefinition,
  TranslationBundle,
  TranslationParams,
} from './i18n.model';

/**
 * Runtime translation layer.
 *
 * Bundles are plain JSON under `public/i18n/`, fetched on demand and cached.
 * Keys are dot-separated (`dashboard.welcome`) and nested JSON is flattened at
 * load time, so authors keep a readable file while lookups stay O(1).
 *
 * A missing key returns the key itself: an untranslated string is visible in
 * the UI and in tests, which beats silently rendering an empty label.
 */
@Injectable({ providedIn: 'root' })
export class TranslationService {
  private readonly config = inject(I18N_CONFIG);
  private readonly http = inject(HttpClient);
  private readonly storage = inject(StorageService);
  private readonly platform = inject(PlatformService);
  private readonly logger = inject(LoggerService).forContext('i18n');

  private readonly bundles = new Map<LocaleCode, TranslationBundle>();
  private readonly _locale = signal<LocaleCode>(this.config.defaultLocale);
  private readonly _bundle = signal<TranslationBundle>({});
  private readonly _ready = signal(false);

  readonly locale = this._locale.asReadonly();
  readonly ready = this._ready.asReadonly();
  readonly locales: readonly LocaleDefinition[] = this.config.locales;

  readonly currentLocale = computed<LocaleDefinition>(
    () =>
      this.config.locales.find((locale) => locale.code === this._locale()) ??
      this.config.locales[0],
  );

  /**
   * Loads the initial bundle. Called once by `provideAppInitializer` so the
   * first paint is never in the wrong language.
   */
  async initialize(): Promise<void> {
    await this.use(this.resolveInitialLocale());
    this._ready.set(true);
  }

  /** Switches language, loading the bundle if it is not cached yet. */
  async use(code: LocaleCode): Promise<void> {
    const bundle = await this.loadBundle(code);
    this._locale.set(code);
    this._bundle.set(bundle);
    this.storage.set(this.config.storageKey, code);
    this.platform.document.documentElement.lang = code;
    this.platform.document.documentElement.dir = this.currentLocale().direction ?? 'ltr';
  }

  /**
   * Resolves a key against the active bundle.
   *
   * `translate('users.count', { total: 12 })` replaces `{total}` in the value.
   */
  translate(key: string, params?: TranslationParams): string {
    const template = this._bundle()[key];
    if (template === undefined) {
      return key;
    }
    return params ? interpolate(template, params) : template;
  }

  /** Reactive variant for templates: re-renders when the language changes. */
  translateSignal(key: string, params?: TranslationParams) {
    return computed(() => {
      // Read the bundle signal so the computed tracks language changes.
      this._bundle();
      return this.translate(key, params);
    });
  }

  has(key: string): boolean {
    return key in this._bundle();
  }

  private resolveInitialLocale(): LocaleCode {
    const stored = this.storage.get<LocaleCode>(this.config.storageKey);
    if (stored && this.isSupported(stored)) {
      return stored;
    }

    if (this.config.detectBrowserLocale) {
      const browserLocale = this.platform.window?.navigator.language;
      if (browserLocale) {
        // Prefer an exact match (pt-BR), then the language family (pt).
        const exact = this.config.locales.find((locale) => locale.code === browserLocale);
        const family = this.config.locales.find((locale) =>
          locale.code.toLowerCase().startsWith(browserLocale.split('-')[0].toLowerCase()),
        );
        if (exact ?? family) {
          return (exact ?? family)!.code;
        }
      }
    }

    return this.config.defaultLocale;
  }

  private isSupported(code: LocaleCode): boolean {
    return this.config.locales.some((locale) => locale.code === code);
  }

  private async loadBundle(code: LocaleCode): Promise<TranslationBundle> {
    const cached = this.bundles.get(code);
    if (cached) {
      return cached;
    }

    const url = this.config.assetPath.replace('{locale}', code);
    try {
      const raw = await firstValueFrom(
        this.http.get<Record<string, unknown>>(url, { context: identityProviderContext() }),
      );
      const bundle = Object.freeze(flatten(raw));
      this.bundles.set(code, bundle);
      return bundle;
    } catch (error) {
      this.logger.warn(`Could not load the "${code}" bundle from ${url}.`, error);
      if (code !== this.config.fallbackLocale) {
        return this.loadBundle(this.config.fallbackLocale);
      }
      return {};
    }
  }
}

/** `{ a: { b: 'x' } }` → `{ 'a.b': 'x' }`. */
function flatten(source: Record<string, unknown>, prefix = ''): Record<string, string> {
  return Object.entries(source).reduce<Record<string, string>>((accumulator, [key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key;
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      Object.assign(accumulator, flatten(value as Record<string, unknown>, path));
    } else {
      accumulator[path] = String(value);
    }
    return accumulator;
  }, {});
}

function interpolate(template: string, params: TranslationParams): string {
  return template.replace(/\{(\w+)\}/gu, (match, key: string) =>
    key in params ? String(params[key]) : match,
  );
}
