/**
 * Authentication error taxonomy.
 *
 * A single error type with a discriminating `code` keeps `catch` blocks simple
 * while still letting the UI react differently per failure mode.
 */

export type AuthErrorCode =
  /** No provider matches the requested id. */
  | 'provider_not_found'
  /** The provider is declared but `enabled: false`. */
  | 'provider_disabled'
  /** `.well-known/openid-configuration` could not be fetched. */
  | 'discovery_failed'
  /** The identity provider returned `error=` on the callback. */
  | 'authorization_failed'
  /** `state` did not match the value stored before the redirect (CSRF). */
  | 'state_mismatch'
  /** No pending authorization request — usually a reloaded callback URL. */
  | 'missing_request_state'
  /** The token endpoint rejected the code or the refresh token. */
  | 'token_request_failed'
  /** The id_token failed claim validation. */
  | 'invalid_id_token'
  /** No refresh token available, or the refresh was rejected. */
  | 'refresh_failed'
  /** Email/password sign-in was rejected by the backend. */
  | 'invalid_credentials'
  /** Web Crypto unavailable (insecure context). */
  | 'insecure_context'
  /** Anything unclassified. */
  | 'unknown';

export class AuthError extends Error {
  constructor(
    readonly code: AuthErrorCode,
    message: string,
    override readonly cause?: unknown,
  ) {
    super(message);
    this.name = 'AuthError';
  }

  /** i18n key resolved by the login screen to show a friendly message. */
  get translationKey(): string {
    return `auth.errors.${this.code}`;
  }

  static from(error: unknown, fallbackCode: AuthErrorCode = 'unknown'): AuthError {
    if (error instanceof AuthError) {
      return error;
    }
    const message = error instanceof Error ? error.message : String(error);
    return new AuthError(fallbackCode, message, error);
  }
}
