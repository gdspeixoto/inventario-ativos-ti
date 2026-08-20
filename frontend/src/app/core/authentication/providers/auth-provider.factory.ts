import { InjectionToken, Injectable, computed, inject } from '@angular/core';
import { PlatformService } from '../../services/platform.service';
import { AUTH_CONFIG } from '../auth.tokens';
import { AuthError } from '../auth.errors';
import { AuthStore } from '../auth.store';
import { AuthStorage } from '../internal/auth-storage';
import { OidcClient } from '../internal/oidc-client';
import type { AuthProviderConfig } from '../models/auth-config.model';
import type { AuthProvider } from './auth-provider';
import type { OidcProviderDeps } from './base-oidc.provider';
import { GenericOidcProvider } from './generic-oidc.provider';
import { KeycloakProvider } from './keycloak.provider';
import { MicrosoftProvider } from './microsoft.provider';

/**
 * Extension point for identity providers that are not shipped with the template.
 *
 * Register a factory as a multi-provider and return `null` for configurations
 * it does not handle:
 *
 * ```ts
 * { provide: AUTH_PROVIDER_FACTORY, multi: true, useValue: (config, deps) =>
 *     config.kind === 'my-idp' ? new MyIdpProvider(config, deps) : null }
 * ```
 *
 * Custom factories are consulted before the built-in ones, so they can also
 * override `keycloak`, `microsoft` or `oidc`.
 */
export type AuthProviderFactoryFn = (
  config: AuthProviderConfig,
  deps: OidcProviderDeps,
) => AuthProvider | null;

export const AUTH_PROVIDER_FACTORY = new InjectionToken<readonly AuthProviderFactoryFn[]>(
  'AUTH_PROVIDER_FACTORY',
);

/**
 * Instantiates and caches one `AuthProvider` per configured entry.
 *
 * The rest of the application asks the registry for a provider by id; it never
 * constructs one, and never imports a concrete class.
 */
@Injectable({ providedIn: 'root' })
export class AuthProviderRegistry {
  private readonly config = inject(AUTH_CONFIG);
  private readonly customFactories = inject(AUTH_PROVIDER_FACTORY, { optional: true }) ?? [];

  private readonly deps: OidcProviderDeps = {
    client: inject(OidcClient),
    storage: inject(AuthStorage),
    store: inject(AuthStore),
    platform: inject(PlatformService),
    authConfig: this.config,
  };

  private readonly instances = new Map<string, AuthProvider>();

  /** Enabled providers, ordered by `branding.order` then by label. */
  readonly enabledConfigs = computed<readonly AuthProviderConfig[]>(() =>
    this.config.providers
      .filter((provider) => provider.enabled)
      .sort(
        (a, b) =>
          (a.branding.order ?? 0) - (b.branding.order ?? 0) ||
          a.branding.label.localeCompare(b.branding.label),
      ),
  );

  /** True when the login screen should skip the provider chooser. */
  readonly hasSingleProvider = computed(() => this.enabledConfigs().length === 1);

  get(providerId: string): AuthProvider {
    const cached = this.instances.get(providerId);
    if (cached) {
      return cached;
    }

    const config = this.config.providers.find((provider) => provider.id === providerId);
    if (!config) {
      throw new AuthError(
        'provider_not_found',
        `No authentication provider is configured with id "${providerId}".`,
      );
    }
    if (!config.enabled) {
      throw new AuthError('provider_disabled', `The provider "${providerId}" is disabled.`);
    }

    const instance = this.create(config);
    this.instances.set(providerId, instance);
    return instance;
  }

  /**
   * Provider used when the caller does not name one: the configured default,
   * otherwise the only enabled provider.
   */
  getDefault(): AuthProvider {
    const { defaultProviderId } = this.config;
    if (defaultProviderId) {
      return this.get(defaultProviderId);
    }
    const enabled = this.enabledConfigs();
    if (enabled.length === 1) {
      return this.get(enabled[0].id);
    }
    throw new AuthError(
      'provider_not_found',
      'No default provider configured and more than one is enabled. ' +
        'Set `defaultProviderId` in config/auth.config.ts or pass a provider id to login().',
    );
  }

  /** `null` instead of throwing — used by the session restore path. */
  tryGet(providerId: string | null | undefined): AuthProvider | null {
    if (!providerId) {
      return null;
    }
    try {
      return this.get(providerId);
    } catch {
      return null;
    }
  }

  private create(config: AuthProviderConfig): AuthProvider {
    for (const factory of this.customFactories) {
      const instance = factory(config, this.deps);
      if (instance) {
        return instance;
      }
    }

    switch (config.kind) {
      case 'keycloak':
        return new KeycloakProvider(config, this.deps);
      case 'microsoft':
        return new MicrosoftProvider(config, this.deps);
      case 'oidc':
        return new GenericOidcProvider(config, this.deps);
      default:
        throw new AuthError(
          'provider_not_found',
          `Unsupported provider kind "${String(config.kind)}". ` +
            'Register a factory with AUTH_PROVIDER_FACTORY — see docs/adding-provider.md.',
        );
    }
  }
}
