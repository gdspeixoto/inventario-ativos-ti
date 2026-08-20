import { describe, expect, it } from 'vitest';
import { buildUser, mergeClaims, readClaim, readStringListClaim } from './claims';

describe('readClaim', () => {
  it('resolves a dot path', () => {
    const claims = { resource_access: { 'web-app': { roles: ['admin'] } } };
    expect(readClaim(claims, 'resource_access.web-app.roles')).toEqual(['admin']);
  });

  it('returns undefined for a broken path instead of throwing', () => {
    expect(readClaim({ a: 1 }, 'a.b.c')).toBeUndefined();
  });
});

describe('readStringListClaim', () => {
  it('merges several paths and de-duplicates', () => {
    const claims = { realm_access: { roles: ['admin', 'user'] }, groups: ['user', 'audit'] };
    expect(readStringListClaim(claims, ['realm_access.roles', 'groups']).sort()).toEqual([
      'admin',
      'audit',
      'user',
    ]);
  });

  it('splits space-separated scope strings', () => {
    expect(readStringListClaim({ scp: 'read write' }, ['scp'])).toEqual(['read', 'write']);
  });
});

describe('buildUser', () => {
  it('maps Keycloak claims onto the canonical model', () => {
    const user = buildUser({
      providerId: 'keycloak',
      mapping: {
        id: 'sub',
        name: 'name',
        username: 'preferred_username',
        email: 'email',
        roles: ['realm_access.roles'],
      },
      claims: {
        sub: 'abc-123',
        name: 'Ana Souza',
        preferred_username: 'ana.souza',
        email: 'ana@empresa.com.br',
        realm_access: { roles: ['admin'] },
      },
    });

    expect(user).toMatchObject({
      id: 'abc-123',
      name: 'Ana Souza',
      username: 'ana.souza',
      email: 'ana@empresa.com.br',
      provider: 'keycloak',
    });
    expect(user.roles).toEqual(['admin']);
  });

  it('falls back to the username when no name claim exists', () => {
    const user = buildUser({
      providerId: 'oidc',
      mapping: {},
      claims: { sub: 'x', preferred_username: 'joao' },
    });
    expect(user.name).toBe('joao');
  });

  it('produces an empty role list rather than undefined', () => {
    const user = buildUser({ providerId: 'oidc', mapping: {}, claims: { sub: 'x' } });
    expect(user.roles).toEqual([]);
    expect(user.permissions).toEqual([]);
  });
});

describe('mergeClaims', () => {
  it('lets later sources win', () => {
    expect(mergeClaims({ email: 'old@x.com' }, null, { email: 'new@x.com' })).toEqual({
      email: 'new@x.com',
    });
  });
});
