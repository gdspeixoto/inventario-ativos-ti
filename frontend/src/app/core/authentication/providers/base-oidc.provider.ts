import type { PlatformService } from '../../services/platform.service';
import { AuthError } from '../auth.errors';
import type { AuthSession, AuthStore } from '../auth.store';
import { buildUser, mergeClaims } from '../internal/claims';
import { claimsOf, tryDecodePayload, validateIdToken } from '../internal/jwt';
import type { AuthStorage } from '../internal/auth-storage';
import type { OidcClient } from '../internal/oidc-client';
import { InsecureContextError, createPkcePair, createRandomString } from '../internal/pkce';
import type {
  AuthConfig,
  AuthProviderConfig,
  AuthProviderKind,
  ClaimMapping,
} from '../models/auth-config.model';
import type { AuthTokens, OidcDiscoveryDocument } from '../models/auth-tokens.model';
import type { User, UserClaims } from '../models/user.model';
import type { AuthProvider, CallbackParams, LoginOptions, LogoutOptions } from './auth-provider';

/** Collaborators handed to a provider at construction time by the factory. */
export interface OidcProviderDeps {
  readonly client: OidcClient;
  readonly storage: AuthStorage;
  readonly store: AuthStore;
  readonly platform: PlatformService;
  readonly authConfig: AuthConfig;
}

/** A pending request state is only valid for a few minutes. */
const REQUEST_STATE_TTL_MS = 10 * 60 * 1000;

/**
 * Authorization Code + PKCE, implemented once.
 *
 * Every shipped provider extends this class and overrides only what genuinely
 * differs — in practice just the claim mapping and, for Entra ID, a couple of
 * authorization parameters. Keeping the flow in one place means a security fix
 * lands for all providers at once.
 *
 * These are plain classes, not `@Injectable`s: several instances may coexist
 * (one per configured provider), and they must be unit-testable without TestBed.
 */
export abstract class BaseOidcProvider implements AuthProvider {
  constructor(
    readonly config: AuthProviderConfig,
    protected readonly deps: OidcProviderDeps,
  ) {}

  get id(): string {
    return this.config.id;
  }

  get kind(): AuthProviderKind {
    return this.config.kind;
  }

  /** Where this provider publishes roles, names and permissions by default. */
  protected abstract get defaultClaimMapping(): ClaimMapping;

  /** Explicit configuration always wins over the provider's defaults. */
  protected get claimMapping(): ClaimMapping {
    return { ...this.defaultClaimMapping, ...this.config.claims };
  }

  // ------------------------------------------------------------- Flow: login

  async login(options: LoginOptions = {}): Promise<void> {
    try {
      const discovery = await this.discover();
      const pkce = await createPkcePair(this.deps.platform.crypto);
      const state = createRandomString(this.deps.platform.crypto);
      const nonce = createRandomString(this.deps.platform.crypto);

      this.deps.storage.writeRequestState({
        providerId: this.id,
        state,
        nonce,
        codeVerifier: pkce.codeVerifier,
        redirectUri: this.config.redirectUri,
        returnUrl: options.returnUrl ?? this.deps.authConfig.routes.afterLogin,
        createdAt: Date.now(),
      });
      this.deps.storage.writeActiveProviderId(this.id);

      const url = this.deps.client.buildAuthorizationUrl({
        discovery,
        config: this.config,
        state,
        nonce,
        codeChallenge: pkce.codeChallenge,
        prompt: options.prompt,
        loginHint: options.loginHint,
      });

      this.deps.platform.navigateTo(url);
    } catch (error) {
      if (error instanceof InsecureContextError) {
        throw new AuthError('insecure_context', error.message, error);
      }
      throw AuthError.from(error, 'authorization_failed');
    }
  }

  // ---------------------------------------------------------- Flow: callback

  async handleCallback(params: CallbackParams): Promise<AuthSession> {
    if (params.error) {
      this.deps.storage.clearRequestState();
      throw new AuthError(
        'authorization_failed',
        params.error_description || `The identity provider returned "${params.error}".`,
      );
    }

    const request = this.deps.storage.readRequestState();
    if (!request) {
      throw new AuthError(
        'missing_request_state',
        'No pending authentication request. Start the sign-in again.',
      );
    }

    // Single-use: consume it before any await so a double callback cannot replay.
    this.deps.storage.clearRequestState();

    if (Date.now() - request.createdAt > REQUEST_STATE_TTL_MS) {
      throw new AuthError('missing_request_state', 'The authentication request has expired.');
    }
    if (!params.state || params.state !== request.state) {
      throw new AuthError('state_mismatch', 'State parameter mismatch — request rejected.');
    }
    if (!params.code) {
      throw new AuthError(
        'authorization_failed',
        'The provider did not return an authorization code.',
      );
    }

    const discovery = await this.discover();
    const tokens = await this.deps.client.exchangeCode(
      discovery,
      this.config,
      params.code,
      request.codeVerifier,
    );

    this.validateIdTokenOf(tokens, request.nonce);
    const user = await this.loadProfile(tokens);

    return { user, tokens };
  }

  // ----------------------------------------------------------- Flow: refresh

  async refreshToken(tokens: AuthTokens): Promise<AuthTokens> {
    if (!tokens.refreshToken) {
      throw new AuthError('refresh_failed', 'No refresh token available for this session.');
    }
    const discovery = await this.discover();
    const refreshed = await this.deps.client.refresh(discovery, this.config, tokens.refreshToken);
    this.validateIdTokenOf(refreshed, null);
    return refreshed;
  }

  // ------------------------------------------------------------ Flow: logout

  async logout(options: LogoutOptions = {}): Promise<void> {
    const idToken = this.deps.store.idToken();
    this.deps.storage.clearAll();

    if (options.localOnly) {
      return;
    }

    try {
      const discovery = await this.discover();
      const url = this.deps.client.buildEndSessionUrl(discovery, this.config, idToken);
      if (url) {
        this.deps.platform.navigateTo(url);
        return;
      }
    } catch {
      // Discovery may fail while the provider is down; a local logout still must work.
    }
    this.deps.platform.navigateTo(this.config.postLogoutRedirectUri);
  }

  // ----------------------------------------------------------------- Profile

  async loadProfile(tokens: AuthTokens): Promise<User> {
    const idClaims = tokens.idToken ? claimsOf(tryDecodePayload(tokens.idToken) ?? {}) : {};
    const accessClaims = claimsOf(tryDecodePayload(tokens.accessToken) ?? {});

    let userInfoClaims: UserClaims | null = null;
    if (this.deps.authConfig.loadUserInfo) {
      const discovery = await this.discover();
      userInfoClaims = await this.deps.client.fetchUserInfo(discovery, tokens.accessToken);
    }

    // Precedence: access token (roles live there for most providers) < id_token
    // (identity) < userinfo (freshest profile data).
    const claims = this.normalizeClaims(mergeClaims(accessClaims, idClaims, userInfoClaims));

    return buildUser({ claims, mapping: this.claimMapping, providerId: this.id });
  }

  /** Hook for providers that need to reshape claims before mapping. */
  protected normalizeClaims(claims: UserClaims): UserClaims {
    return claims;
  }

  // ------------------------------------------------------- Session accessors

  getUser(): User | null {
    return this.deps.store.user();
  }

  isAuthenticated(): boolean {
    return this.deps.store.isAuthenticated();
  }

  getAccessToken(): string | null {
    return this.deps.store.accessToken();
  }

  getIdToken(): string | null {
    return this.deps.store.idToken();
  }

  // ------------------------------------------------------------------ Shared

  protected discover(): Promise<OidcDiscoveryDocument> {
    return this.deps.client.discover(this.config.issuer);
  }

  private validateIdTokenOf(tokens: AuthTokens, nonce: string | null): void {
    if (!tokens.idToken) {
      return;
    }
    const payload = tryDecodePayload(tokens.idToken);
    if (!payload) {
      throw new AuthError('invalid_id_token', 'The id_token could not be decoded.');
    }
    try {
      validateIdToken(payload, {
        issuer: this.config.issuer,
        clientId: this.config.clientId,
        nonce,
        clockSkewSeconds: this.deps.authConfig.clockSkewSeconds,
      });
    } catch (error) {
      throw AuthError.from(error, 'invalid_id_token');
    }
  }
}
