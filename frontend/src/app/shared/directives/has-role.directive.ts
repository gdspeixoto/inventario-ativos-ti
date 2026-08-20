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
 * Structural directive that renders content only for the given roles.
 *
 * ```html
 * <button *appHasRole="'admin'">Delete</button>
 * <section *appHasRole="['admin', 'auditor']">…</section>
 * <section *appHasRole="['admin', 'auditor']; match: 'all'">…</section>
 * ```
 *
 * A convenience for the UI — never a security control. The route guards and the
 * API are what actually enforce access.
 */
@Directive({ selector: '[appHasRole]' })
export class HasRoleDirective {
  private readonly auth = inject(AuthService);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly appHasRole = input.required<string | readonly string[]>();
  readonly appHasRoleMatch = input<'any' | 'all'>('any');

  private readonly allowed = computed(() => {
    const value = this.appHasRole();
    const roles = typeof value === 'string' ? [value] : value;
    if (roles.length === 0) {
      return true;
    }
    return this.appHasRoleMatch() === 'all'
      ? this.auth.hasAllRoles(roles)
      : this.auth.hasAnyRole(roles);
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
