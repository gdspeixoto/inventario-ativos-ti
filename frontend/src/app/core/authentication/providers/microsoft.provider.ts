import type { ClaimMapping } from '../models/auth-config.model';
import type { UserClaims } from '../models/user.model';
import { BaseOidcProvider } from './base-oidc.provider';
import type { LoginOptions } from './auth-provider';

/**
 * Microsoft Entra ID (formerly Azure AD) and Azure AD B2C.
 *
 * Differences from a textbook OIDC server:
 *
 *  - The stable object identifier is `oid`, not `sub` — `sub` is pairwise and
 *    changes per application.
 *  - The username lives in `preferred_username` or `upn` depending on tenant
 *    configuration; B2C often only ships `emails[0]`.
 *  - App roles arrive in `roles`; group membership in `groups` (requires the
 *    optional claim to be enabled on the app registration).
 *  - Graph does not expose the avatar through `/userinfo`; the picture claim is
 *    absent by design, so the UI falls back to initials.
 *  - B2C selects the user journey with `p=<policy>`, handled by `OidcClient`
 *    from `config.userFlow`.
 */
export class MicrosoftProvider extends BaseOidcProvider {
  protected get defaultClaimMapping(): ClaimMapping {
    return {
      id: 'oid',
      name: 'name',
      username: 'preferred_username',
      email: 'email',
      picture: 'picture',
      roles: ['roles', 'groups', 'wids'],
      permissions: ['scp', 'permissions'],
    };
  }

  override login(options: LoginOptions = {}): Promise<void> {
    // Entra ID keeps a session cookie: without `select_account` a user with two
    // work accounts is silently signed back in as the wrong one.
    return super.login({ prompt: 'select_account', ...options });
  }

  protected override normalizeClaims(claims: UserClaims): UserClaims {
    const emails = claims['emails'];
    const primaryEmail = Array.isArray(emails)
      ? emails.find((entry): entry is string => typeof entry === 'string')
      : undefined;

    if (claims['email'] || !primaryEmail) {
      return claims;
    }
    // B2C tenants publish `emails` (array) instead of the standard `email`.
    return Object.freeze({ ...claims, email: primaryEmail });
  }
}
