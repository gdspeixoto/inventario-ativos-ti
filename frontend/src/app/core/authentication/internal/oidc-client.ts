import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { identityProviderContext } from '../../http/http-context.tokens';
import { AuthError } from '../auth.errors';
import type { AuthProviderConfig } from '../models/auth-config.model';
import type {
  AuthTokens,
  OidcDiscoveryDocument,
  TokenEndpointResponse,
} from '../models/auth-tokens.model';
import type { UserClaims } from '../models/user.model';
import { expiryOf } from './jwt';

const FORM_HEADERS = { 'Content-Type': 'application/x-www-form-urlencoded' };
/** Assumed lifetime when the provider omits `expires_in`. */
const DEFAULT_EXPIRES_IN_SECONDS = 300;

export interface AuthorizationUrlOptions {
  discovery: OidcDiscoveryDocument;
  config: AuthProviderConfig;
  state: string;
  nonce: string;
  codeChallenge: string;
  /** Forces the account picker / re-authentication. */
  prompt?: 'none' | 'login' | 'consent' | 'select_account';
  /** Pre-fills the username field at the identity provider. */
  loginHint?: string;
}

/**
 * Transport-level OIDC client: discovery, authorization URL, token exchange,
 * refresh, userinfo and RP-initiated logout.
 *
 * Stateless and provider-agnostic — every call takes the provider config
 * explicitly. Session state belongs to `AuthStore`, flow orchestration to the
 * provider classes.
 */
@Injectable({ providedIn: 'root' })
export class OidcClient {
  private readonly http = inject(HttpClient);
  /** Discovery documents rarely change; one fetch per issuer per page load. */
  private readonly discoveryCache = new Map<string, Promise<OidcDiscoveryDocument>>();

  discover(issuer: string): Promise<OidcDiscoveryDocument> {
    const cached = this.discoveryCache.get(issuer);
    if (cached) {
      return cached;
    }

    const url = `${issuer.replace(/\/+$/u, '')}/.well-known/openid-configuration`;
    const request = firstValueFrom(
      this.http.get<OidcDiscoveryDocument>(url, { context: identityProviderContext() }),
    ).catch((error: unknown) => {
      // Do not cache a failure: a transient outage must not break every retry.
      this.discoveryCache.delete(issuer);
      throw new AuthError(
        'discovery_failed',
        `Unable to load the OpenID configuration from ${url}.`,
        error,
      );
    });

    this.discoveryCache.set(issuer, request);
    return request;
  }

  buildAuthorizationUrl(options: AuthorizationUrlOptions): string {
    const { discovery, config, state, nonce, codeChallenge, prompt, loginHint } = options;

    const params = new URLSearchParams({
      response_type: config.responseType,
      client_id: config.clientId,
      redirect_uri: config.redirectUri,
      scope: config.scope,
      state,
      nonce,
      code_challenge: codeChallenge,
      code_challenge_method: 'S256',
      ...config.extraAuthorizationParams,
    });

    if (prompt) {
      params.set('prompt', prompt);
    }
    if (loginHint) {
      params.set('login_hint', loginHint);
    }
    // Azure AD B2C selects the user journey through a policy parameter.
    if (config.userFlow) {
      params.set('p', config.userFlow);
    }

    return `${discovery.authorization_endpoint}?${params.toString()}`;
  }

  async exchangeCode(
    discovery: OidcDiscoveryDocument,
    config: AuthProviderConfig,
    code: string,
    codeVerifier: string,
  ): Promise<AuthTokens> {
    const body = new HttpParams({
      fromObject: {
        grant_type: 'authorization_code',
        code,
        client_id: config.clientId,
        redirect_uri: config.redirectUri,
        code_verifier: codeVerifier,
      },
    });
    const response = await this.postToken(discovery, body, 'token_request_failed');
    return this.normalizeTokens(response, config.id);
  }

  async refresh(
    discovery: OidcDiscoveryDocument,
    config: AuthProviderConfig,
    refreshToken: string,
  ): Promise<AuthTokens> {
    const body = new HttpParams({
      fromObject: {
        grant_type: 'refresh_token',
        refresh_token: refreshToken,
        client_id: config.clientId,
        scope: config.scope,
      },
    });
    const response = await this.postToken(discovery, body, 'refresh_failed');
    // Providers that rotate refresh tokens omit the old one; keep it either way.
    return this.normalizeTokens({ refresh_token: refreshToken, ...response }, config.id);
  }

  async fetchUserInfo(
    discovery: OidcDiscoveryDocument,
    accessToken: string,
  ): Promise<UserClaims | null> {
    if (!discovery.userinfo_endpoint) {
      return null;
    }
    try {
      const claims = await firstValueFrom(
        this.http.get<Record<string, unknown>>(discovery.userinfo_endpoint, {
          headers: { Authorization: `Bearer ${accessToken}` },
          context: identityProviderContext(),
        }),
      );
      return Object.freeze(claims);
    } catch {
      // A missing or forbidden /userinfo must not break an otherwise valid login.
      return null;
    }
  }

  buildEndSessionUrl(
    discovery: OidcDiscoveryDocument,
    config: AuthProviderConfig,
    idToken: string | null,
  ): string | null {
    if (!discovery.end_session_endpoint) {
      return null;
    }
    const params = new URLSearchParams({
      post_logout_redirect_uri: config.postLogoutRedirectUri,
      client_id: config.clientId,
    });
    if (idToken) {
      params.set('id_token_hint', idToken);
    }
    return `${discovery.end_session_endpoint}?${params.toString()}`;
  }

  private async postToken(
    discovery: OidcDiscoveryDocument,
    body: HttpParams,
    errorCode: 'token_request_failed' | 'refresh_failed',
  ): Promise<TokenEndpointResponse> {
    try {
      return await firstValueFrom(
        this.http.post<TokenEndpointResponse>(discovery.token_endpoint, body.toString(), {
          headers: FORM_HEADERS,
          context: identityProviderContext(),
        }),
      );
    } catch (error) {
      throw new AuthError(errorCode, describeTokenError(error), error);
    }
  }

  private normalizeTokens(response: TokenEndpointResponse, providerId: string): AuthTokens {
    const now = Date.now();
    // Prefer the JWT's own `exp`: it is authoritative and immune to clock drift
    // between the moment the response left the server and the moment it arrived.
    const expiresAt =
      expiryOf(response.access_token) ??
      now + (response.expires_in ?? DEFAULT_EXPIRES_IN_SECONDS) * 1000;

    return Object.freeze({
      accessToken: response.access_token,
      idToken: response.id_token ?? null,
      refreshToken: response.refresh_token ?? null,
      tokenType: response.token_type || 'Bearer',
      expiresAt,
      refreshExpiresAt: response.refresh_expires_in
        ? now + response.refresh_expires_in * 1000
        : null,
      scope: response.scope ?? null,
      provider: providerId,
    });
  }
}

/** Surfaces the RFC 6749 `error_description` when the provider sends one. */
function describeTokenError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { error?: string; error_description?: string } | null;
    if (body?.error_description) {
      return body.error_description;
    }
    if (body?.error) {
      return body.error;
    }
    return `Token endpoint returned HTTP ${error.status}.`;
  }
  return 'The token request failed.';
}
