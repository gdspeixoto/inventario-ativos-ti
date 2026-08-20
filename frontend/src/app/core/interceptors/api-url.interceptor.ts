import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '@env/environment';
import { SKIP_API_PREFIX } from '../http/http-context.tokens';

/** Absolute URLs and template assets bypass the API prefix. */
const ABSOLUTE_URL = /^(https?:)?\/\//iu;

/**
 * Prefixes relative URLs with `environment.apiBaseUrl`.
 *
 * Services can therefore call `api.get('/users')` and stay environment-agnostic.
 * Opt out per request with the `SKIP_API_PREFIX` context token.
 */
export const apiUrlInterceptor: HttpInterceptorFn = (request, next) => {
  const isAbsolute = ABSOLUTE_URL.test(request.url);
  if (isAbsolute || request.context.get(SKIP_API_PREFIX)) {
    return next(request);
  }

  const base = environment.apiBaseUrl.replace(/\/+$/u, '');
  const path = request.url.startsWith('/') ? request.url : `/${request.url}`;

  return next(request.clone({ url: `${base}${path}` }));
};
