import {
  HttpClient,
  HttpContext,
  HttpEvent,
  HttpEventType,
  HttpHeaders,
  HttpParams,
} from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, filter, firstValueFrom, map } from 'rxjs';
import { PlatformService } from './platform.service';
import { RETRY_COUNT, SKIP_ERROR_TOAST, SKIP_LOADING } from '../http/http-context.tokens';

/** Query string values accepted by the request helpers. */
export type QueryParams = Record<
  string,
  string | number | boolean | readonly (string | number | boolean)[] | null | undefined
>;

export interface RequestOptions {
  params?: QueryParams;
  headers?: Record<string, string>;
  /** Suppresses the automatic error toast so the caller can render the error. */
  silent?: boolean;
  /** Hides this request from the global progress bar. */
  background?: boolean;
  /** Retries on transient failures (network, 429, 5xx). */
  retries?: number;
  context?: HttpContext;
}

export interface UploadProgress {
  /** 0–100. */
  readonly percent: number;
  readonly loaded: number;
  readonly total: number;
  readonly done: boolean;
}

/**
 * Typed façade over `HttpClient`.
 *
 * Feature services depend on `ApiService`, not on `HttpClient`, which gives one
 * place to standardise URL building, query serialization, upload progress and
 * per-request behaviour flags. Cross-cutting concerns (auth, errors, loading)
 * stay in the interceptors.
 *
 * ```ts
 * const page = await this.api.get<Page<User>>('/users', { params: { page: 0 } });
 * ```
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly platform = inject(PlatformService);

  get<T>(url: string, options: RequestOptions = {}): Promise<T> {
    return firstValueFrom(this.get$<T>(url, options));
  }

  post<T>(url: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
    return firstValueFrom(this.post$<T>(url, body, options));
  }

  put<T>(url: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
    return firstValueFrom(this.put$<T>(url, body, options));
  }

  patch<T>(url: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
    return firstValueFrom(this.patch$<T>(url, body, options));
  }

  delete<T>(url: string, options: RequestOptions = {}): Promise<T> {
    return firstValueFrom(this.delete$<T>(url, options));
  }

  // ----------------------------------------------------------- Observable API
  // Kept for streams that genuinely benefit from RxJS: typeahead, polling,
  // cancellation. Prefer the promise-based methods everywhere else.

  get$<T>(url: string, options: RequestOptions = {}): Observable<T> {
    return this.http.get<T>(url, this.buildOptions(options));
  }

  post$<T>(url: string, body?: unknown, options: RequestOptions = {}): Observable<T> {
    return this.http.post<T>(url, body ?? null, this.buildOptions(options));
  }

  put$<T>(url: string, body?: unknown, options: RequestOptions = {}): Observable<T> {
    return this.http.put<T>(url, body ?? null, this.buildOptions(options));
  }

  patch$<T>(url: string, body?: unknown, options: RequestOptions = {}): Observable<T> {
    return this.http.patch<T>(url, body ?? null, this.buildOptions(options));
  }

  delete$<T>(url: string, options: RequestOptions = {}): Observable<T> {
    return this.http.delete<T>(url, this.buildOptions(options));
  }

  // ------------------------------------------------------------------ Upload

  /**
   * Uploads files as `multipart/form-data`, emitting progress updates.
   * The `Content-Type` header is intentionally omitted so the browser can set
   * the multipart boundary itself.
   */
  upload(
    url: string,
    files: File | readonly File[],
    options: RequestOptions & { fieldName?: string; extra?: Record<string, string> } = {},
  ): Observable<UploadProgress> {
    const form = new FormData();
    const fieldName = options.fieldName ?? 'file';
    const list = Array.isArray(files) ? files : [files as File];
    list.forEach((file) => form.append(fieldName, file, file.name));
    Object.entries(options.extra ?? {}).forEach(([key, value]) => form.append(key, value));

    return this.http
      .post(url, form, {
        ...this.buildOptions(options),
        observe: 'events',
        reportProgress: true,
      })
      .pipe(
        filter(
          (event) =>
            event.type === HttpEventType.UploadProgress || event.type === HttpEventType.Response,
        ),
        map((event) => toUploadProgress(event)),
      );
  }

  // ---------------------------------------------------------------- Download

  /** Fetches a binary payload and triggers the browser's save dialog. */
  async download(url: string, fileName: string, options: RequestOptions = {}): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(url, { ...this.buildOptions(options), responseType: 'blob' }),
    );
    this.saveBlob(blob, fileName);
  }

  /** Returns the raw `Blob` for callers that render it instead of saving it. */
  getBlob(url: string, options: RequestOptions = {}): Promise<Blob> {
    return firstValueFrom(
      this.http.get(url, { ...this.buildOptions(options), responseType: 'blob' }),
    );
  }

  saveBlob(blob: Blob, fileName: string): void {
    const window = this.platform.window;
    if (!window) {
      return;
    }
    const objectUrl = URL.createObjectURL(blob);
    const anchor = this.platform.document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.click();
    // Revoking immediately would cancel the download in Firefox.
    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1_000);
  }

  // ------------------------------------------------------------------ Private

  private buildOptions(options: RequestOptions) {
    let context = options.context ?? new HttpContext();
    if (options.silent) {
      context = context.set(SKIP_ERROR_TOAST, true);
    }
    if (options.background) {
      context = context.set(SKIP_LOADING, true);
    }
    if (options.retries) {
      context = context.set(RETRY_COUNT, options.retries);
    }

    return {
      params: toHttpParams(options.params),
      headers: options.headers ? new HttpHeaders(options.headers) : undefined,
      context,
    };
  }
}

/** Drops `null`/`undefined` and expands arrays into repeated keys. */
function toHttpParams(params?: QueryParams): HttpParams | undefined {
  if (!params) {
    return undefined;
  }
  return Object.entries(params).reduce((httpParams, [key, value]) => {
    if (value === null || value === undefined) {
      return httpParams;
    }
    if (Array.isArray(value)) {
      return value.reduce<HttpParams>(
        (accumulator, entry) => accumulator.append(key, String(entry)),
        httpParams,
      );
    }
    return httpParams.set(key, String(value));
  }, new HttpParams());
}

function toUploadProgress(event: HttpEvent<unknown>): UploadProgress {
  if (event.type === HttpEventType.UploadProgress) {
    const total = event.total ?? 0;
    return {
      loaded: event.loaded,
      total,
      percent: total > 0 ? Math.round((event.loaded / total) * 100) : 0,
      done: false,
    };
  }
  return { loaded: 0, total: 0, percent: 100, done: true };
}
