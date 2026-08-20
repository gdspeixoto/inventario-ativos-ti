# Autenticação

A autenticação é uma **biblioteca interna** (`src/app/core/authentication`) totalmente desacoplada da aplicação. Nenhuma página, guard ou componente conhece Keycloak, Microsoft Entra ID ou qualquer outro fornecedor: todos falam com `AuthService` e com a interface `AuthProvider`.

Trocar de provedor de identidade é **uma alteração em `src/app/config/auth.config.ts`**.

---

## 1. Fluxo suportado: Authorization Code + PKCE

Somente. O fluxo implícito **não está implementado** e não será: ele expõe tokens no fragmento da URL (histórico do navegador, logs de proxy, `Referer`) e foi removido do OAuth 2.1.

```
┌──────────┐  1. /authorize?code_challenge=...  ┌─────────────────┐
│ Browser  │ ─────────────────────────────────▶ │ Identity        │
│          │                                    │ Provider        │
│          │ ◀───────────────────────────────── │                 │
└──────────┘  2. redirect /auth/callback?code   └─────────────────┘
      │
      │ 3. POST /token  { code, code_verifier }        (sem client secret)
      ▼
┌──────────────────────────────────────────────┐
│ access_token · id_token · refresh_token      │
└──────────────────────────────────────────────┘
```

### Proteções aplicadas

| Ataque                  | Defesa                                                                  | Implementação           |
| ----------------------- | ----------------------------------------------------------------------- | ----------------------- |
| Interceptação do código | PKCE `S256` (`code_verifier` de 64 bytes aleatórios)                    | `internal/pkce.ts`      |
| CSRF no callback        | `state` aleatório, comparado e **consumido antes de qualquer `await`**  | `base-oidc.provider.ts` |
| Replay do `id_token`    | `nonce` gerado por requisição e validado no token                       | `internal/jwt.ts`       |
| Token de outro emissor  | Validação de `iss`, `aud`, `exp`, `nbf` com tolerância de relógio       | `validateIdToken()`     |
| Callback reaproveitado  | `state` de uso único + TTL de 10 minutos                                | `REQUEST_STATE_TTL_MS`  |
| Contexto inseguro       | Erro explícito quando `crypto.subtle` não existe (sem fallback `plain`) | `InsecureContextError`  |

> **Sobre a assinatura do `id_token`.** Ela não é verificada no browser, por decisão consciente e alinhada à RFC 8725 §3.10: no Authorization Code flow o token vem **direto do token endpoint sobre TLS**, não passa pelo agente do usuário. O `access_token` é opaco para o frontend e é validado pelo backend. Tudo que é decodificado no cliente serve apenas para exibição — **autorização de verdade acontece no servidor**.

---

## 2. Anatomia da biblioteca

```
core/authentication/
├── index.ts                    # API pública (importe daqui)
├── auth.service.ts             # fachada usada pela aplicação
├── auth.store.ts               # estado da sessão (signals)
├── auth.errors.ts              # AuthError + códigos
├── auth.tokens.ts              # AUTH_CONFIG
├── models/
│   ├── auth-config.model.ts    # contratos de configuração
│   ├── auth-tokens.model.ts    # AuthTokens, discovery, request state
│   └── user.model.ts           # User canônico + helpers de role/permission
├── providers/
│   ├── auth-provider.ts        # a INTERFACE
│   ├── base-oidc.provider.ts   # fluxo PKCE implementado uma única vez
│   ├── generic-oidc.provider.ts
│   ├── keycloak.provider.ts
│   ├── microsoft.provider.ts
│   └── auth-provider.factory.ts # registry + AUTH_PROVIDER_FACTORY
└── internal/                   # detalhe de implementação — não importe
    ├── pkce.ts  jwt.ts  claims.ts  auth-storage.ts  oidc-client.ts
```

### A interface

```ts
export interface AuthProvider {
  readonly id: string;
  readonly kind: AuthProviderKind;
  readonly config: AuthProviderConfig;

  login(options?: LoginOptions): Promise<void>;
  handleCallback(params: CallbackParams): Promise<AuthSession>;
  logout(options?: LogoutOptions): Promise<void>;
  refreshToken(tokens: AuthTokens): Promise<AuthTokens>;

  getUser(): User | null;
  isAuthenticated(): boolean;
  getAccessToken(): string | null;
  getIdToken(): string | null;
  loadProfile(tokens: AuthTokens): Promise<User>;
}
```

`BaseOidcProvider` implementa tudo isso. Os provedores concretos sobrescrevem, na prática, apenas o **mapeamento de claims**.

---

## 3. Configuração

Arquivo: `src/app/config/auth.config.ts`.

```ts
export const authConfig: AuthConfig = {
  defaultProviderId: 'keycloak',
  autoRedirectSingleProvider: false,
  storage: 'session',
  clockSkewSeconds: 30,
  refreshLeewaySeconds: 60,
  loadUserInfo: true,

  routes: {
    login: '/login',
    callback: '/auth/callback',
    afterLogin: '/dashboard',
    afterLogout: '/login',
    forbidden: '/403',
  },

  credentials: { enabled: true, endpoint: '/auth/login' },

  providers: [/* ... */],
};
```

### Campos do provedor

| Campo                      | Obrigatório | Descrição                                                          |
| -------------------------- | ----------- | ------------------------------------------------------------------ |
| `id`                       | sim         | Chave estável; aparece em storage e logs                           |
| `kind`                     | sim         | `keycloak` \| `microsoft` \| `oidc`                                |
| `enabled`                  | sim         | Controla a exibição na tela de login                               |
| `issuer`                   | sim         | Discovery é buscado em `{issuer}/.well-known/openid-configuration` |
| `clientId`                 | sim         | Cliente **público** (sem secret)                                   |
| `scope`                    | sim         | Inclua `offline_access` para receber refresh token                 |
| `redirectUri`              | sim         | Deve bater exatamente com o cadastrado no provedor                 |
| `postLogoutRedirectUri`    | sim         | Destino após o logout no provedor                                  |
| `responseType`             | sim         | Apenas `'code'`                                                    |
| `branding`                 | sim         | `label`, `icon`, `order`, `accentColor` — usados no botão de login |
| `extraAuthorizationParams` | não         | Parâmetros extras na URL de autorização                            |
| `userFlow`                 | não         | Política do Azure AD B2C (`p=`)                                    |
| `claims`                   | não         | Sobrescreve o mapeamento padrão do provedor                        |

### Opções globais

| Opção                        | Padrão    | Efeito                                                                |
| ---------------------------- | --------- | --------------------------------------------------------------------- |
| `storage`                    | `session` | `session` morre com a aba · `local` sobrevive · `memory` não persiste |
| `clockSkewSeconds`           | `30`      | Tolerância na validação de `exp`/`nbf`                                |
| `refreshLeewaySeconds`       | `60`      | Antecedência do refresh silencioso                                    |
| `loadUserInfo`               | `true`    | Consulta `/userinfo` para enriquecer o perfil                         |
| `autoRedirectSingleProvider` | `false`   | Pula a tela de login quando há um único provedor e não há formulário  |

---

## 4. Receitas de configuração

### Keycloak

```ts
{
  id: 'keycloak',
  kind: 'keycloak',
  enabled: true,
  branding: { label: 'Keycloak', icon: 'pi pi-key', order: 1 },
  issuer: 'https://sso.example.com/realms/inventario',
  clientId: 'controle-ativos-frontend',
  scope: 'openid profile email offline_access',
  redirectUri: `${origin}/auth/callback`,
  postLogoutRedirectUri: `${origin}/login`,
  responseType: 'code',
}
```

No console do Keycloak, o client precisa de:

- **Client authentication**: `Off` (cliente público)
- **Standard flow**: `On`; **Implicit flow**: `Off`
- **Valid redirect URIs**: `https://app.empresa.com.br/auth/callback`
- **Valid post logout redirect URIs**: `https://app.empresa.com.br/login`
- **Web origins**: `https://app.empresa.com.br` (não use `*`)
- **Proof Key for Code Exchange**: `S256`

Roles chegam em `realm_access.roles` e `resource_access.<clientId>.roles`; ambos são lidos automaticamente.

### Microsoft Entra ID

```ts
{
  id: 'microsoft',
  kind: 'microsoft',
  enabled: true,
  branding: { label: 'Microsoft', icon: 'pi pi-microsoft', order: 2 },
  issuer: `https://login.microsoftonline.com/${tenantId}/v2.0`,
  clientId: '<application-id>',
  scope: 'openid profile email offline_access User.Read',
  redirectUri: `${origin}/auth/callback`,
  postLogoutRedirectUri: `${origin}/login`,
  responseType: 'code',
  tenantId,
}
```

No portal Azure, no _App registration_:

- **Platform**: Single-page application (SPA) — obriga PKCE e libera CORS no token endpoint
- **Redirect URI**: `https://app.empresa.com.br/auth/callback`
- **Implicit grant**: ambas as caixas desmarcadas
- **App roles** e/ou **Groups claim** habilitados, se você usa autorização por perfil

Particularidades já tratadas por `MicrosoftProvider`: identificador estável em `oid` (não `sub`), `prompt=select_account` por padrão e normalização de `emails[]` para `email`.

### Azure AD B2C

Igual ao Entra ID, com duas diferenças:

```ts
issuer: 'https://empresa.b2clogin.com/empresa.onmicrosoft.com/B2C_1_signupsignin/v2.0',
userFlow: 'B2C_1_signupsignin',
```

### OIDC genérico (Auth0, Okta, Google, Ping…)

```ts
{
  id: 'okta',
  kind: 'oidc',
  enabled: true,
  branding: { label: 'Okta', icon: 'pi pi-shield' },
  issuer: 'https://empresa.okta.com/oauth2/default',
  clientId: '<client-id>',
  scope: 'openid profile email offline_access',
  redirectUri: `${origin}/auth/callback`,
  postLogoutRedirectUri: `${origin}/login`,
  responseType: 'code',
  claims: { roles: ['https://empresa.com.br/claims/roles'] },
}
```

---

## 5. Mapeamento de claims

Cada provedor declara **onde procurar** cada informação; `internal/claims.ts` faz a leitura. Caminhos são separados por ponto e podem apontar para arrays ou strings.

```ts
claims: {
  id: 'sub',
  name: 'name',
  username: 'preferred_username',
  email: 'email',
  picture: 'picture',
  roles: ['realm_access.roles', 'resource_access.web-app.roles'],
  permissions: ['scope'],
}
```

Precedência das fontes ao montar o `User`: `access_token` → `id_token` → `/userinfo` (a última vence).

O resultado é sempre o mesmo modelo, independentemente do provedor:

```ts
interface User {
  id: string;
  name: string;
  username: string;
  email: string;
  picture: string | null;
  roles: readonly string[];
  permissions: readonly string[];
  claims: UserClaims; // payload bruto, para casos específicos
  provider: string;
}
```

---

## 6. Uso na aplicação

### Nos componentes

```ts
export class MinhaPagina {
  private readonly auth = inject(AuthService);

  readonly user = this.auth.user; // Signal<User>
  readonly isAdmin = computed(() => this.auth.hasRole('admin'));

  sair(): void {
    void this.auth.logout();
  }
}
```

### Nos templates

```html
<button *appHasRole="'admin'">Excluir</button>
<section *appHasPermission="['report:read', 'report:export']; match: 'all'">…</section>
```

### Nas rotas

```ts
{
  path: 'relatorios',
  canActivate: [authGuard, permissionGuard('report:read')],
  loadComponent: () => import('./relatorios.component').then((m) => m.RelatoriosComponent),
}

{
  path: 'admin',
  canActivate: [authGuard, roleGuard(['admin', 'auditor'], { match: 'all' })],
  loadComponent: () => import('./admin.component').then((m) => m.AdminComponent),
}
```

> Esconder um botão ou bloquear uma rota é **usabilidade**, não segurança. A API precisa validar a mesma regra.

---

## 7. Ciclo de vida da sessão

### Restauração no boot

`provideAppInitializer` chama `restoreSession()`:

1. lê tokens do storage;
2. se expirados, tenta `refreshToken()`;
3. recarrega o perfil;
4. em qualquer falha, limpa tudo e marca a sessão como anônima.

Como isso roda **antes da primeira rota**, os guards são síncronos e não existe piscar de tela.

### Refresh silencioso

Ao receber um token, `AuthService` agenda um `setTimeout` para `expiresAt - refreshLeewaySeconds`. O usuário nunca vê um 401 por expiração normal.

### Refresh reativo (401)

Se um 401 escapar (revogação no servidor, desvio de relógio), `authInterceptor` dispara o refresh e **repete a requisição**. Chamadas concorrentes compartilham um único refresh — `AuthService.refresh()` deduplica com `inFlightRefresh`.

Se o refresh falhar: sessão encerrada, URL atual guardada como `returnUrl`, redirecionamento para o login.

### Logout

`logout()` limpa o storage e, salvo `{ localOnly: true }`, redireciona para o `end_session_endpoint` do provedor (RP-Initiated Logout) com `id_token_hint`. Se o discovery falhar, o logout local ainda acontece.

---

## 8. Login com e-mail e senha

O formulário da tela de login chama `AuthService.loginWithCredentials()`, que faz `POST` em `credentials.endpoint` (por padrão `/auth/login`) **do backend da própria aplicação**.

Ele **não** executa o grant _Resource Owner Password_ contra o provedor de identidade: esse fluxo exige client secret, é vedado a aplicações públicas e foi removido do OAuth 2.1. O backend é quem detém o segredo e faz a troca, devolvendo ao frontend um envelope compatível com o token endpoint:

```jsonc
{ "access_token": "...", "token_type": "Bearer", "expires_in": 3600, "refresh_token": "..." }
```

Se sua organização usa SSO exclusivo, desative:

```ts
credentials: { enabled: false, endpoint: '' }
```

A tela de login se ajusta sozinha — some o formulário e o separador "ou continue com".

---

## 9. Onde os tokens ficam

| Modo      | Local            | Sobrevive a reload | Sobrevive a fechar a aba | Quando usar                          |
| --------- | ---------------- | ------------------ | ------------------------ | ------------------------------------ |
| `session` | `sessionStorage` | sim                | não                      | **Padrão.** Melhor equilíbrio        |
| `local`   | `localStorage`   | sim                | sim                      | SSO entre abas, sessões longas       |
| `memory`  | memória          | não                | não                      | Ambientes de altíssima sensibilidade |

O `state`/`nonce`/`code_verifier` pendentes **sempre** usam `sessionStorage`: são de uso único e não devem sobreviver à aba.

> Qualquer storage acessível por JavaScript é vulnerável a XSS. A mitigação real é não ter XSS: CSP restritiva, nunca usar `innerHTML` com dado de usuário, dependências auditadas. Cookies `HttpOnly` só ajudam se o backend fizer o papel de BFF — nesse caso, veja a seção seguinte.

---

## 10. Erros

Todos os erros são `AuthError` com um `code` discriminante e uma `translationKey` pronta:

| Código                  | Significado                                  |
| ----------------------- | -------------------------------------------- |
| `provider_not_found`    | Nenhum provedor com esse id                  |
| `provider_disabled`     | Provedor existe mas está desabilitado        |
| `discovery_failed`      | `.well-known` inacessível                    |
| `authorization_failed`  | O provedor devolveu `error=` no callback     |
| `state_mismatch`        | `state` divergente — possível CSRF           |
| `missing_request_state` | Callback sem requisição pendente ou expirada |
| `token_request_failed`  | Token endpoint recusou o código              |
| `invalid_id_token`      | `id_token` inválido na validação de claims   |
| `refresh_failed`        | Refresh recusado ou ausente                  |
| `invalid_credentials`   | E-mail/senha rejeitados pelo backend         |
| `insecure_context`      | Web Crypto indisponível (sem HTTPS)          |

A tela de login traduz automaticamente via `auth.errors.<code>` nos bundles de i18n.

---

## 11. Solução de problemas

| Sintoma                                    | Causa provável                                                               |
| ------------------------------------------ | ---------------------------------------------------------------------------- |
| `insecure_context` em desenvolvimento      | Acessando por IP em vez de `localhost`; Web Crypto exige contexto seguro     |
| `discovery_failed`                         | `issuer` errado, ou CORS bloqueando o `.well-known`                          |
| `state_mismatch` ao recarregar o callback  | Comportamento correto: o `state` é de uso único                              |
| `token_request_failed` com `invalid_grant` | `redirectUri` diferente do cadastrado, ou código já usado                    |
| Nenhum refresh acontece                    | `offline_access` ausente do `scope`, ou refresh token desabilitado no client |
| `roles` vazio                              | Claim não incluída no token — habilite o mapper no provedor                  |
| Logout não encerra a sessão no IdP         | Provedor sem `end_session_endpoint` no discovery                             |

---

## Leitura relacionada

- [`adding-provider.md`](./adding-provider.md) — como adicionar um provedor não suportado
- [`architecture.md`](./architecture.md) — como a biblioteca se encaixa no todo
