import type { ThemeConfig } from '@core/theme/theme.model';

/**
 * Theme configuration.
 *
 * Colors are NOT configured here — they live in `src/styles/tokens`. This file
 * only decides behaviour: which mode a first-time visitor gets, whether the
 * switcher is visible and how the preference is persisted.
 */
export const themeConfig: ThemeConfig = {
  defaultMode: 'system',
  defaultDensity: 'comfortable',
  allowUserToggle: true,
  storageKey: 'theme.preference',
  attribute: 'data-theme',
  ripple: true,
  inputVariant: 'outlined',
};
