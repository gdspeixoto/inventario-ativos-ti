import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { BreakpointService } from '../services/breakpoint.service';
import { StorageService } from '../services/storage.service';

const STORAGE_KEY = 'layout.sidebarCollapsed';

/**
 * Chrome state: sidebar, mobile drawer and page title.
 *
 * Two distinct concepts, deliberately not merged:
 *  - `collapsed` — desktop rail mode, a persisted user preference;
 *  - `mobileOpen` — the overlay drawer, transient and never persisted.
 */
@Injectable({ providedIn: 'root' })
export class LayoutStore {
  private readonly storage = inject(StorageService);
  private readonly breakpoints = inject(BreakpointService);

  private readonly _collapsed = signal(this.storage.get<boolean>(STORAGE_KEY) ?? false);
  private readonly _mobileOpen = signal(false);
  private readonly _pageTitle = signal<string | null>(null);

  readonly collapsed = this._collapsed.asReadonly();
  readonly mobileOpen = this._mobileOpen.asReadonly();
  readonly pageTitle = this._pageTitle.asReadonly();

  readonly isMobile = this.breakpoints.isMobile;

  /** The rail only exists on desktop; on mobile the drawer takes over. */
  readonly isRail = computed(() => this._collapsed() && !this.isMobile());
  readonly isSidebarVisible = computed(() => !this.isMobile() || this._mobileOpen());

  constructor() {
    // Leaving a mobile viewport must not strand an open overlay drawer.
    effect(() => {
      if (!this.isMobile() && this._mobileOpen()) {
        this._mobileOpen.set(false);
      }
    });
  }

  toggleSidebar(): void {
    if (this.isMobile()) {
      this._mobileOpen.update((open) => !open);
      return;
    }
    this._collapsed.update((collapsed) => {
      this.storage.set(STORAGE_KEY, !collapsed);
      return !collapsed;
    });
  }

  closeMobileSidebar(): void {
    this._mobileOpen.set(false);
  }

  setPageTitle(title: string | null): void {
    this._pageTitle.set(title);
  }
}
