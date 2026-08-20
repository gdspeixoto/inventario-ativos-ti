import type { ClaimMapping } from '../models/auth-config.model';
import { BaseOidcProvider } from './base-oidc.provider';

/**
 * Any spec-compliant OpenID Connect provider (Auth0, Okta, Google, Ping, ...).
 *
 * Uses only standard claims. When a server publishes roles somewhere else,
 * point at it from `config/auth.config.ts` instead of subclassing:
 *
 * ```ts
 * claims: { roles: ['https://empresa.com.br/claims/roles'] }
 * ```
 */
export class GenericOidcProvider extends BaseOidcProvider {
  protected get defaultClaimMapping(): ClaimMapping {
    return {
      id: 'sub',
      name: 'name',
      username: 'preferred_username',
      email: 'email',
      picture: 'picture',
      roles: ['roles', 'groups'],
      permissions: ['permissions', 'scope'],
    };
  }
}
