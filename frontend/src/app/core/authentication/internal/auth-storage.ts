import { Injectable, inject } from '@angular/core';
import { StorageService } from '../../services/storage.service';
import { AUTH_CONFIG } from '../auth.tokens';
import type { AuthTokens, AuthorizationRequestState } from '../models/auth-tokens.model';

const KEYS = {
  tokens: 'auth.tokens',
  request: 'auth.request',
  provider: 'auth.provider',
} as const;

/**
 * Persistence for the authenticated session.
 *
 * Storage kind is configurable (`auth.config.ts`):
 *  - `session` (default) — tokens die with the tab. Safest sensible option.
 *  - `local`             — survives a browser restart; enables silent SSO across tabs.
 *  - `memory`            — nothing is written; a page refresh forces a new login.
 *
 * The pending authorization request (state/nonce/verifier) always uses
 * sessionStorage: it is single-use, tab-scoped, and must not outlive the tab.
 */
@Injectable({ providedIn: 'root' })
export class AuthStorage {
  private readonly storage = inject(StorageService);
  private readonly config = inject(AUTH_CONFIG);

  private memoryTokens: AuthTokens | null = null;

  get isMemoryOnly(): boolean {
    return this.config.storage === 'memory';
  }

  readTokens(): AuthTokens | null {
    if (this.isMemoryOnly) {
      return this.memoryTokens;
    }
    return this.storage.get<AuthTokens>(KEYS.tokens, this.kind);
  }

  writeTokens(tokens: AuthTokens): void {
    if (this.isMemoryOnly) {
      this.memoryTokens = tokens;
      return;
    }
    this.storage.set(KEYS.tokens, tokens, this.kind);
  }

  clearTokens(): void {
    this.memoryTokens = null;
    this.storage.remove(KEYS.tokens, this.kind);
  }

  readActiveProviderId(): string | null {
    return this.storage.get<string>(KEYS.provider, this.kind);
  }

  writeActiveProviderId(providerId: string): void {
    this.storage.set(KEYS.provider, providerId, this.kind);
  }

  readRequestState(): AuthorizationRequestState | null {
    return this.storage.get<AuthorizationRequestState>(KEYS.request, 'session');
  }

  writeRequestState(state: AuthorizationRequestState): void {
    this.storage.set(KEYS.request, state, 'session');
  }

  clearRequestState(): void {
    this.storage.remove(KEYS.request, 'session');
  }

  clearAll(): void {
    this.clearTokens();
    this.clearRequestState();
    this.storage.remove(KEYS.provider, this.kind);
  }

  private get kind(): 'local' | 'session' {
    return this.config.storage === 'local' ? 'local' : 'session';
  }
}
