import { describe, expect, it } from 'vitest';
import { InvalidTokenError, decodeJwt, expiryOf, tryDecodePayload, validateIdToken } from './jwt';

/**
 * Builds an unsigned JWT — enough for the decoder, which never verifies
 * signatures. Segments are UTF-8 bytes in base64url, exactly as RFC 7519
 * requires: `btoa` alone would throw on any accented character.
 */
function makeToken(payload: Record<string, unknown>): string {
  const encode = (value: unknown) => {
    const bytes = new TextEncoder().encode(JSON.stringify(value));
    const binary = String.fromCharCode(...bytes);
    return btoa(binary).replace(/\+/gu, '-').replace(/\//gu, '_').replace(/=+$/u, '');
  };
  return `${encode({ alg: 'RS256', typ: 'JWT' })}.${encode(payload)}.signature`;
}

describe('decodeJwt', () => {
  it('decodes header and payload', () => {
    const token = makeToken({ sub: '123', name: 'Ana' });
    const { header, payload } = decodeJwt(token);

    expect(header.alg).toBe('RS256');
    expect(payload.sub).toBe('123');
  });

  it('handles non-ASCII claims', () => {
    const payload = tryDecodePayload(makeToken({ name: 'João Gonçalves' }));
    expect(payload?.['name']).toBe('João Gonçalves');
  });

  it('rejects a malformed token', () => {
    expect(() => decodeJwt('not-a-jwt')).toThrow(InvalidTokenError);
  });

  it('returns null instead of throwing in the safe variant', () => {
    expect(tryDecodePayload('garbage')).toBeNull();
    expect(tryDecodePayload(null)).toBeNull();
  });
});

describe('expiryOf', () => {
  it('converts the exp claim to milliseconds', () => {
    const token = makeToken({ exp: 1_700_000_000 });
    expect(expiryOf(token)).toBe(1_700_000_000_000);
  });

  it('returns null when exp is absent', () => {
    expect(expiryOf(makeToken({ sub: 'x' }))).toBeNull();
  });
});

describe('validateIdToken', () => {
  const base = {
    issuer: 'https://idp.example.com/realms/corp',
    clientId: 'web-app',
    nonce: null,
    clockSkewSeconds: 30,
  };
  const future = Math.floor(Date.now() / 1000) + 600;

  it('accepts a well-formed token', () => {
    expect(() =>
      validateIdToken({ iss: base.issuer, aud: 'web-app', exp: future }, base),
    ).not.toThrow();
  });

  it('ignores a trailing slash on the issuer', () => {
    expect(() =>
      validateIdToken({ iss: `${base.issuer}/`, aud: 'web-app', exp: future }, base),
    ).not.toThrow();
  });

  it('rejects a foreign issuer', () => {
    expect(() =>
      validateIdToken({ iss: 'https://evil.example.com', aud: 'web-app', exp: future }, base),
    ).toThrow(/issuer/iu);
  });

  it('rejects an audience that excludes this client', () => {
    expect(() =>
      validateIdToken({ iss: base.issuer, aud: ['other-app'], exp: future }, base),
    ).toThrow(/audience/iu);
  });

  it('rejects an expired token', () => {
    const past = Math.floor(Date.now() / 1000) - 600;
    expect(() => validateIdToken({ iss: base.issuer, aud: 'web-app', exp: past }, base)).toThrow(
      /expired/iu,
    );
  });

  it('rejects a replayed nonce', () => {
    expect(() =>
      validateIdToken(
        { iss: base.issuer, aud: 'web-app', exp: future, nonce: 'other' },
        { ...base, nonce: 'expected' },
      ),
    ).toThrow(/nonce/iu);
  });
});
