/**
 * PKCE (RFC 7636) helpers.
 *
 * The template only ever performs Authorization Code + PKCE. The implicit flow
 * is not implemented, on purpose: it leaks tokens through the URL fragment and
 * is forbidden by OAuth 2.1.
 *
 * `crypto.subtle` requires a secure context, so the app must be served over
 * HTTPS (or `localhost`). We fail loudly instead of silently downgrading to the
 * `plain` challenge method.
 */

export interface PkcePair {
  readonly codeVerifier: string;
  readonly codeChallenge: string;
  readonly codeChallengeMethod: 'S256';
}

/** RFC 7636 §4.1 — verifier length must be between 43 and 128 characters. */
const VERIFIER_BYTES = 64;

export class InsecureContextError extends Error {
  constructor() {
    super(
      'Web Crypto is unavailable. Authentication with PKCE requires a secure ' +
        'context: serve the application over HTTPS or from localhost.',
    );
    this.name = 'InsecureContextError';
  }
}

export async function createPkcePair(crypto: Crypto | null): Promise<PkcePair> {
  const codeVerifier = createRandomString(crypto, VERIFIER_BYTES);
  const codeChallenge = await sha256Base64Url(crypto, codeVerifier);
  return { codeVerifier, codeChallenge, codeChallengeMethod: 'S256' };
}

/** Cryptographically secure, URL-safe random string (used for state and nonce). */
export function createRandomString(crypto: Crypto | null, byteLength = 32): string {
  if (!crypto?.getRandomValues) {
    throw new InsecureContextError();
  }
  const bytes = new Uint8Array(byteLength);
  crypto.getRandomValues(bytes);
  return base64UrlEncode(bytes);
}

async function sha256Base64Url(crypto: Crypto | null, value: string): Promise<string> {
  if (!crypto?.subtle) {
    throw new InsecureContextError();
  }
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(value));
  return base64UrlEncode(new Uint8Array(digest));
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = '';
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }
  return btoa(binary).replace(/\+/gu, '-').replace(/\//gu, '_').replace(/=+$/u, '');
}
