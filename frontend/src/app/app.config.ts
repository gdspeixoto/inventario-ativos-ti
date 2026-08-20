import {
  ApplicationConfig,
  ErrorHandler,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  TitleStrategy,
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withRouterConfig,
} from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';

import { routes } from './app.routes';

import { APP_SETTINGS } from '@core/app-settings';
import { AUTH_CONFIG } from '@core/authentication/auth.tokens';
import { AuthService } from '@core/authentication/auth.service';
import { AppErrorHandler } from '@core/errors/app-error-handler';
import { I18N_CONFIG } from '@core/i18n/i18n.tokens';
import { TranslationService } from '@core/i18n/translation.service';
import { MENU_SECTIONS } from '@core/layout/menu.tokens';
import { AppTitleStrategy } from '@core/layout/app-title.strategy';
import { THEME_CONFIG } from '@core/theme/theme.tokens';
import { ThemeService } from '@core/theme/theme.service';
import { CorporatePreset } from '@core/theme/primeng-preset';

import { apiUrlInterceptor } from '@core/interceptors/api-url.interceptor';
import { authInterceptor } from '@core/interceptors/auth.interceptor';
import { errorInterceptor } from '@core/interceptors/error.interceptor';
import { loadingInterceptor } from '@core/interceptors/loading.interceptor';

import { appSettings } from '@config/app.settings';
import { authConfig } from '@config/auth.config';
import { i18nConfig } from '@config/i18n.config';
import { menuSections } from '@config/menu.config';
import { themeConfig } from '@config/theme.config';

/**
 * Application composition root.
 *
 * This is the only file that knows about both `core/` and `config/`: the core
 * libraries declare injection tokens, and this file fills them with the values
 * from `config/`. That indirection is what makes `core/` reusable across
 * projects without edits.
 */
/*
 * Os pipes `currency`, `date` e `number` exigem os dados do locale carregados.
 * Sem este registro, qualquer template que formate dinheiro lanca NG0701 e
 * derruba a renderizacao da tela inteira — nao apenas do valor.
 */
registerLocaleData(localePt, 'pt-BR');

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: 'pt-BR' },

    provideBrowserGlobalErrorListeners(),

    provideRouter(
      routes,
      // Binds `data.code` on the error routes straight into a component input.
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
      withRouterConfig({ onSameUrlNavigation: 'reload' }),
    ),

    /**
     * Interceptor order is deliberate. The array runs first → last on the way
     * out and last → first on the way back, so `authInterceptor` sits closest
     * to the network and sees a raw 401 *before* `errorInterceptor` normalizes
     * it into an `AppHttpError` — which is what lets the silent refresh work.
     */
    provideHttpClient(
      withFetch(),
      withInterceptors([apiUrlInterceptor, loadingInterceptor, errorInterceptor, authInterceptor]),
    ),

    providePrimeNG({
      theme: {
        preset: CorporatePreset,
        options: {
          // Same selector the design tokens use — one source of truth for dark mode.
          darkModeSelector: '[data-theme="dark"]',
          // Layered so Tailwind utilities can still override component styles.
          cssLayer: { name: 'primeng', order: 'theme, base, primeng, utilities' },
        },
      },
      ripple: themeConfig.ripple,
      inputVariant: themeConfig.inputVariant,
    }),

    MessageService,
    ConfirmationService,

    { provide: ErrorHandler, useClass: AppErrorHandler },
    { provide: TitleStrategy, useClass: AppTitleStrategy },

    { provide: APP_SETTINGS, useValue: appSettings },
    { provide: AUTH_CONFIG, useValue: authConfig },
    { provide: THEME_CONFIG, useValue: themeConfig },
    { provide: I18N_CONFIG, useValue: i18nConfig },
    { provide: MENU_SECTIONS, useValue: menuSections },

    /**
     * Boot sequence, awaited before the first route activates:
     *   1. theme  — applied synchronously so there is no flash of light mode;
     *   2. i18n   — the first paint is already in the right language;
     *   3. auth   — guards then observe a resolved session, never `unknown`.
     */
    provideAppInitializer(() => {
      inject(ThemeService).initialize();
      const i18n = inject(TranslationService);
      const auth = inject(AuthService);
      return Promise.all([i18n.initialize(), auth.restoreSession()]);
    }),
  ],
};
