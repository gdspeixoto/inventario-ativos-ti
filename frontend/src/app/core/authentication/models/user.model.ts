/**
 * Canonical user model.
 *
 * Every provider normalizes its claims into this shape, so application code
 * (guards, directives, templates) never learns which identity provider is in
 * use. Provider-specific data stays reachable through `claims`.
 */

/** Arbitrary claim payload as returned by the identity provider. */
export type UserClaims = Readonly<Record<string, unknown>>;

export interface User {
  /** Subject identifier (`sub`). Stable and unique per provider. */
  readonly id: string;
  readonly name: string;
  readonly username: string;
  readonly email: string;
  /** Absolute URL to the avatar, or `null` when the provider has none. */
  readonly picture: string | null;
  readonly roles: readonly string[];
  readonly permissions: readonly string[];
  readonly claims: UserClaims;
  /** `AuthProviderConfig.id` that authenticated this session. */
  readonly provider: string;
}

/** Empty user used before the session is resolved. */
export const ANONYMOUS_USER: User = Object.freeze({
  id: '',
  name: '',
  username: '',
  email: '',
  picture: null,
  roles: Object.freeze([]),
  permissions: Object.freeze([]),
  claims: Object.freeze({}),
  provider: '',
});

export function hasRole(user: User | null, role: string): boolean {
  return !!user && user.roles.includes(role);
}

export function hasAnyRole(user: User | null, roles: readonly string[]): boolean {
  if (!user || roles.length === 0) {
    return true;
  }
  return roles.some((role) => user.roles.includes(role));
}

export function hasAllRoles(user: User | null, roles: readonly string[]): boolean {
  if (!user) {
    return false;
  }
  return roles.every((role) => user.roles.includes(role));
}

export function hasPermission(user: User | null, permission: string): boolean {
  return !!user && user.permissions.includes(permission);
}

export function hasAnyPermission(user: User | null, permissions: readonly string[]): boolean {
  if (!user || permissions.length === 0) {
    return true;
  }
  return permissions.some((permission) => user.permissions.includes(permission));
}

/** Best available display name, for avatars, menus and greetings. */
export function displayNameOf(user: User | null): string {
  return user?.name || user?.username || user?.email || '';
}
