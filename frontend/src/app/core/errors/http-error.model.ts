import { HttpErrorResponse } from '@angular/common/http';

/**
 * Normalized HTTP failure.
 *
 * Backends disagree on error envelopes (`message`, `error`, `detail`,
 * RFC 7807 `title`/`detail`, Spring's `errors[]`). Everything is funnelled into
 * this shape so components and the global handler read one contract.
 */
export interface AppHttpError {
  /** HTTP status, or `0` for network/CORS failures. */
  readonly status: number;
  /** Machine-readable code from the backend, when provided. */
  readonly code: string | null;
  /** Human-readable message, already suitable for display. */
  readonly message: string;
  /** Field-level validation errors, keyed by field name. */
  readonly fieldErrors: Readonly<Record<string, string[]>>;
  /** Correlation id for support tickets (`traceId`, `X-Trace-Id`, ...). */
  readonly traceId: string | null;
  readonly url: string | null;
  /** i18n key for a generic message, used when the backend sends none. */
  readonly translationKey: string;
}

interface ErrorEnvelope {
  message?: string;
  error?: string | { message?: string };
  detail?: string;
  title?: string;
  code?: string;
  errorCode?: string;
  traceId?: string;
  errors?: Record<string, string[]> | { field: string; message: string }[];
  violations?: { field: string; message: string }[];
}

/** Statuses that carry a dedicated translation under `errors.http.*`. */
const KNOWN_STATUSES = new Set([0, 400, 401, 403, 404, 409, 422, 429, 500, 503]);

export function toAppHttpError(response: HttpErrorResponse): AppHttpError {
  const envelope = extractEnvelope(response);
  const status = response.status;

  return Object.freeze({
    status,
    code: envelope?.code ?? envelope?.errorCode ?? null,
    message: extractMessage(envelope, response),
    fieldErrors: Object.freeze(extractFieldErrors(envelope)),
    traceId: envelope?.traceId ?? response.headers?.get('X-Trace-Id') ?? null,
    url: response.url,
    translationKey: KNOWN_STATUSES.has(status) ? `errors.http.${status}` : 'errors.http.unknown',
  });
}

export function isAppHttpError(value: unknown): value is AppHttpError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'status' in value &&
    'translationKey' in value &&
    'fieldErrors' in value
  );
}

function extractEnvelope(response: HttpErrorResponse): ErrorEnvelope | null {
  const body = response.error;
  if (!body || typeof body !== 'object' || body instanceof ProgressEvent) {
    return null;
  }
  return body as ErrorEnvelope;
}

function extractMessage(envelope: ErrorEnvelope | null, response: HttpErrorResponse): string {
  if (!envelope) {
    return response.message || 'Request failed.';
  }
  if (typeof envelope.error === 'string') {
    return envelope.error;
  }
  return (
    envelope.message ??
    envelope.detail ??
    envelope.title ??
    (typeof envelope.error === 'object' ? envelope.error?.message : undefined) ??
    response.message ??
    'Request failed.'
  );
}

function extractFieldErrors(envelope: ErrorEnvelope | null): Record<string, string[]> {
  const source = envelope?.errors ?? envelope?.violations;
  if (!source) {
    return {};
  }

  // Spring / Jakarta style: [{ field, message }]
  if (Array.isArray(source)) {
    return source.reduce<Record<string, string[]>>((accumulator, violation) => {
      if (typeof violation === 'object' && 'field' in violation) {
        (accumulator[violation.field] ??= []).push(violation.message);
      }
      return accumulator;
    }, {});
  }

  // Map style: { field: ['message'] }
  return Object.entries(source).reduce<Record<string, string[]>>((accumulator, [field, value]) => {
    accumulator[field] = Array.isArray(value) ? value.map(String) : [String(value)];
    return accumulator;
  }, {});
}
