import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslationService } from './translation.service';
import type { TranslationParams } from './i18n.model';

/**
 * Translates a key in a template: `{{ 'dashboard.welcome' | translate }}`.
 *
 * Declared impure so a language switch repaints existing views. The lookup is a
 * single object access, so the cost is negligible; heavy templates can use
 * `TranslationService.translateSignal()` instead.
 */
@Pipe({ name: 'translate', pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(TranslationService);

  transform(key: string | null | undefined, params?: TranslationParams): string {
    if (!key) {
      return '';
    }
    return this.i18n.translate(key, params);
  }
}
