/**
 * Configuration contract for the authentication library.
 *
 * The application never imports a vendor SDK: it configures *providers*.
 * Switching from Keycloak to Microsoft Entra ID is a change in
 * `config/auth.config.ts`, never a change in application code.
 */

/** Built-in provider implementations shipped with the template. */
export type AuthProviderKind = 'keycloak' | 'microsoft' | 'oidc';

/** Where tokens are persisted between page reloads. */
export type AuthStorageKind = 'session' | 'local' | 'memory';

/**
 * Maps provider-specific claims onto the canonical `User` model.
 * Every entry is a dot-path evaluated against the merged id_token / userinfo
 * claim set, e.g. `resource_access.my-client.roles`.
 */
export interface ClaimMapping {
  id?: string;
  name?: string;
  username?: string;
  email?: string;
  picture?: string;
  /** Claim paths merged into `User.roles`. */
  roles?: string[];
  /** Claim paths merged into `User.permissions`. */
  permissions?: string[];
}

/** Visual identity of a provider button on the login screen. */
export interface AuthProviderBranding {
  /** Label rendered on the button, or an i18n key. */
  label: string;
  /** PrimeIcons class (`pi pi-microsoft`) — ignored when `iconSvg` is set. */
  icon?: string;
  /** Inline SVG markup for brands PrimeIcons does not cover. */
  iconSvg?: string;
  /** Optional accent used for the button border/hover. Must be a CSS color. */
  accentColor?: string;
  /** Lower numbers render first. */
  order?: number;
}

export interface AuthProviderConfig {
  /** Stable key. Appears in storage keys and in `?provider=` on the callback. */
  id: string;
  kind: AuthProviderKind;
  enabled: boolean;
  branding: AuthProviderBranding;

  /** OIDC issuer. Discovery document is fetched from `{issuer}/.well-known/openid-configuration`. */
  issuer: string;
  clientId: string;
  scope: string;
  redirectUri: string;
  postLogoutRedirectUri: string;

  /**
   * Only `code` is supported. The implicit flow is intentionally unavailable —
   * see `docs/authentication.md`.
   */
  responseType: 'code';

  /** Requests a refresh token (`offline_access` still has to be in `scope`). */
  useRefreshToken?: boolean;

  /** Extra query params appended to the authorization request. */
  extraAuthorizationParams?: Record<string, string>;

  /** Keycloak realm — informational, the realm is already part of `issuer`. */
  realm?: string;

  /** Microsoft Entra ID / Azure AD B2C tenant (`common`, `organizations`, or a GUID). */
  tenantId?: string;

  /** Azure AD B2C user flow, e.g. `B2C_1_signupsignin`. */
  userFlow?: string;

  /** Overrides the provider's default claim mapping. */
  claims?: ClaimMapping;
}

/** Email + password sign-in handled by the application's own backend. */
export interface CredentialsLoginConfig {
  enabled: boolean;
  /** Relative path resolved against `environment.apiBaseUrl`. */
  endpoint: string;
  /** Shown under the form; typically a "forgot password" URL. */
  forgotPasswordUrl?: string;
}

export interface AuthRoutesConfig {
  login: string;
  callback: string;
  afterLogin: string;
  afterLogout: string;
  forbidden: string;
}

export interface AuthConfig {
  /** Provider used by `login()` when no id is supplied. */
  defaultProviderId: string | null;

  /**
   * Redirects straight to the identity provider when exactly one provider is
   * enabled and credentials login is off — skips the login screen entirely.
   */
  autoRedirectSingleProvider: boolean;

  storage: AuthStorageKind;

  /** Tolerance applied to `exp` / `nbf` validation, in seconds. */
  clockSkewSeconds: number;

  /** How long before expiry the silent refresh fires, in seconds. */
  refreshLeewaySeconds: number;

  /** Loads `/userinfo` after the token exchange to enrich the profile. */
  loadUserInfo: boolean;

  routes: AuthRoutesConfig;
  credentials: CredentialsLoginConfig;
  providers: AuthProviderConfig[];
}
