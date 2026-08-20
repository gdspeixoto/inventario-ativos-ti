import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '@core/authentication/auth.service';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import type { CallbackParams } from '@core/authentication/providers/auth-provider';

/**
 * Landing route for the identity provider redirect (`/auth/callback`).
 *
 * Deliberately dumb: it forwards the query string to `AuthService` and shows a
 * spinner. All the decisions — state validation, token exchange, where to go
 * next, what to do on failure — live in the authentication library.
 *
 * The route is registered without a guard: the user is, by definition, not yet
 * authenticated when they arrive here.
 */
@Component({
  selector: 'app-auth-callback',
  imports: [LoadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="callback">
      <app-loading message="auth.completing" size="lg" />
    </div>
  `,
  styles: `
    .callback {
      display: grid;
      min-height: 100vh;
      min-height: 100dvh;
      place-items: center;
      background-color: var(--app-color-background);
    }
  `,
})
export class AuthCallbackComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams as CallbackParams;
    void this.auth.completeLogin(params);
  }
}
