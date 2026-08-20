import type { AuthProviderConfig, AuthProviderKind } from '../models/auth-config.model';
import type { AuthTokens } from '../models/auth-tokens.model';
import type { User } from '../models/user.model';
import type { AuthSession } from '../auth.store';

/**
 * The contract every identity provider implements.
 *
 * This interface is the seam that keeps the application vendor-neutral: no page,
 * guard or component may import a concrete provider. Add a new identity source
 * by implementing this interface and registering it in the factory — see
 * `docs/adding-provider.md`.
 */

export interface LoginOptions {
  /** In-app URL to restore after a successful sign-in. */
  returnUrl?: string;
  /** Forces re-authentication or the account picker at the provider. */
  prompt?: 'none' | 'login' | 'consent' | 'select_account';
  /** Pre-fills the username field at the provider. */
  loginHint?: string;
}

export interface LogoutOptions {
  /** Clears the local session without redirecting to the provider. */
  localOnly?: boolean;
}

/** Query parameters of the redirect back from the identity provider. */
export interface CallbackParams {
  code?: string | null;
  state?: string | null;
  error?: string | null;
  error_description?: string | null;
  [key: string]: string | null | undefined;
}

export interface AuthProvider {
  readonly id: string;
  readonly kind: AuthProviderKind;
  readonly config: AuthProviderConfig;

  /** Redirects the browser to the provider's authorization endpoint. */
  login(options?: LoginOptions): Promise<void>;

  /** Completes the flow: validates `state`, exchanges the code, builds the user. */
  handleCallback(params: CallbackParams): Promise<AuthSession>;

  /** Ends the session locally and, unless `localOnly`, at the provider (RP-initiated). */
  logout(options?: LogoutOptions): Promise<void>;

  /** Exchanges the refresh token for a fresh token set. */
  refreshToken(tokens: AuthTokens): Promise<AuthTokens>;

  /** Current user, or `null` when the session is anonymous. */
  getUser(): User | null;

  isAuthenticated(): boolean;

  getAccessToken(): string | null;

  getIdToken(): string | null;

  /** Re-reads the profile from the provider (id_token claims + `/userinfo`). */
  loadProfile(tokens: AuthTokens): Promise<User>;
}
