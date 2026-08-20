/**
 * Token set returned by the OIDC token endpoint, normalized for internal use.
 */

/** Raw response body of `POST {token_endpoint}`. */
export interface TokenEndpointResponse {
  access_token: string;
  token_type: string;
  expires_in?: number;
  refresh_token?: string;
  refresh_expires_in?: number;
  id_token?: string;
  scope?: string;
  session_state?: string;
}

export interface AuthTokens {
  readonly accessToken: string;
  readonly idToken: string | null;
  readonly refreshToken: string | null;
  readonly tokenType: string;
  /** Absolute expiry as an epoch timestamp in milliseconds. */
  readonly expiresAt: number;
  /** Absolute refresh-token expiry, when the provider reports one. */
  readonly refreshExpiresAt: number | null;
  readonly scope: string | null;
  /** `AuthProviderConfig.id` that issued this token set. */
  readonly provider: string;
}

/** OIDC discovery document, trimmed to the fields the client actually uses. */
export interface OidcDiscoveryDocument {
  issuer: string;
  authorization_endpoint: string;
  token_endpoint: string;
  userinfo_endpoint?: string;
  end_session_endpoint?: string;
  jwks_uri?: string;
  revocation_endpoint?: string;
  code_challenge_methods_supported?: string[];
  scopes_supported?: string[];
}

/** Transient state persisted between the authorize redirect and the callback. */
export interface AuthorizationRequestState {
  readonly providerId: string;
  readonly state: string;
  readonly nonce: string;
  readonly codeVerifier: string;
  readonly redirectUri: string;
  /** In-app URL to restore once the callback completes. */
  readonly returnUrl: string;
  readonly createdAt: number;
}

export function isExpired(tokens: AuthTokens | null, clockSkewSeconds = 0): boolean {
  if (!tokens) {
    return true;
  }
  return Date.now() >= tokens.expiresAt - clockSkewSeconds * 1000;
}

/** Milliseconds until expiry; never negative. */
export function timeUntilExpiry(tokens: AuthTokens | null): number {
  if (!tokens) {
    return 0;
  }
  return Math.max(0, tokens.expiresAt - Date.now());
}
