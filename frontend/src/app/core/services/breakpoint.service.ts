import { BreakpointObserver } from '@angular/cdk/layout';
import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

/**
 * Responsive breakpoints as signals.
 *
 * Values match the Tailwind scale used in templates, so a component and its
 * stylesheet never disagree about what "mobile" means.
 */
export const BREAKPOINTS = {
  sm: '(min-width: 40rem)', // 640px
  md: '(min-width: 48rem)', // 768px
  lg: '(min-width: 64rem)', // 1024px
  xl: '(min-width: 80rem)', // 1280px
} as const;

@Injectable({ providedIn: 'root' })
export class BreakpointService {
  private readonly observer = inject(BreakpointObserver);

  private readonly state = toSignal(
    this.observer.observe(Object.values(BREAKPOINTS)).pipe(map((result) => result.breakpoints)),
    { initialValue: {} as Record<string, boolean> },
  );

  readonly isSmUp = computed(() => this.state()[BREAKPOINTS.sm] ?? false);
  readonly isMdUp = computed(() => this.state()[BREAKPOINTS.md] ?? false);
  readonly isLgUp = computed(() => this.state()[BREAKPOINTS.lg] ?? false);
  readonly isXlUp = computed(() => this.state()[BREAKPOINTS.xl] ?? false);

  /** Below `md`: the sidebar becomes an overlay drawer. */
  readonly isMobile = computed(() => !this.isMdUp());
  readonly isTablet = computed(() => this.isMdUp() && !this.isLgUp());
  readonly isDesktop = computed(() => this.isLgUp());
}
