import { InjectionToken } from '@angular/core';
import type { I18nConfig } from './i18n.model';

/** Provided in `app.config.ts` from `config/i18n.config.ts`. */
export const I18N_CONFIG = new InjectionToken<I18nConfig>('I18N_CONFIG');
