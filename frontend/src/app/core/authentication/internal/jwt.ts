import type { UserClaims } from '../models/user.model';

/**
 * Minimal JWT reader.
 *
 * The signature is deliberately NOT verified in the browser. With Authorization
 * Code + PKCE the id_token is fetched directly from the token endpoint over TLS,
 * so RFC 8725 §3.10 lets the client skip local signature validation; the access
 * token is opaque to the frontend and is validated by the resource server.
 * Everything decoded here is treated as untrusted display data — authorization
 * decisions still happen on the backend.
 */

export interface JwtHeader {
  alg: string;
  typ?: string;
  kid?: string;
}

export interface JwtPayload extends Record<string, unknown> {
  iss?: string;
  sub?: string;
  aud?: string | string[];
  exp?: number;
  iat?: number;
  nbf?: number;
  nonce?: string;
}

export interface DecodedJwt {
  header: JwtHeader;
  payload: JwtPayload;
}

export class InvalidTokenError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidTokenError';
  }
}

export function decodeJwt(token: string): DecodedJwt {
  const parts = token.split('.');
  if (parts.length < 2) {
    throw new InvalidTokenError('Malformed JWT: expected at least two segments.');
  }
  return {
    header: decodeSegment<JwtHeader>(parts[0]),
    payload: decodeSegment<JwtPayload>(parts[1]),
  };
}

/** Returns the payload, or `null` when the token cannot be parsed. */
export function tryDecodePayload(token: string | null | undefined): JwtPayload | null {
  if (!token) {
    return null;
  }
  try {
    return decodeJwt(token).payload;
  } catch {
    return null;
  }
}

/** Expiry as an epoch timestamp in milliseconds, or `null` when absent. */
export function expiryOf(token: string | null | undefined): number | null {
  const exp = tryDecodePayload(token)?.exp;
  return typeof exp === 'number' ? exp * 1000 : null;
}

export interface IdTokenValidation {
  issuer: string;
  clientId: string;
  nonce: string | null;
  clockSkewSeconds: number;
}

/**
 * Validates the id_token claims required by OpenID Connect Core §3.1.3.7
 * (issuer, audience, expiry and nonce replay protection).
 */
export function validateIdToken(payload: JwtPayload, options: IdTokenValidation): void {
  const { issuer, clientId, nonce, clockSkewSeconds } = options;
  const skewMs = clockSkewSeconds * 1000;
  const now = Date.now();

  if (payload.iss && normalizeIssuer(payload.iss) !== normalizeIssuer(issuer)) {
    throw new InvalidTokenError(`Unexpected id_token issuer: ${payload.iss}`);
  }

  const audiences = Array.isArray(payload.aud) ? payload.aud : [payload.aud];
  if (payload.aud !== undefined && !audiences.includes(clientId)) {
    throw new InvalidTokenError('id_token audience does not include this client.');
  }

  if (typeof payload.exp === 'number' && now > payload.exp * 1000 + skewMs) {
    throw new InvalidTokenError('id_token has expired.');
  }

  if (typeof payload.nbf === 'number' && now < payload.nbf * 1000 - skewMs) {
    throw new InvalidTokenError('id_token is not valid yet.');
  }

  if (nonce && payload.nonce !== nonce) {
    throw new InvalidTokenError('id_token nonce mismatch — possible replay attack.');
  }
}

export function claimsOf(payload: JwtPayload): UserClaims {
  return Object.freeze({ ...payload });
}

function normalizeIssuer(issuer: string): string {
  return issuer.replace(/\/+$/u, '');
}

function decodeSegment<T>(segment: string | undefined): T {
  if (!segment) {
    throw new InvalidTokenError('Malformed JWT: missing segment.');
  }
  const base64 = segment.replace(/-/gu, '+').replace(/_/gu, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  try {
    const binary = atob(padded);
    const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes)) as T;
  } catch (cause) {
    throw new InvalidTokenError(`Unable to decode JWT segment: ${String(cause)}`);
  }
}
