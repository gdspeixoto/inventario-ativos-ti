/**
 * Development environment.
 *
 * Swapped for `environment.production.ts` by the `production` build
 * configuration (see `fileReplacements` in angular.json).
 *
 * Keep this file free of secrets: everything here ships to the browser.
 */
export const environment = {
  production: false,

  /** Base URL prepended to every relative path passed to `ApiService`. */
  apiBaseUrl: 'http://localhost:5169/api/v1',

  /** Identity provider base URLs, referenced by `config/auth.config.ts`. */
  keycloakIssuer: 'https://sso.example.com/realms/inventario',
  microsoftTenantId: 'common',
  microsoftClientId: '00000000-0000-0000-0000-000000000000',
  keycloakClientId: 'controle-ativos-frontend',

  /** Origin used to build OIDC redirect URIs. */
  appOrigin: 'http://localhost:4200',

  /** Verbosity of `LoggerService`: 'debug' | 'info' | 'warn' | 'error' | 'silent'. */
  logLevel: 'debug' as const,

  /** Feature flags evaluated by `FeatureFlagService`. */
  features: {
    darkMode: true,
    i18n: true,
    /**
     * Login por usuario e senha, atendido pelo proprio backend.
     * E acesso de emergencia: o caminho normal e o SSO corporativo.
     */
    localCredentialsLogin: true,

    /**
     * Popula as telas com dados ficticios, sem chamar a API.
     *
     * Ligado, nenhuma requisicao sai do browser: as telas leem `demo-data.ts`.
     * Mantenha `false` para exercitar o backend de verdade; ligue apenas para
     * demonstrar a interface sem depender do Keycloak.
     */
    demoData: false,
  },
} as const;
