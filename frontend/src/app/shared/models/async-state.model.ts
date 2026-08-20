import { computed, signal, type Signal, type WritableSignal } from '@angular/core';
import type { AppHttpError } from '@core/errors/http-error.model';

/**
 * The four states every asynchronous screen can be in.
 *
 * Making them explicit is what lets `LoadingComponent`, `EmptyStateComponent`
 * and `ErrorStateComponent` be reused everywhere instead of each page
 * reinventing its own `isLoading` / `hasError` booleans.
 */
export type AsyncStatus = 'idle' | 'loading' | 'success' | 'error';

export type AsyncError = AppHttpError | Error;

export interface AsyncState<T> {
  status: AsyncStatus;
  data: T | null;
  error: AsyncError | null;
}

export function idleState<T>(): AsyncState<T> {
  return { status: 'idle', data: null, error: null };
}

export interface AsyncSignal<T> {
  readonly state: Signal<AsyncState<T>>;
  readonly data: Signal<T | null>;
  readonly isLoading: Signal<boolean>;
  readonly isEmpty: Signal<boolean>;
  readonly hasError: Signal<boolean>;
  readonly error: Signal<AsyncError | null>;
  /** Runs the loader, moving through loading → success | error. */
  run(loader: () => Promise<T>): Promise<T | null>;
  set(data: T): void;
  reset(): void;
}

/**
 * Wraps a promise-returning loader in the four-state machine above.
 *
 * ```ts
 * readonly users = asyncSignal<User[]>();
 * ngOnInit(): void {
 *   void this.users.run(() => this.api.get<User[]>('/users'));
 * }
 * ```
 * ```html
 * @if (users.isLoading()) { <app-loading /> }
 * @else if (users.hasError()) { <app-error-state (retry)="reload()" /> }
 * @else if (users.isEmpty()) { <app-empty-state /> }
 * ```
 */
export function asyncSignal<T>(): AsyncSignal<T> {
  const state: WritableSignal<AsyncState<T>> = signal(idleState<T>());

  const isEmptyValue = (data: T | null): boolean =>
    data === null || (Array.isArray(data) && data.length === 0);

  return {
    state: state.asReadonly(),
    data: computed(() => state().data),
    isLoading: computed(() => state().status === 'loading'),
    isEmpty: computed(() => state().status === 'success' && isEmptyValue(state().data)),
    hasError: computed(() => state().status === 'error'),
    error: computed(() => state().error),

    async run(loader) {
      // Keep the previous data visible while reloading — avoids a layout jump.
      state.set({ status: 'loading', data: state().data, error: null });
      try {
        const data = await loader();
        state.set({ status: 'success', data, error: null });
        return data;
      } catch (error) {
        state.set({ status: 'error', data: null, error: error as AsyncError });
        return null;
      }
    },

    set(data) {
      state.set({ status: 'success', data, error: null });
    },

    reset() {
      state.set(idleState<T>());
    },
  };
}
