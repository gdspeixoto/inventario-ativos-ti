/**
 * Public API of the authentication library.
 *
 * Application code imports from here. Everything under `internal/` is an
 * implementation detail and may change without notice.
 *
 * ```ts
 * import { AuthService, type User } from '@core/authentication';
 * ```
 */

export { AuthService } from './auth.service';
export { AuthStore, type AuthSession, type AuthStatus } from './auth.store';
export { AuthError, type AuthErrorCode } from './auth.errors';
export { AUTH_CONFIG } from './auth.tokens';

export type {
  AuthConfig,
  AuthProviderConfig,
  AuthProviderKind,
  AuthRoutesConfig,
  AuthStorageKind,
  ClaimMapping,
  CredentialsLoginConfig,
} from './models/auth-config.model';

export type { AuthTokens, OidcDiscoveryDocument } from './models/auth-tokens.model';

export {
  ANONYMOUS_USER,
  displayNameOf,
  hasAllRoles,
  hasAnyPermission,
  hasAnyRole,
  hasPermission,
  hasRole,
  type User,
  type UserClaims,
} from './models/user.model';

export type {
  AuthProvider,
  CallbackParams,
  LoginOptions,
  LogoutOptions,
} from './providers/auth-provider';

export {
  AUTH_PROVIDER_FACTORY,
  AuthProviderRegistry,
  type AuthProviderFactoryFn,
} from './providers/auth-provider.factory';

export { BaseOidcProvider, type OidcProviderDeps } from './providers/base-oidc.provider';
export { GenericOidcProvider } from './providers/generic-oidc.provider';
export { KeycloakProvider } from './providers/keycloak.provider';
export { MicrosoftProvider } from './providers/microsoft.provider';
