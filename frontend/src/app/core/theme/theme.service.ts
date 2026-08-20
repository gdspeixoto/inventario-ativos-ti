import { DestroyRef, Injectable, Injector, effect, inject } from '@angular/core';
import { PlatformService } from '../services/platform.service';
import { StorageService } from '../services/storage.service';
import { ThemeStore } from './theme.store';
import { THEME_CONFIG } from './theme.tokens';
import {
  isThemeMode,
  type ThemeDensity,
  type ThemeMode,
  type ThemePreference,
} from './theme.model';

const SYSTEM_DARK_QUERY = '(prefers-color-scheme: dark)';

/**
 * Applies the theme to the document and keeps the user's preference.
 *
 * Responsibilities, in order:
 *   1. restore the persisted preference (falling back to `defaultMode`);
 *   2. track the OS preference so `system` stays live;
 *   3. mirror the resolved theme onto `<html data-theme>` for CSS tokens,
 *      PrimeNG's `darkModeSelector` and Tailwind's `dark:` variant;
 *   4. persist every change.
 *
 * Initialization is triggered once by `provideAppInitializer` in `app.config.ts`.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly config = inject(THEME_CONFIG);
  private readonly store = inject(ThemeStore);
  private readonly storage = inject(StorageService);
  private readonly platform = inject(PlatformService);
  private readonly destroyRef = inject(DestroyRef);
  /** Passed to `effect()` so `initialize()` can be called from anywhere. */
  private readonly injector = inject(Injector);

  /** Re-exported so components can inject only `ThemeService`. */
  readonly mode = this.store.mode;
  readonly density = this.store.density;
  readonly resolved = this.store.resolved;
  readonly isDark = this.store.isDark;
  readonly isFollowingSystem = this.store.isFollowingSystem;
  readonly canToggle = this.store.canToggle;

  private initialized = false;

  initialize(): void {
    if (this.initialized) {
      return;
    }
    this.initialized = true;

    this.restorePreference();
    this.watchSystemPreference();

    effect(
      () => {
        const theme = this.store.resolved();
        const density = this.store.density();
        this.applyToDocument(theme, density);
        this.persist({ mode: this.store.mode(), density });
      },
      { injector: this.injector },
    );
  }

  setMode(mode: ThemeMode): void {
    this.store.setMode(mode);
  }

  setDensity(density: ThemeDensity): void {
    this.store.setDensity(density);
  }

  toggle(): void {
    if (!this.config.allowUserToggle) {
      return;
    }
    this.store.toggle();
  }

  /** Hands control back to the operating system preference. */
  followSystem(): void {
    this.store.setMode('system');
  }

  private restorePreference(): void {
    const stored = this.storage.get<Partial<ThemePreference>>(this.config.storageKey);
    if (stored && isThemeMode(stored.mode)) {
      this.store.setMode(stored.mode);
    }
    if (stored?.density === 'compact' || stored?.density === 'comfortable') {
      this.store.setDensity(stored.density);
    }
  }

  private watchSystemPreference(): void {
    const mediaQuery = this.platform.matchMedia(SYSTEM_DARK_QUERY);
    if (!mediaQuery) {
      return;
    }

    this.store.setSystemPrefersDark(mediaQuery.matches);

    const onChange = (event: MediaQueryListEvent) => this.store.setSystemPrefersDark(event.matches);
    mediaQuery.addEventListener('change', onChange);
    this.destroyRef.onDestroy(() => mediaQuery.removeEventListener('change', onChange));
  }

  private applyToDocument(theme: string, density: ThemeDensity): void {
    const root = this.platform.document.documentElement;
    root.setAttribute(this.config.attribute, theme);
    root.setAttribute('data-density', density);
    root.style.colorScheme = theme;
  }

  private persist(preference: ThemePreference): void {
    this.storage.set(this.config.storageKey, preference);
  }
}
