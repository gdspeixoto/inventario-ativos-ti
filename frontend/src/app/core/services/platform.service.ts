import { DOCUMENT, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Single gateway to browser-only globals.
 *
 * The template is SSR-ready: no service may touch `window`, `document`,
 * `localStorage` or `matchMedia` directly. Everything goes through here, which
 * degrades to safe no-ops on the server.
 */
@Injectable({ providedIn: 'root' })
export class PlatformService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly documentRef = inject(DOCUMENT);

  readonly isBrowser = isPlatformBrowser(this.platformId);
  readonly isServer = !this.isBrowser;

  get document(): Document {
    return this.documentRef;
  }

  /** `null` on the server. */
  get window(): Window | null {
    return this.documentRef.defaultView;
  }

  get location(): Location | null {
    return this.window?.location ?? null;
  }

  /** Current origin, or an empty string when rendering on the server. */
  get origin(): string {
    return this.location?.origin ?? '';
  }

  /** `null` when storage is unavailable or blocked by the browser. */
  storage(kind: 'local' | 'session'): Storage | null {
    if (!this.isBrowser) {
      return null;
    }
    try {
      const storage = kind === 'local' ? this.window?.localStorage : this.window?.sessionStorage;
      // Safari in private mode exposes the API but throws on write.
      const probe = '__app_storage_probe__';
      storage?.setItem(probe, '1');
      storage?.removeItem(probe);
      return storage ?? null;
    } catch {
      return null;
    }
  }

  matchMedia(query: string): MediaQueryList | null {
    return typeof this.window?.matchMedia === 'function' ? this.window.matchMedia(query) : null;
  }

  /** Cryptographically secure randomness, required by the PKCE implementation. */
  get crypto(): Crypto | null {
    return this.isBrowser ? (this.window?.crypto ?? null) : null;
  }

  navigateTo(url: string): void {
    this.location?.assign(url);
  }

  replaceUrl(url: string): void {
    this.window?.history.replaceState({}, '', url);
  }
}
