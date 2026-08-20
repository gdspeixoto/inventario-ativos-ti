import { Pipe, PipeTransform } from '@angular/core';
import { initialsOf } from '@shared/utils/string.utils';

/**
 * Reduces a name to at most two initials: `"Gabriel dos Santos"` → `"GS"`.
 *
 * Thin wrapper over `initialsOf()` so the same rule serves templates and the
 * `User` model without drifting.
 */
@Pipe({ name: 'initials' })
export class InitialsPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return initialsOf(value);
  }
}
