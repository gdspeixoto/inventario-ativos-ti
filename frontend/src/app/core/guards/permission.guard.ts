import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../authentication/auth.service';
import { AUTH_CONFIG } from '../authentication/auth.tokens';

export interface PermissionGuardOptions {
  match?: 'any' | 'all';
}

/**
 * Requires one or more fine-grained permissions.
 *
 * Roles answer "who is this user"; permissions answer "what may they do".
 * Prefer permissions for feature-level access — they survive role renames and
 * map cleanly onto Keycloak Authorization Services scopes.
 *
 * ```ts
 * { path: 'reports', canActivate: [authGuard, permissionGuard('report:read')] }
 * { path: 'reports', canActivate: [authGuard, permissionGuard()],
 *   data: { permissions: ['report:read'] } }
 * ```
 */
export function permissionGuard(
  permissions: string | readonly string[] = [],
  options: PermissionGuardOptions = {},
): CanActivateFn {
  const declared = typeof permissions === 'string' ? [permissions] : permissions;

  return (route) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const config = inject(AUTH_CONFIG);

    const fromRouteData = (route.data['permissions'] as string[] | undefined) ?? [];
    const required = [...declared, ...fromRouteData];

    if (required.length === 0) {
      return true;
    }

    const match = options.match ?? 'any';
    const allowed =
      match === 'all'
        ? required.every((permission) => auth.hasPermission(permission))
        : auth.hasAnyPermission(required);

    return allowed ? true : router.createUrlTree([config.routes.forbidden]);
  };
}
