/**
 * Production environment.
 *
 * Values here are placeholders: in a container deployment they are typically
 * rewritten at container start-up (see `docs/deployment.md`) so that the same
 * image can be promoted across environments.
 */
export const environment = {
  production: true,

  apiBaseUrl: '/api/v1',

  keycloakIssuer: 'https://sso.example.com/realms/inventario',
  microsoftTenantId: '00000000-0000-0000-0000-000000000000',
  microsoftClientId: '00000000-0000-0000-0000-000000000000',
  keycloakClientId: 'controle-ativos-frontend',

  appOrigin: 'https://inventario.example.com',

  logLevel: 'warn' as const,

  features: {
    darkMode: true,
    i18n: true,
    /**
     * Em producao o acesso de emergencia so deve ser exposto por decisao
     * explicita, e o backend ainda exige `Access:LocalLogin:AllowedInProduction`.
     */
    localCredentialsLogin: false,

    /** Dados ficticios de demonstracao. Sempre desligado em producao. */
    demoData: false,
  },
} as const;
