import { InjectionToken } from '@angular/core';
import type { ThemeConfig } from './theme.model';

/**
 * Theme configuration.
 *
 * Provided in `app.config.ts` from `config/theme.config.ts`, so `core/` never
 * imports application configuration directly.
 */
export const THEME_CONFIG = new InjectionToken<ThemeConfig>('THEME_CONFIG');
