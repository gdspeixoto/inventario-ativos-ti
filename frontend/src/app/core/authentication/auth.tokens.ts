import { InjectionToken } from '@angular/core';
import type { AuthConfig } from './models/auth-config.model';

/**
 * Authentication configuration.
 *
 * Provided in `app.config.ts` from `config/auth.config.ts`. Nothing under
 * `core/authentication` imports application configuration directly, which keeps
 * the library reusable across projects.
 */
export const AUTH_CONFIG = new InjectionToken<AuthConfig>('AUTH_CONFIG');
