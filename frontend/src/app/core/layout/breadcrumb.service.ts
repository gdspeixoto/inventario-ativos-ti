import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  type ActivatedRouteSnapshot,
} from '@angular/router';
import { filter } from 'rxjs';
import type { BreadcrumbItem } from './menu.model';

/**
 * Builds the breadcrumb trail from route metadata.
 *
 * A route opts in by declaring `data.breadcrumb`:
 *
 * ```ts
 * { path: 'users', data: { breadcrumb: 'nav.users' }, ... }
 * ```
 *
 * Routes without the key are skipped, so layout-only parents (`''`, `:id`
 * wrappers) do not pollute the trail. The value is an i18n key, resolved by the
 * component at render time.
 */
@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly _items = signal<BreadcrumbItem[]>([]);
  readonly items = this._items.asReadonly();

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this._items.set(this.build(this.route.snapshot)));
  }

  /** Lets a page append a dynamic leaf, e.g. the name of the record it loaded. */
  append(item: BreadcrumbItem): void {
    this._items.update((items) => [...items, item]);
  }

  private build(root: ActivatedRouteSnapshot): BreadcrumbItem[] {
    const items: BreadcrumbItem[] = [];
    let segments: string[] = [];
    let current: ActivatedRouteSnapshot | null = root;

    while (current) {
      const path = current.url.map((segment) => segment.path).filter(Boolean);
      segments = [...segments, ...path];

      const label = current.data['breadcrumb'] as string | undefined;
      if (label) {
        items.push({
          label,
          route: `/${segments.join('/')}`,
          icon: current.data['breadcrumbIcon'] as string | undefined,
        });
      }
      current = current.firstChild;
    }

    return items;
  }
}
