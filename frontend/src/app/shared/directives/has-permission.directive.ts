import {
  Directive,
  TemplateRef,
  ViewContainerRef,
  computed,
  effect,
  inject,
  input,
} from '@angular/core';
import { AuthService } from '@core/authentication/auth.service';

/**
 * Structural directive that renders content only for the given permissions.
 *
 * ```html
 * <button *appHasPermission="'user:delete'">Delete</button>
 * <div *appHasPermission="['report:read', 'report:export']; match: 'all'">…</div>
 * ```
 *
 * Prefer this over `*appHasRole` for feature-level checks: permissions survive
 * role renames and map onto the backend's authorization scopes.
 */
@Directive({ selector: '[appHasPermission]' })
export class HasPermissionDirective {
  private readonly auth = inject(AuthService);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly appHasPermission = input.required<string | readonly string[]>();
  readonly appHasPermissionMatch = input<'any' | 'all'>('any');

  private readonly allowed = computed(() => {
    const value = this.appHasPermission();
    const permissions = typeof value === 'string' ? [value] : value;
    if (permissions.length === 0) {
      return true;
    }
    return this.appHasPermissionMatch() === 'all'
      ? permissions.every((permission) => this.auth.hasPermission(permission))
      : this.auth.hasAnyPermission(permissions);
  });

  private rendered = false;

  constructor() {
    effect(() => {
      const allowed = this.allowed();
      if (allowed && !this.rendered) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.viewContainer.clear();
        this.rendered = false;
      }
    });
  }
}
