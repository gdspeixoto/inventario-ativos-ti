import { readClaim } from '../internal/claims';
import type { ClaimMapping } from '../models/auth-config.model';
import type { UserClaims } from '../models/user.model';
import { BaseOidcProvider } from './base-oidc.provider';

/**
 * Keycloak.
 *
 * Two Keycloak-specific details are handled here:
 *
 *  - **Roles live in two places.** `realm_access.roles` holds realm-wide roles;
 *    `resource_access.<clientId>.roles` holds client roles. The client id is
 *    only known at runtime, so the mapping is built dynamically.
 *  - **Permissions are optional.** Keycloak Authorization Services publish them
 *    under `authorization.permissions[].scopes`; that array is flattened into a
 *    synthetic `permissions` claim before mapping.
 */
export class KeycloakProvider extends BaseOidcProvider {
  protected get defaultClaimMapping(): ClaimMapping {
    return {
      id: 'sub',
      name: 'name',
      username: 'preferred_username',
      email: 'email',
      picture: 'picture',
      roles: ['realm_access.roles', `resource_access.${this.config.clientId}.roles`],
      permissions: ['permissions', 'scope'],
    };
  }

  protected override normalizeClaims(claims: UserClaims): UserClaims {
    const permissions = flattenAuthorizationPermissions(claims);
    if (!permissions.length) {
      return claims;
    }
    const existing = Array.isArray(claims['permissions'])
      ? (claims['permissions'] as unknown[]).filter((v): v is string => typeof v === 'string')
      : [];
    return Object.freeze({
      ...claims,
      permissions: [...new Set([...existing, ...permissions])],
    });
  }
}

interface KeycloakPermission {
  rsname?: string;
  scopes?: string[];
}

/**
 * `authorization.permissions` is an array of `{ rsname, scopes }`.
 * Flattened as `resource:scope`, or just `resource` when no scope is declared.
 */
function flattenAuthorizationPermissions(claims: UserClaims): string[] {
  const raw = readClaim(claims, 'authorization.permissions');
  if (!Array.isArray(raw)) {
    return [];
  }
  return raw.flatMap((entry) => {
    const permission = entry as KeycloakPermission;
    if (!permission?.rsname) {
      return [];
    }
    if (!permission.scopes?.length) {
      return [permission.rsname];
    }
    return permission.scopes.map((scope) => `${permission.rsname}:${scope}`);
  });
}
