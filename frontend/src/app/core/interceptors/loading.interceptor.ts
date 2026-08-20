import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { SKIP_LOADING } from '../http/http-context.tokens';
import { LoadingService } from '../services/loading.service';

/**
 * Feeds the shell's progress bar.
 *
 * `finalize` runs on success, error *and* unsubscription, so a cancelled
 * request (a typeahead the user outran) never leaves the indicator stuck.
 */
export const loadingInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(SKIP_LOADING)) {
    return next(request);
  }

  const loading = inject(LoadingService);
  loading.start();

  return next(request).pipe(finalize(() => loading.stop()));
};
