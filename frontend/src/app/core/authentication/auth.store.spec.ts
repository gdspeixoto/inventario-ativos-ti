import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthStore } from './auth.store';
import { AuthError } from './auth.errors';
import type { AuthTokens } from './models/auth-tokens.model';
import type { User } from './models/user.model';

const tokens: AuthTokens = {
  accessToken: 'access',
  idToken: 'id',
  refreshToken: 'refresh',
  tokenType: 'Bearer',
  expiresAt: Date.now() + 60_000,
  refreshExpiresAt: null,
  scope: 'openid',
  provider: 'keycloak',
};

const user: User = {
  id: '1',
  name: 'Ana',
  username: 'ana',
  email: 'ana@empresa.com.br',
  picture: null,
  roles: ['admin'],
  permissions: ['user:read'],
  claims: {},
  provider: 'keycloak',
};

describe('AuthStore', () => {
  let store: AuthStore;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    store = TestBed.inject(AuthStore);
  });

  it('starts unresolved so guards never act on a half-known session', () => {
    expect(store.status()).toBe('unknown');
    expect(store.isResolved()).toBe(false);
    expect(store.isAuthenticated()).toBe(false);
  });

  it('exposes derived session data once authenticated', () => {
    store.setSession({ user, tokens });

    expect(store.isAuthenticated()).toBe(true);
    expect(store.accessToken()).toBe('access');
    expect(store.roles()).toEqual(['admin']);
    expect(store.providerId()).toBe('keycloak');
  });

  it('falls back to an anonymous user instead of null in templates', () => {
    expect(store.currentUser().name).toBe('');
    expect(store.currentUser().roles).toEqual([]);
  });

  it('clears user and tokens when signing out', () => {
    store.setSession({ user, tokens });
    store.setAnonymous();

    expect(store.status()).toBe('anonymous');
    expect(store.accessToken()).toBeNull();
    expect(store.user()).toBeNull();
  });

  it('consumes the return URL exactly once', () => {
    store.setReturnUrl('/reports/42');

    expect(store.consumeReturnUrl()).toBe('/reports/42');
    expect(store.consumeReturnUrl()).toBeNull();
  });

  it('an error ends the session rather than leaving it pending', () => {
    store.startAuthentication();
    store.setError(new AuthError('state_mismatch', 'bad state'));

    expect(store.status()).toBe('anonymous');
    expect(store.error()?.code).toBe('state_mismatch');
  });
});
