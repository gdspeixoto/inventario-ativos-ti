import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError, timer } from 'rxjs';
import { retry } from 'rxjs/operators';
import { AUTH_CONFIG } from '../authentication/auth.tokens';
import { RETRY_COUNT, SKIP_ERROR_TOAST } from '../http/http-context.tokens';
import { LoggerService } from '../services/logger.service';
import { ToastService } from '../services/toast.service';
import { toAppHttpError } from '../errors/http-error.model';

/** Statuses worth retrying: the request may succeed unchanged moments later. */
const RETRYABLE_STATUSES = new Set([0, 408, 429, 502, 503, 504]);
const RETRY_BASE_DELAY_MS = 400;

/**
 * Turns transport failures into a consistent, user-visible outcome.
 *
 *  - retries idempotent transient failures with exponential backoff
 *    (opt in per request with the `RETRY_COUNT` context token);
 *  - shows one toast describing the failure, unless `SKIP_ERROR_TOAST` is set;
 *  - routes 403 to the "forbidden" page — the user is signed in but not allowed;
 *  - rethrows a normalized `AppHttpError` so callers can still react.
 *
 * 401 is *not* handled here: `authInterceptor` owns it, because a 401 is a
 * session problem rather than a request problem.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);
  const authConfig = inject(AUTH_CONFIG);
  const logger = inject(LoggerService).forContext('http');

  const maxRetries = request.context.get(RETRY_COUNT);

  return next(request).pipe(
    retry({
      count: maxRetries,
      delay: (error: unknown, retryCount: number) => {
        const status = error instanceof HttpErrorResponse ? error.status : -1;
        if (!RETRYABLE_STATUSES.has(status)) {
          return throwError(() => error);
        }
        return timer(RETRY_BASE_DELAY_MS * 2 ** (retryCount - 1));
      },
    }),
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const appError = toAppHttpError(error);
      logger.error(`${request.method} ${appError.url} → ${appError.status}`, appError.message);

      if (appError.status === 403) {
        void router.navigate([authConfig.routes.forbidden]);
      }

      if (appError.status !== 401 && !request.context.get(SKIP_ERROR_TOAST)) {
        toast.error(appError.translationKey);
      }

      return throwError(() => appError);
    }),
  );
};
