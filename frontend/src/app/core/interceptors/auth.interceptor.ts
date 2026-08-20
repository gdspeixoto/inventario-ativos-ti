import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { EMPTY, catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from '../authentication/auth.service';
import { SKIP_AUTH } from '../http/http-context.tokens';

/**
 * Attaches the Bearer token and keeps the session alive.
 *
 * Flow per request:
 *   1. skip entirely when `SKIP_AUTH` is set (identity-provider calls, assets);
 *   2. ask `AuthService` for a *valid* token — it refreshes proactively when the
 *      current one is inside the expiry window;
 *   3. on a 401 that slipped through (server-side revocation, clock drift),
 *      refresh once and replay the request;
 *   4. if the refresh fails, end the session and send the user to the login page.
 *
 * Concurrent 401s share one refresh: `AuthService.refresh()` de-duplicates them.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(SKIP_AUTH)) {
    return next(request);
  }

  const auth = inject(AuthService);

  return from(auth.getValidAccessToken()).pipe(
    switchMap((token) => next(withBearer(request, token))),
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return from(auth.refresh()).pipe(
        switchMap((tokens) => {
          if (!tokens) {
            // The session is gone: `refresh()` already redirected to the login page.
            return EMPTY;
          }
          return next(withBearer(request, tokens.accessToken));
        }),
      );
    }),
  );
};

function withBearer(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) {
    return request;
  }
  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
