import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LoggerService } from '../services/logger.service';
import { AUTH_CONFIG } from './auth.tokens';
import { AuthError } from './auth.errors';
import { AuthStore, type AuthSession } from './auth.store';
import { AuthStorage } from './internal/auth-storage';
import { claimsOf, tryDecodePayload } from './internal/jwt';
import { isExpired, timeUntilExpiry, type AuthTokens } from './models/auth-tokens.model';
import type { TokenEndpointResponse } from './models/auth-tokens.model';
import { hasAllRoles, hasAnyPermission, hasAnyRole, type User } from './models/user.model';
import { AuthProviderRegistry } from './providers/auth-provider.factory';
import type { CallbackParams, LoginOptions, LogoutOptions } from './providers/auth-provider';

/** Refresh is retried this many times before the session is dropped. */
const MAX_REFRESH_ATTEMPTS = 2;

/**
 * Origem das sessoes abertas por usuario e senha.
 *
 * Nao corresponde a nenhum provedor do registry, e isso e proposital: essas
 * sessoes nao tem discovery, refresh nem `/userinfo`.
 */
const CREDENTIALS_PROVIDER_ID = 'credentials';

/**
 * The application's only entry point into authentication.
 *
 * Pages, guards and interceptors talk to this service; the provider classes,
 * the OIDC client and the token storage stay internal. Swapping Keycloak for
 * Entra ID changes `config/auth.config.ts` and nothing else.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly config = inject(AUTH_CONFIG);
  private readonly registry = inject(AuthProviderRegistry);
  private readonly store = inject(AuthStore);
  private readonly storage = inject(AuthStorage);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly logger = inject(LoggerService).forContext('AuthService');
  private readonly destroyRef = inject(DestroyRef);

  private refreshTimer: ReturnType<typeof setTimeout> | null = null;
  /** De-duplicates concurrent refreshes triggered by parallel 401s. */
  private inFlightRefresh: Promise<AuthTokens> | null = null;

  // --------------------------------------------------------------- Read model

  readonly status = this.store.status;
  readonly user = this.store.currentUser;
  readonly isAuthenticated = this.store.isAuthenticated;
  readonly isResolved = this.store.isResolved;
  readonly isBusy = this.store.isBusy;
  readonly error = this.store.error;
  readonly roles = this.store.roles;
  readonly permissions = this.store.permissions;
  readonly providerId = this.store.providerId;

  readonly availableProviders = this.registry.enabledConfigs;
  readonly hasSingleProvider = this.registry.hasSingleProvider;
  readonly isCredentialsLoginEnabled = this.config.credentials.enabled;
  readonly showProviderSeparator = computed(
    () => this.isCredentialsLoginEnabled && this.availableProviders().length > 0,
  );

  constructor() {
    this.destroyRef.onDestroy(() => this.cancelRefreshTimer());
  }

  // ------------------------------------------------------------ Bootstrapping

  /**
   * Restores the session on application start-up.
   *
   * Called once by `provideAppInitializer`, before the router activates any
   * route, so guards never see an `unknown` status.
   */
  async restoreSession(): Promise<void> {
    const tokens = this.storage.readTokens();

    /*
     * A sessao por credencial local nao tem provedor OIDC por tras: o perfil
     * sai do proprio token e nao ha refresh. Quando o token expira, resta
     * autenticar de novo — que e o comportamento desejado para um acesso de
     * emergencia, deliberadamente curto.
     */
    if (tokens?.provider === CREDENTIALS_PROVIDER_ID) {
      if (isExpired(tokens, this.config.clockSkewSeconds)) {
        this.clearSession();
        return;
      }

      const username =
        (tryDecodePayload(tokens.accessToken)?.['preferred_username'] as string | undefined) ?? '';

      this.applySession({ user: this.minimalUserFrom(username, tokens), tokens });
      return;
    }

    const provider = this.registry.tryGet(tokens?.provider ?? this.storage.readActiveProviderId());

    if (!tokens || !provider) {
      this.store.setAnonymous();
      return;
    }

    try {
      const valid = isExpired(tokens, this.config.clockSkewSeconds)
        ? await provider.refreshToken(tokens)
        : tokens;
      const user = await provider.loadProfile(valid);
      this.applySession({ user, tokens: valid });
    } catch (error) {
      this.logger.warn('Could not restore the previous session.', error);
      this.clearSession();
    }
  }

  // -------------------------------------------------------------------- Login

  /** Redirects to the identity provider. Resolves only if the redirect fails. */
  async login(providerId?: string, options: LoginOptions = {}): Promise<void> {
    const provider = providerId ? this.registry.get(providerId) : this.registry.getDefault();
    this.store.startAuthentication();
    try {
      await provider.login({
        returnUrl: options.returnUrl ?? this.store.returnUrl() ?? undefined,
        ...options,
      });
    } catch (error) {
      const authError = AuthError.from(error, 'authorization_failed');
      this.store.setError(authError);
      throw authError;
    }
  }

  /**
   * Email + password sign-in against the application's own backend.
   *
   * Only meaningful when the backend owns the credentials (or brokers them to
   * the identity provider through a confidential client). Browsers must never
   * hold a client secret, so the Resource Owner Password grant is not called
   * from here — see `docs/authentication.md`.
   */
  async loginWithCredentials(username: string, password: string): Promise<void> {
    if (!this.config.credentials.enabled) {
      throw new AuthError('invalid_credentials', 'Credentials sign-in is disabled.');
    }

    this.store.startAuthentication();
    try {
      const response = await firstValueFrom(
        this.http.post<TokenEndpointResponse>(this.config.credentials.endpoint, {
          username,
          password,
        }),
      );
      const tokens = this.tokensFromCredentialsResponse(response);

      /*
       * O perfil vem do proprio token, e nao de um provedor OIDC: a credencial
       * local nao existe no provedor corporativo, e pedir o perfil a ele
       * falharia — ou, pior, devolveria os dados de outra sessao.
       */
      const user = this.minimalUserFrom(username, tokens);

      this.applySession({ user, tokens });
      await this.router.navigateByUrl(this.consumeReturnUrl());
    } catch (error) {
      const authError = AuthError.from(error, 'invalid_credentials');
      this.store.setError(authError);
      throw authError;
    }
  }

  // ----------------------------------------------------------------- Callback

  /**
   * Completes the redirect flow and navigates to the originally requested route.
   * Called by the callback page.
   */
  async completeLogin(params: CallbackParams): Promise<void> {
    this.store.startAuthentication();
    const providerId = params['provider'] ?? this.storage.readRequestState()?.providerId;
    const returnUrl = this.storage.readRequestState()?.returnUrl;

    try {
      const provider = this.registry.tryGet(providerId) ?? this.registry.getDefault();
      const session = await provider.handleCallback(params);
      this.applySession(session);
      await this.router.navigateByUrl(returnUrl ?? this.consumeReturnUrl());
    } catch (error) {
      const authError = AuthError.from(error, 'authorization_failed');
      this.logger.error('Sign-in failed.', authError);
      this.clearSession();
      this.store.setError(authError);
      await this.router.navigate([this.config.routes.login], {
        queryParams: { error: authError.code },
      });
    }
  }

  // ------------------------------------------------------------------- Logout

  async logout(options: LogoutOptions = {}): Promise<void> {
    const provider = this.registry.tryGet(this.store.providerId());
    this.clearSession();

    if (!provider || options.localOnly) {
      await this.router.navigateByUrl(this.config.routes.afterLogout);
      return;
    }
    await provider.logout(options);
  }

  // ------------------------------------------------------------------ Tokens

  getAccessToken(): string | null {
    return this.store.accessToken();
  }

  getIdToken(): string | null {
    return this.store.idToken();
  }

  /** True when the access token is missing or within the clock-skew window. */
  isAccessTokenExpired(): boolean {
    return isExpired(this.store.tokens(), this.config.clockSkewSeconds);
  }

  /**
   * Returns a valid access token, refreshing it if needed.
   * Concurrent callers share a single refresh round-trip.
   */
  async getValidAccessToken(): Promise<string | null> {
    const tokens = this.store.tokens();
    if (!tokens) {
      return null;
    }
    if (!isExpired(tokens, this.config.clockSkewSeconds)) {
      return tokens.accessToken;
    }
    const refreshed = await this.refresh();
    return refreshed?.accessToken ?? null;
  }

  /** Refreshes the session. Returns `null` and signs the user out on failure. */
  async refresh(): Promise<AuthTokens | null> {
    if (this.inFlightRefresh) {
      return this.inFlightRefresh.catch(() => null);
    }

    const tokens = this.store.tokens();
    const provider = this.registry.tryGet(tokens?.provider);
    if (!tokens || !provider) {
      return null;
    }

    this.inFlightRefresh = this.refreshWithRetry(provider.refreshToken.bind(provider), tokens);
    try {
      const refreshed = await this.inFlightRefresh;
      this.store.setTokens(refreshed);
      this.storage.writeTokens(refreshed);
      this.scheduleSilentRefresh(refreshed);
      return refreshed;
    } catch (error) {
      this.logger.warn('Token refresh failed — ending the session.', error);
      await this.forceLogout();
      return null;
    } finally {
      this.inFlightRefresh = null;
    }
  }

  /** Drops the session and sends the user back to the login screen. */
  async forceLogout(reason?: string): Promise<void> {
    if (reason) {
      this.logger.info(`Session ended: ${reason}`);
    }
    this.clearSession();
    this.store.setReturnUrl(this.router.url);
    await this.router.navigate([this.config.routes.login]);
  }

  // ------------------------------------------------------------ Authorization

  hasRole(role: string): boolean {
    return this.store.roles().includes(role);
  }

  hasAnyRole(roles: readonly string[]): boolean {
    return hasAnyRole(this.store.user(), roles);
  }

  hasAllRoles(roles: readonly string[]): boolean {
    return hasAllRoles(this.store.user(), roles);
  }

  hasPermission(permission: string): boolean {
    return this.store.permissions().includes(permission);
  }

  hasAnyPermission(permissions: readonly string[]): boolean {
    return hasAnyPermission(this.store.user(), permissions);
  }

  /** Remembers where the user was heading before the guard redirected them. */
  setReturnUrl(url: string | null): void {
    this.store.setReturnUrl(url);
  }

  // ------------------------------------------------------------------ Private

  private applySession(session: AuthSession): void {
    this.store.setSession(session);
    this.storage.writeTokens(session.tokens);
    this.storage.writeActiveProviderId(session.tokens.provider || session.user.provider);
    this.scheduleSilentRefresh(session.tokens);
  }

  private clearSession(): void {
    this.cancelRefreshTimer();
    this.storage.clearAll();
    this.store.setAnonymous();
  }

  private consumeReturnUrl(): string {
    return this.store.consumeReturnUrl() ?? this.config.routes.afterLogin;
  }

  /**
   * Schedules a refresh shortly before expiry so the user never sees a 401.
   * A fresh timer replaces the previous one on every token update.
   */
  private scheduleSilentRefresh(tokens: AuthTokens): void {
    this.cancelRefreshTimer();
    if (!tokens.refreshToken) {
      return;
    }

    const leadTimeMs = this.config.refreshLeewaySeconds * 1000;
    // Never schedule at 0: a pathologically short token would spin the loop.
    const delay = Math.max(5_000, timeUntilExpiry(tokens) - leadTimeMs);
    this.refreshTimer = setTimeout(() => void this.refresh(), delay);
  }

  private cancelRefreshTimer(): void {
    if (this.refreshTimer !== null) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  private async refreshWithRetry(
    refreshFn: (tokens: AuthTokens) => Promise<AuthTokens>,
    tokens: AuthTokens,
  ): Promise<AuthTokens> {
    let lastError: unknown;
    for (let attempt = 1; attempt <= MAX_REFRESH_ATTEMPTS; attempt++) {
      try {
        return await refreshFn(tokens);
      } catch (error) {
        lastError = error;
        // A rejected refresh token will never succeed; only retry transport errors.
        if (error instanceof AuthError && error.code !== 'discovery_failed') {
          break;
        }
      }
    }
    throw AuthError.from(lastError, 'refresh_failed');
  }

  private tokensFromCredentialsResponse(response: TokenEndpointResponse): AuthTokens {
    const expiresIn = response.expires_in ?? 3600;

    return Object.freeze({
      accessToken: response.access_token,
      idToken: response.id_token ?? null,
      refreshToken: response.refresh_token ?? null,
      tokenType: response.token_type || 'Bearer',
      expiresAt: Date.now() + expiresIn * 1000,
      refreshExpiresAt: null,
      scope: response.scope ?? null,

      /*
       * Marca a origem como `credentials`, e nao como o provedor padrao. O
       * `restoreSession` usa este campo para escolher quem renova a sessao:
       * apontar para o Keycloak faria o refresh de um token que ele nunca
       * emitiu — a sessao morreria no primeiro recarregamento da pagina.
       */
      provider: CREDENTIALS_PROVIDER_ID,
    });
  }

  /**
   * Perfil montado a partir das claims do proprio token.
   *
   * O login por credenciais nao tem `/userinfo` para consultar; os papeis vem
   * no access token e precisam chegar ao `AuthStore`, senao a interface esconde
   * tudo de quem, no servidor, tem acesso a tudo.
   */
  private minimalUserFrom(username: string, tokens: AuthTokens): User {
    const claims = claimsOf(tryDecodePayload(tokens.accessToken) ?? {});

    const roles = Array.isArray(claims['role'])
      ? (claims['role'] as string[])
      : typeof claims['role'] === 'string'
        ? [claims['role'] as string]
        : [];

    const name = typeof claims['name'] === 'string' ? claims['name'] : username;
    const email = typeof claims['email'] === 'string' ? claims['email'] : '';

    return Object.freeze({
      id: typeof claims['sub'] === 'string' ? claims['sub'] : username,
      name,
      username,
      email,
      picture: null,
      roles: Object.freeze(roles),
      permissions: Object.freeze([]),
      claims,
      provider: tokens.provider,
    });
  }
}
