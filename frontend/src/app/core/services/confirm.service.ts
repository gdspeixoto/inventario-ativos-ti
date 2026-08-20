import { Injectable, inject } from '@angular/core';
import { ConfirmationService } from 'primeng/api';
import { TranslationService } from '../i18n/translation.service';

export interface ConfirmOptions {
  /** Body text, or an i18n key. */
  message: string;
  /** Dialog title, or an i18n key. */
  header?: string;
  icon?: string;
  acceptLabel?: string;
  rejectLabel?: string;
  /** Styles the accept button as destructive. */
  destructive?: boolean;
  /** Skips translation and uses the strings verbatim. */
  raw?: boolean;
}

/**
 * Promise-based confirmation dialogs.
 *
 * PrimeNG's `ConfirmationService` is callback-driven; awaiting a boolean keeps
 * call sites linear:
 *
 * ```ts
 * if (await this.confirm.ask({ message: 'users.deleteConfirm', destructive: true })) {
 *   await this.remove(user);
 * }
 * ```
 *
 * Requires a single `<p-confirmDialog>` in the shell.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly confirmation = inject(ConfirmationService);
  private readonly i18n = inject(TranslationService);

  ask(options: ConfirmOptions): Promise<boolean> {
    const t = (value: string | undefined, fallback: string): string => {
      const key = value ?? fallback;
      return options.raw ? key : this.i18n.translate(key);
    };

    return new Promise((resolve) => {
      this.confirmation.confirm({
        message: t(options.message, options.message),
        header: t(options.header, 'common.confirm'),
        icon:
          options.icon ??
          (options.destructive ? 'pi pi-exclamation-triangle' : 'pi pi-question-circle'),
        acceptLabel: t(options.acceptLabel, 'common.yes'),
        rejectLabel: t(options.rejectLabel, 'common.no'),
        acceptButtonStyleClass: options.destructive ? 'p-button-danger' : '',
        rejectButtonStyleClass: 'p-button-text',
        accept: () => resolve(true),
        reject: () => resolve(false),
      });
    });
  }

  /** Shorthand for the most common case: confirming a destructive action. */
  askDelete(itemName?: string): Promise<boolean> {
    return this.ask({
      message: itemName
        ? this.i18n.translate('common.deleteConfirmNamed', { name: itemName })
        : 'common.deleteConfirm',
      header: 'common.confirmDelete',
      destructive: true,
      raw: !!itemName,
    });
  }
}
