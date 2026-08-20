import { Injectable, computed, signal } from '@angular/core';
import type { AuthError } from './auth.errors';
import type { AuthTokens } from './models/auth-tokens.model';
import { ANONYMOUS_USER, type User } from './models/user.model';

/** Lifecycle of the session, as observed by the UI. */
export type AuthStatus =
  /** Before the initializer has run — render a splash, never a login form. */
  | 'unknown'
  /** A redirect or token exchange is in flight. */
  | 'authenticating'
  | 'authenticated'
  | 'anonymous';

export interface AuthSession {
  readonly user: User;
  readonly tokens: AuthTokens;
}

/**
 * Session state.
 *
 * The single source of truth for "who is signed in". Guards, interceptors,
 * directives and templates all read from here; only `AuthService` writes.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly _status = signal<AuthStatus>('unknown');
  private readonly _user = signal<User | null>(null);
  private readonly _tokens = signal<AuthTokens | null>(null);
  private readonly _error = signal<AuthError | null>(null);
  /** Route to return to once authentication completes. */
  private readonly _returnUrl = signal<string | null>(null);

  readonly status = this._status.asReadonly();
  readonly user = this._user.asReadonly();
  readonly tokens = this._tokens.asReadonly();
  readonly error = this._error.asReadonly();
  readonly returnUrl = this._returnUrl.asReadonly();

  readonly isAuthenticated = computed(() => this._status() === 'authenticated');
  readonly isResolved = computed(() => this._status() !== 'unknown');
  readonly isBusy = computed(() => this._status() === 'authenticating');

  /** Never null, so templates can bind without optional chaining everywhere. */
  readonly currentUser = computed<User>(() => this._user() ?? ANONYMOUS_USER);
  readonly accessToken = computed(() => this._tokens()?.accessToken ?? null);
  readonly idToken = computed(() => this._tokens()?.idToken ?? null);
  readonly refreshToken = computed(() => this._tokens()?.refreshToken ?? null);
  readonly expiresAt = computed(() => this._tokens()?.expiresAt ?? null);
  readonly roles = computed<readonly string[]>(() => this.currentUser().roles);
  readonly permissions = computed<readonly string[]>(() => this.currentUser().permissions);
  readonly providerId = computed(() => this._user()?.provider ?? this._tokens()?.provider ?? null);

  startAuthentication(): void {
    this._status.set('authenticating');
    this._error.set(null);
  }

  setSession(session: AuthSession): void {
    this._user.set(session.user);
    this._tokens.set(session.tokens);
    this._error.set(null);
    this._status.set('authenticated');
  }

  /** Refreshes the token set without touching the profile. */
  setTokens(tokens: AuthTokens): void {
    this._tokens.set(tokens);
  }

  setUser(user: User): void {
    this._user.set(user);
  }

  setAnonymous(): void {
    this._user.set(null);
    this._tokens.set(null);
    this._status.set('anonymous');
  }

  setError(error: AuthError): void {
    this._error.set(error);
    this._status.set('anonymous');
  }

  clearError(): void {
    this._error.set(null);
  }

  setReturnUrl(url: string | null): void {
    this._returnUrl.set(url);
  }

  /** Reads and clears in one step — the return URL is single-use. */
  consumeReturnUrl(): string | null {
    const url = this._returnUrl();
    this._returnUrl.set(null);
    return url;
  }
}
