import type { ClaimMapping } from '../models/auth-config.model';
import type { User, UserClaims } from '../models/user.model';

/**
 * Claim-to-`User` normalization.
 *
 * Identity providers disagree on everything: Keycloak nests roles under
 * `resource_access.<client>.roles`, Entra ID uses a flat `roles` array, generic
 * OIDC servers may use `groups`. A provider declares *where* to look through a
 * `ClaimMapping`; this module does the reading.
 */

/** Reads a dot-separated path, e.g. `resource_access.web-app.roles`. */
export function readClaim(claims: UserClaims, path: string): unknown {
  return path.split('.').reduce<unknown>((current, segment) => {
    if (current === null || current === undefined || typeof current !== 'object') {
      return undefined;
    }
    return (current as Record<string, unknown>)[segment];
  }, claims);
}

/** Reads the first path that resolves to a non-empty string. */
export function readStringClaim(
  claims: UserClaims,
  paths: readonly (string | undefined)[],
): string | null {
  for (const path of paths) {
    if (!path) {
      continue;
    }
    const value = readClaim(claims, path);
    if (typeof value === 'string' && value.trim().length > 0) {
      return value;
    }
  }
  return null;
}

/** Collects and de-duplicates string values found across several claim paths. */
export function readStringListClaim(
  claims: UserClaims,
  paths: readonly string[] | undefined,
): string[] {
  if (!paths?.length) {
    return [];
  }
  const collected = new Set<string>();
  for (const path of paths) {
    const value = readClaim(claims, path);
    if (typeof value === 'string') {
      // Some providers deliver a space- or comma-separated scope-like string.
      value
        .split(/[\s,]+/u)
        .filter(Boolean)
        .forEach((entry) => collected.add(entry));
    } else if (Array.isArray(value)) {
      value
        .filter((entry): entry is string => typeof entry === 'string')
        .forEach((entry) => collected.add(entry));
    }
  }
  return [...collected];
}

export interface BuildUserOptions {
  claims: UserClaims;
  mapping: ClaimMapping;
  providerId: string;
}

/** Builds the canonical `User` from raw claims plus a provider's mapping. */
export function buildUser({ claims, mapping, providerId }: BuildUserOptions): User {
  const id = readStringClaim(claims, [mapping.id, 'sub', 'oid', 'user_id']) ?? '';
  const username =
    readStringClaim(claims, [mapping.username, 'preferred_username', 'upn', 'email', 'sub']) ?? '';
  const email =
    readStringClaim(claims, [mapping.email, 'email', 'upn', 'preferred_username']) ?? '';
  const name = readStringClaim(claims, [mapping.name, 'name', 'given_name']) ?? (username || email);

  return Object.freeze({
    id,
    name,
    username,
    email,
    picture: readStringClaim(claims, [mapping.picture, 'picture', 'avatar_url']),
    roles: Object.freeze(readStringListClaim(claims, mapping.roles)),
    permissions: Object.freeze(readStringListClaim(claims, mapping.permissions)),
    claims,
    provider: providerId,
  });
}

/** Later sources win, so `/userinfo` can refine what the id_token carried. */
export function mergeClaims(...sources: (UserClaims | null | undefined)[]): UserClaims {
  return Object.freeze(Object.assign({}, ...sources.filter(Boolean)) as UserClaims);
}
