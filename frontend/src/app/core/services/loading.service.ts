import { Injectable, computed, signal } from '@angular/core';

/**
 * Global in-flight request counter.
 *
 * Incremented by `loadingInterceptor` and read by the shell's progress bar.
 * A counter — not a boolean — so overlapping requests do not hide the indicator
 * when the first one finishes.
 */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly pending = signal(0);

  readonly isLoading = computed(() => this.pending() > 0);
  readonly pendingCount = this.pending.asReadonly();

  start(): void {
    this.pending.update((count) => count + 1);
  }

  stop(): void {
    this.pending.update((count) => Math.max(0, count - 1));
  }

  /** Escape hatch for navigation errors that could otherwise strand the counter. */
  reset(): void {
    this.pending.set(0);
  }
}
