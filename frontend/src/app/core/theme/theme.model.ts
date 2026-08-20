/**
 * Theme contracts.
 *
 * `ThemeMode` is what the *user* picks; `ResolvedTheme` is what actually gets
 * painted — `system` resolves to one or the other based on the OS preference.
 */

export type ThemeMode = 'light' | 'dark' | 'system';
export type ResolvedTheme = 'light' | 'dark';

/** Layout density, applied as `data-density` on <html>. */
export type ThemeDensity = 'comfortable' | 'compact';

export interface ThemeConfig {
  /** Mode used on first visit, before the user expresses a preference. */
  defaultMode: ThemeMode;
  defaultDensity: ThemeDensity;
  /** Hides the theme switcher when false. */
  allowUserToggle: boolean;
  /** LocalStorage key holding the persisted preference. */
  storageKey: string;
  /** Attribute written on <html>. Must match the CSS token selectors. */
  attribute: string;
  /** PrimeNG ripple effect on buttons and list items. */
  ripple: boolean;
  /** PrimeNG input style. `outlined` matches the corporate look. */
  inputVariant: 'outlined' | 'filled';
}

export interface ThemePreference {
  mode: ThemeMode;
  density: ThemeDensity;
}

export const THEME_MODES: readonly ThemeMode[] = ['light', 'dark', 'system'] as const;

export function isThemeMode(value: unknown): value is ThemeMode {
  return typeof value === 'string' && (THEME_MODES as readonly string[]).includes(value);
}
