import { Injectable, computed, inject, signal } from '@angular/core';
import { THEME_CONFIG } from './theme.tokens';
import type { ResolvedTheme, ThemeDensity, ThemeMode } from './theme.model';

/**
 * Theme state.
 *
 * A store in this template is a plain injectable holding signals: state is
 * `readonly` to consumers, mutations go through intention-revealing methods.
 * No NgRx, no reducers, no boilerplate — see `docs/architecture.md`.
 *
 * Side effects (DOM attribute, persistence, media queries) live in
 * `ThemeService`; this class stays synchronous and trivially testable.
 */
@Injectable({ providedIn: 'root' })
export class ThemeStore {
  private readonly config = inject(THEME_CONFIG);

  private readonly _mode = signal<ThemeMode>(this.config.defaultMode);
  private readonly _density = signal<ThemeDensity>(this.config.defaultDensity);
  private readonly _systemPrefersDark = signal(false);

  /** What the user selected — may be `system`. */
  readonly mode = this._mode.asReadonly();
  readonly density = this._density.asReadonly();
  readonly systemPrefersDark = this._systemPrefersDark.asReadonly();

  /** What is actually painted, after resolving `system`. */
  readonly resolved = computed<ResolvedTheme>(() => {
    const mode = this._mode();
    if (mode === 'system') {
      return this._systemPrefersDark() ? 'dark' : 'light';
    }
    return mode;
  });

  readonly isDark = computed(() => this.resolved() === 'dark');
  readonly isFollowingSystem = computed(() => this._mode() === 'system');
  readonly canToggle = this.config.allowUserToggle;

  setMode(mode: ThemeMode): void {
    this._mode.set(mode);
  }

  setDensity(density: ThemeDensity): void {
    this._density.set(density);
  }

  /** Flips between light and dark, dropping the `system` binding. */
  toggle(): void {
    this._mode.set(this.resolved() === 'dark' ? 'light' : 'dark');
  }

  /** Called by `ThemeService` when the OS preference changes. */
  setSystemPrefersDark(prefersDark: boolean): void {
    this._systemPrefersDark.set(prefersDark);
  }
}
