/**
 * Pagination contracts shared by every list screen.
 *
 * Modelled on Spring Data's `Page`, the most common backend shape in the
 * company's stack. Adapt `toPage()` if your API differs.
 */

export interface PageRequest {
  /** Zero-based page index. */
  page: number;
  size: number;
  /** Property to sort by. */
  sort?: string;
  direction?: 'asc' | 'desc';
}

export interface Page<T> {
  content: T[];
  totalElements: number;
  totalPages: number;
  /** Zero-based index of the current page. */
  number: number;
  size: number;
  first: boolean;
  last: boolean;
}

export const DEFAULT_PAGE_SIZE = 20;
export const PAGE_SIZE_OPTIONS: readonly number[] = [10, 20, 50, 100];

export function emptyPage<T>(size = DEFAULT_PAGE_SIZE): Page<T> {
  return {
    content: [],
    totalElements: 0,
    totalPages: 0,
    number: 0,
    size,
    first: true,
    last: true,
  };
}

/** Converts a `PageRequest` into query parameters. */
export function toPageParams(request: PageRequest): Record<string, string | number> {
  const params: Record<string, string | number> = {
    page: request.page,
    size: request.size,
  };
  if (request.sort) {
    params['sort'] = `${request.sort},${request.direction ?? 'asc'}`;
  }
  return params;
}
