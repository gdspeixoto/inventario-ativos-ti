import { ErrorHandler, Injectable, NgZone, inject } from '@angular/core';
import { LoggerService } from '../services/logger.service';
import { ToastService } from '../services/toast.service';
import { isAppHttpError } from './http-error.model';

/**
 * Last line of defence for uncaught errors.
 *
 * HTTP failures are already reported by `errorInterceptor`, so they are logged
 * here but never toasted twice. Everything else — a template expression that
 * threw, a rejected promise nobody awaited — surfaces one generic toast, since
 * a raw stack trace means nothing to the user.
 *
 * This is where an observability SDK (Sentry, Application Insights, Faro) is
 * wired in: one `capture(error)` call in `handleError`.
 */
@Injectable()
export class AppErrorHandler implements ErrorHandler {
  private readonly logger = inject(LoggerService).forContext('unhandled');
  private readonly toast = inject(ToastService);
  private readonly zone = inject(NgZone);

  /** Prevents an error loop from flooding the screen with toasts. */
  private lastToastAt = 0;
  private static readonly TOAST_THROTTLE_MS = 3_000;

  handleError(error: unknown): void {
    const unwrapped = unwrap(error);

    if (isAppHttpError(unwrapped)) {
      this.logger.debug('HTTP error already handled by the interceptor.', unwrapped);
      return;
    }

    this.logger.error('Uncaught error', unwrapped);

    // Chunk load failures mean a new version was deployed mid-session.
    if (isChunkLoadError(unwrapped)) {
      this.notify('errors.http.503');
      return;
    }

    this.notify('errors.http.unknown');
  }

  private notify(translationKey: string): void {
    const now = Date.now();
    if (now - this.lastToastAt < AppErrorHandler.TOAST_THROTTLE_MS) {
      return;
    }
    this.lastToastAt = now;
    // Errors can escape outside Angular's zone; re-enter so the toast renders.
    this.zone.run(() => this.toast.error(translationKey));
  }
}

/** Angular wraps errors thrown inside effects/promises in a `rejection` field. */
function unwrap(error: unknown): unknown {
  if (error && typeof error === 'object' && 'rejection' in error) {
    return (error as { rejection: unknown }).rejection;
  }
  return error;
}

function isChunkLoadError(error: unknown): boolean {
  const message = error instanceof Error ? error.message : String(error);
  return /ChunkLoadError|Loading chunk .* failed|dynamically imported module/iu.test(message);
}
