import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslationService } from '@core/i18n/translation.service';

const UNITS: readonly { unit: Intl.RelativeTimeFormatUnit; ms: number }[] = [
  { unit: 'year', ms: 365 * 24 * 60 * 60 * 1000 },
  { unit: 'month', ms: 30 * 24 * 60 * 60 * 1000 },
  { unit: 'day', ms: 24 * 60 * 60 * 1000 },
  { unit: 'hour', ms: 60 * 60 * 1000 },
  { unit: 'minute', ms: 60 * 1000 },
  { unit: 'second', ms: 1000 },
];

/**
 * Relative time using the platform's `Intl.RelativeTimeFormat`, so the wording
 * follows the active locale without shipping a translation table.
 *
 * Impure so the label updates when the language changes.
 */
@Pipe({ name: 'timeAgo', pure: false })
export class TimeAgoPipe implements PipeTransform {
  private readonly i18n = inject(TranslationService);

  transform(value: Date | string | number | null | undefined): string {
    if (value === null || value === undefined) {
      return '';
    }

    const timestamp = value instanceof Date ? value.getTime() : new Date(value).getTime();
    if (Number.isNaN(timestamp)) {
      return '';
    }

    const deltaMs = timestamp - Date.now();
    const absolute = Math.abs(deltaMs);
    const match = UNITS.find((entry) => absolute >= entry.ms) ?? UNITS[UNITS.length - 1];

    const formatter = new Intl.RelativeTimeFormat(this.i18n.currentLocale().intlLocale, {
      numeric: 'auto',
    });
    return formatter.format(Math.round(deltaMs / match.ms), match.unit);
  }
}
