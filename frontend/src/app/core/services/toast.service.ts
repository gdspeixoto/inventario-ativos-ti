import { Injectable, inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { TranslationService } from '../i18n/translation.service';

export type ToastSeverity = 'success' | 'info' | 'warn' | 'error' | 'secondary' | 'contrast';

export interface ToastOptions {
  /** Milliseconds before auto-dismiss. `0` keeps the toast until closed. */
  life?: number;
  /** Multiple `<p-toast>` outlets can coexist; targets one by key. */
  key?: string;
  /** Prevents duplicates of the same notification stacking up. */
  id?: string;
  /** Skips translation and shows the strings verbatim. */
  raw?: boolean;
}

const DEFAULT_LIFE_MS = 5_000;
const ERROR_LIFE_MS = 8_000;

/**
 * Application-facing notification API.
 *
 * Wraps PrimeNG's `MessageService` so components never depend on it directly:
 * messages are translated by default and severities carry sensible lifetimes.
 * Requires a single `<p-toast>` in the shell.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly messages = inject(MessageService);
  private readonly i18n = inject(TranslationService);

  success(detail: string, summary = 'common.success', options?: ToastOptions): void {
    this.show('success', summary, detail, options);
  }

  info(detail: string, summary = 'common.info', options?: ToastOptions): void {
    this.show('info', summary, detail, options);
  }

  warn(detail: string, summary = 'common.warning', options?: ToastOptions): void {
    this.show('warn', summary, detail, options);
  }

  error(detail: string, summary = 'common.error', options?: ToastOptions): void {
    this.show('error', summary, detail, { life: ERROR_LIFE_MS, ...options });
  }

  show(severity: ToastSeverity, summary: string, detail: string, options: ToastOptions = {}): void {
    this.messages.add({
      severity,
      summary: options.raw ? summary : this.i18n.translate(summary),
      detail: options.raw ? detail : this.i18n.translate(detail),
      life: options.life ?? DEFAULT_LIFE_MS,
      key: options.key,
      id: options.id,
    });
  }

  clear(key?: string): void {
    this.messages.clear(key);
  }
}
