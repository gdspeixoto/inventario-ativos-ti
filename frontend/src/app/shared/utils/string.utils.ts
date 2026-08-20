/**
 * String helpers shared across layers.
 *
 * Pure functions only — no Angular, no DI — so they are usable from models,
 * pipes and services alike.
 */

/**
 * Reduces a name to at most two initials.
 *
 * `"Gabriel dos Santos"` → `"GS"` · `"ana.souza@empresa.com.br"` → `"AB"`
 *
 * Splits on whitespace and on the separators common in usernames and emails,
 * so it degrades sensibly when only a login is available.
 */
export function initialsOf(value: string | null | undefined): string {
  const source = value?.trim();
  if (!source) {
    return '?';
  }

  const parts = source.split(/[\s@._-]+/u).filter(Boolean);
  const first = parts[0]?.charAt(0) ?? '';
  const last = parts.length > 1 ? (parts[parts.length - 1]?.charAt(0) ?? '') : '';
  return (first + last).toUpperCase() || '?';
}

/** Truncates to `max` characters, appending an ellipsis when it had to cut. */
export function truncate(value: string, max: number): string {
  return value.length <= max ? value : `${value.slice(0, Math.max(0, max - 1))}…`;
}

/** Removes diacritics, for accent-insensitive search and sorting. */
export function deaccent(value: string): string {
  return value.normalize('NFD').replace(/\p{Diacritic}/gu, '');
}

/** Case- and accent-insensitive "contains", suitable for client-side filters. */
export function looseIncludes(haystack: string, needle: string): boolean {
  return deaccent(haystack).toLowerCase().includes(deaccent(needle).toLowerCase().trim());
}
