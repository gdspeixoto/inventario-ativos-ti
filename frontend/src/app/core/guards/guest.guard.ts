import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../authentication/auth.service';
import { AUTH_CONFIG } from '../authentication/auth.tokens';

/**
 * Blocks routes that only make sense while signed out (login, password reset).
 *
 * Without it, an authenticated user reaching `/login` would see a form that
 * immediately bounces them somewhere else.
 */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const config = inject(AUTH_CONFIG);

  if (!auth.isAuthenticated()) {
    return true;
  }
  return router.createUrlTree([config.routes.afterLogin]);
};
