import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../authentication/auth.service';
import { AUTH_CONFIG } from '../authentication/auth.tokens';

/**
 * Requires an authenticated session.
 *
 * The session is already resolved by the time any guard runs
 * (`provideAppInitializer` awaits `restoreSession()`), so this guard is
 * synchronous — no flicker, no "unknown" state to handle.
 *
 * The blocked URL is stored as the return URL, so the user lands where they
 * intended after signing in.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const config = inject(AUTH_CONFIG);

  if (auth.isAuthenticated()) {
    return true;
  }

  auth.setReturnUrl(state.url);
  return router.createUrlTree([config.routes.login]);
};

/** Same rule applied to every child route of a lazy feature. */
export const authChildGuard: CanActivateChildFn = (route, state) => authGuard(route, state);
