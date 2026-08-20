import { HttpContext, HttpContextToken } from '@angular/common/http';

/**
 * Per-request flags read by the interceptor chain.
 *
 * `HttpContext` keeps cross-cutting concerns opt-out-able without leaking
 * conditionals into the interceptors' URL matching.
 */

/** Skips `authInterceptor`: no Bearer token, no 401 refresh attempt. */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

/** Skips `errorInterceptor`'s automatic toast — the caller handles the error. */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

/** Skips `apiUrlInterceptor`: the URL is used verbatim. */
export const SKIP_API_PREFIX = new HttpContextToken<boolean>(() => false);

/** Skips the global loading indicator (useful for polling/autocomplete). */
export const SKIP_LOADING = new HttpContextToken<boolean>(() => false);

/** Number of retries on network/5xx failures. */
export const RETRY_COUNT = new HttpContextToken<number>(() => 0);

/** Context used for identity-provider calls: raw URL, no token, no toast. */
export function identityProviderContext(): HttpContext {
  return new HttpContext()
    .set(SKIP_AUTH, true)
    .set(SKIP_API_PREFIX, true)
    .set(SKIP_ERROR_TOAST, true)
    .set(SKIP_LOADING, true);
}
