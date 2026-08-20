import type { AuthConfig } from '@core/authentication/models/auth-config.model';
import { environment } from '@env/environment';

/**
 * Authentication configuration — the only file to touch when changing identity
 * provider.
 *
 * To switch from Keycloak to Microsoft Entra ID:
 *   1. set `enabled: true` on the `microsoft` entry (and `false` on `keycloak`);
 *   2. point `defaultProviderId` at it.
 * No application code changes. See `docs/authentication.md`.
 *
 * Every provider uses Authorization Code + PKCE. The implicit flow is not
 * supported by the library, by design.
 */

const origin = environment.appOrigin;
const redirectUri = `${origin}/auth/callback`;
const postLogoutRedirectUri = `${origin}/login`;

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

  /**
   * Email + password handled by the application's own backend. Disable it when
   * the company standard is single sign-on only.
   */
  credentials: {
    enabled: environment.features.localCredentialsLogin,
    endpoint: '/auth/local/login',
    forgotPasswordUrl: '',
  },

  providers: [
    {
      id: 'keycloak',
      kind: 'keycloak',
      enabled: true,
      branding: {
        label: 'Keycloak',
        icon: 'pi pi-key',
        order: 1,
      },
      issuer: environment.keycloakIssuer,
      clientId: environment.keycloakClientId,
      scope: 'openid profile email offline_access',
      redirectUri,
      postLogoutRedirectUri,
      responseType: 'code',
      useRefreshToken: true,
      realm: 'corporate',
    },
    {
      id: 'microsoft',
      kind: 'microsoft',
      enabled: false,
      branding: {
        label: 'Microsoft',
        icon: 'pi pi-microsoft',
        order: 2,
      },
      issuer: `https://login.microsoftonline.com/${environment.microsoftTenantId}/v2.0`,
      clientId: environment.microsoftClientId,
      scope: 'openid profile email offline_access User.Read',
      redirectUri,
      postLogoutRedirectUri,
      responseType: 'code',
      useRefreshToken: true,
      tenantId: environment.microsoftTenantId,
    },
    {
      /**
       * Azure AD B2C. Note the different issuer host (`*.b2clogin.com`) and the
       * mandatory `userFlow` policy.
       */
      id: 'azure-b2c',
      kind: 'microsoft',
      enabled: false,
      branding: {
        label: 'Azure AD B2C',
        icon: 'pi pi-cloud',
        order: 3,
      },
      issuer: 'https://empresa.b2clogin.com/empresa.onmicrosoft.com/B2C_1_signupsignin/v2.0',
      clientId: environment.microsoftClientId,
      scope: 'openid profile email offline_access',
      redirectUri,
      postLogoutRedirectUri,
      responseType: 'code',
      userFlow: 'B2C_1_signupsignin',
    },
    {
      /** Any spec-compliant OIDC server. Google is used here as an example. */
      id: 'google',
      kind: 'oidc',
      enabled: false,
      branding: {
        label: 'Google',
        icon: 'pi pi-google',
        order: 4,
      },
      issuer: 'https://accounts.google.com',
      clientId: 'CHANGE_ME.apps.googleusercontent.com',
      scope: 'openid profile email',
      redirectUri,
      postLogoutRedirectUri,
      responseType: 'code',
    },
  ],
};
