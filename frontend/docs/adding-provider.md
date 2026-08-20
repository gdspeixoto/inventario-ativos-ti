# Adicionando um provedor de autenticação

Três cenários, do mais simples ao mais raro.

---

## Cenário 1 — Provedor OIDC padrão (95% dos casos)

**Sem escrever código.** Adicione uma entrada em `src/app/config/auth.config.ts`.

```ts
{
  id: 'okta',
  kind: 'oidc',
  enabled: true,
  branding: { label: 'Okta', icon: 'pi pi-shield', order: 3, accentColor: '#007dc1' },
  issuer: 'https://empresa.okta.com/oauth2/default',
  clientId: '<client-id>',
  scope: 'openid profile email offline_access',
  redirectUri: `${origin}/auth/callback`,
  postLogoutRedirectUri: `${origin}/login`,
  responseType: 'code',
}
```

O botão aparece automaticamente na tela de login, ordenado por `branding.order`.

### Se as claims estiverem em lugares fora do padrão

```ts
claims: {
  id: 'user_id',
  name: 'displayName',
  roles: ['https://empresa.com.br/claims/roles', 'groups'],
  permissions: ['scope'],
}
```

Caminhos são separados por ponto e podem apontar para string ou array. Aceita múltiplos caminhos, cujos valores são unidos e desduplicados.

### Se o provedor exigir parâmetros extras

```ts
extraAuthorizationParams: {
  audience: 'https://api.empresa.com.br',   // Auth0
  acr_values: 'urn:mace:incommon:iap:silver',
},
```

---

## Cenário 2 — Provedor com particularidades

Se o provedor precisa de comportamento próprio (normalizar claims, forçar um `prompt`, tratar um endpoint fora do padrão), crie uma subclasse de `BaseOidcProvider`.

### Passo 1 — a classe

```ts
// core/authentication/providers/gov-br.provider.ts
import type { ClaimMapping } from '../models/auth-config.model';
import type { UserClaims } from '../models/user.model';
import { BaseOidcProvider } from './base-oidc.provider';
import type { LoginOptions } from './auth-provider';

/**
 * Gov.br.
 *
 * Duas particularidades: o nível de confiabilidade da conta chega em
 * `amr`, e o CPF vem em `sub` — que é numérico e não serve como
 * `preferred_username`.
 */
export class GovBrProvider extends BaseOidcProvider {
  protected get defaultClaimMapping(): ClaimMapping {
    return {
      id: 'sub',
      name: 'name',
      username: 'email',
      email: 'email',
      picture: 'picture',
      roles: ['amr'],
      permissions: [],
    };
  }

  override login(options: LoginOptions = {}): Promise<void> {
    return super.login({ prompt: 'login', ...options });
  }

  protected override normalizeClaims(claims: UserClaims): UserClaims {
    // Expõe o CPF em uma claim explícita, sem alterar `sub`.
    return Object.freeze({ ...claims, cpf: claims['sub'] });
  }
}
```

Pontos de extensão disponíveis:

| Membro                | Quando sobrescrever                                 |
| --------------------- | --------------------------------------------------- |
| `defaultClaimMapping` | Sempre — é o mínimo que um provedor define          |
| `normalizeClaims()`   | Claims em formato não padrão                        |
| `login()`             | Parâmetros de autorização específicos               |
| `logout()`            | Logout fora do padrão RP-Initiated                  |
| `loadProfile()`       | Perfil vindo de um endpoint próprio (ex.: MS Graph) |

### Passo 2 — o `kind`

```ts
// core/authentication/models/auth-config.model.ts
export type AuthProviderKind = 'keycloak' | 'microsoft' | 'oidc' | 'gov-br';
```

### Passo 3 — registrar

Duas opções.

**A. No registry** (provedor que passa a fazer parte do template):

```ts
// core/authentication/providers/auth-provider.factory.ts
switch (config.kind) {
  case 'keycloak':
    return new KeycloakProvider(config, this.deps);
  case 'microsoft':
    return new MicrosoftProvider(config, this.deps);
  case 'gov-br':
    return new GovBrProvider(config, this.deps);
  case 'oidc':
    return new GenericOidcProvider(config, this.deps);
}
```

**B. Via token de extensão** (provedor específico de um projeto — preferível, pois não altera `core/`):

```ts
// app.config.ts
import { AUTH_PROVIDER_FACTORY } from '@core/authentication';
import { GovBrProvider } from './providers/gov-br.provider';

{
  provide: AUTH_PROVIDER_FACTORY,
  multi: true,
  useValue: (config, deps) =>
    config.kind === 'gov-br' ? new GovBrProvider(config, deps) : null,
}
```

Factories customizadas são consultadas **antes** das nativas, então essa via também permite substituir o comportamento de `keycloak`, `microsoft` ou `oidc`.

### Passo 4 — configurar e traduzir

```ts
// config/auth.config.ts
{ id: 'gov-br', kind: 'gov-br', enabled: true, branding: { label: 'Gov.br', icon: 'pi pi-id-card' }, … }
```

Nenhuma alteração é necessária na tela de login.

### Passo 5 — testar

```ts
describe('GovBrProvider', () => {
  it('expõe o CPF a partir de `sub`', async () => {
    const provider = new GovBrProvider(config, deps);
    const user = await provider.loadProfile(tokens);
    expect(user.claims['cpf']).toBe('12345678900');
  });
});
```

---

## Cenário 3 — Autenticação que não é OIDC

Para SAML, mTLS, um gateway proprietário ou um BFF com cookie de sessão, implemente `AuthProvider` diretamente — sem herdar de `BaseOidcProvider`.

```ts
export class BffSessionProvider implements AuthProvider {
  readonly kind = 'bff' as const;

  constructor(
    readonly config: AuthProviderConfig,
    private readonly deps: OidcProviderDeps,
  ) {}

  get id() {
    return this.config.id;
  }

  async login(): Promise<void> {
    // O BFF cuida do handshake e devolve um cookie HttpOnly.
    this.deps.platform.navigateTo(`${this.config.issuer}/login`);
  }

  async handleCallback(): Promise<AuthSession> {
    const user = await this.fetchSessionUser();
    return { user, tokens: this.sessionTokens() };
  }

  async logout(): Promise<void> {
    this.deps.platform.navigateTo(`${this.config.issuer}/logout`);
  }
  async refreshToken(t: AuthTokens): Promise<AuthTokens> {
    return t;
  } // o cookie renova sozinho
  async loadProfile(): Promise<User> {
    return this.fetchSessionUser();
  }

  getUser() {
    return this.deps.store.user();
  }
  isAuthenticated() {
    return this.deps.store.isAuthenticated();
  }
  getAccessToken() {
    return this.deps.store.accessToken();
  }
  getIdToken() {
    return this.deps.store.idToken();
  }
}
```

O restante da aplicação — guards, diretivas, interceptors, telas — continua funcionando sem qualquer alteração. Esse é exatamente o ponto da interface.

---

## Checklist

- [ ] Provedor configurado em `auth.config.ts` com `redirectUri` idêntico ao cadastrado no IdP
- [ ] `kind` adicionado ao tipo, se for um provedor novo
- [ ] Registrado no registry **ou** via `AUTH_PROVIDER_FACTORY`
- [ ] Mapeamento de claims validado com um token real
- [ ] `scope` inclui `offline_access` se houver refresh token
- [ ] Redirect e post-logout URIs cadastrados no provedor
- [ ] PKCE `S256` habilitado; fluxo implícito desabilitado
- [ ] Testado: login, refresh, logout e restauração de sessão após reload

---

## Leitura relacionada

- [`authentication.md`](./authentication.md) — o funcionamento da biblioteca
