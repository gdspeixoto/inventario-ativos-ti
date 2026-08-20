import { InjectionToken } from '@angular/core';
import type { AppMenuSection } from './menu.model';

/** Provided in `app.config.ts` from `config/menu.config.ts`. */
export const MENU_SECTIONS = new InjectionToken<AppMenuSection[]>('MENU_SECTIONS');
