import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../authentication/auth.service';
import { AUTH_CONFIG } from '../authentication/auth.tokens';

/** How the required roles are combined. `any` (default) or `all`. */
export type RoleMatch = 'any' | 'all';

export interface RoleGuardOptions {
  match?: RoleMatch;
}

/**
 * Requires one or more roles.
 *
 * Two equivalent styles — pick whichever reads better in the route file:
 *
 * ```ts
 * { path: 'admin', canActivate: [authGuard, roleGuard('admin')] }
 * { path: 'admin', canActivate: [authGuard, roleGuard()], data: { roles: ['admin'] } }
 * ```
 *
 * Requesting `all` roles instead of any:
 *
 * ```ts
 * roleGuard(['admin', 'auditor'], { match: 'all' })
 * ```
 *
 * The frontend hides what the user cannot use; the backend still enforces it.
 */
export function roleGuard(
  roles: string | readonly string[] = [],
  options: RoleGuardOptions = {},
): CanActivateFn {
  const declared = typeof roles === 'string' ? [roles] : roles;

  return (route) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const config = inject(AUTH_CONFIG);

    const fromRouteData = (route.data['roles'] as string[] | undefined) ?? [];
    const required = [...declared, ...fromRouteData];

    if (required.length === 0) {
      return true;
    }

    const match = options.match ?? (route.data['roleMatch'] as RoleMatch | undefined) ?? 'any';
    const allowed = match === 'all' ? auth.hasAllRoles(required) : auth.hasAnyRole(required);

    return allowed ? true : router.createUrlTree([config.routes.forbidden]);
  };
}
