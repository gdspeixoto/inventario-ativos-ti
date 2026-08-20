import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Location } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { APP_SETTINGS } from '@core/app-settings';
import { AUTH_CONFIG } from '@core/authentication/auth.tokens';
import { AuthService } from '@core/authentication/auth.service';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { BrandLogoComponent } from '@shared/components/brand-logo/brand-logo.component';

export type ErrorCode = '401' | '403' | '404' | '500';

const ICONS: Record<ErrorCode, string> = {
  '401': 'pi pi-lock',
  '403': 'pi pi-ban',
  '404': 'pi pi-compass',
  '500': 'pi pi-exclamation-triangle',
};

/**
 * One component for every error screen.
 *
 * The status comes from the route's `data.code` — bound automatically thanks to
 * `withComponentInputBinding()` — so 401/403/404/500 are four route entries and
 * zero duplicated markup.
 *
 * The primary action adapts: an unauthenticated user is offered a sign-in link,
 * everyone else gets a way back.
 */
@Component({
  selector: 'app-error-page',
  imports: [RouterLink, Button, TranslatePipe, BrandLogoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="error-page app-grid-backdrop">
      <div class="error-page__inner">
        <app-brand-logo size="md" nameVariant="full" />

        <p class="error-page__code">{{ code() }}</p>
        <i class="error-page__icon {{ icon() }}" aria-hidden="true"></i>

        <h1 class="error-page__title">{{ 'errors.' + code() + '.title' | translate }}</h1>
        <p class="error-page__message">{{ 'errors.' + code() + '.message' | translate }}</p>

        <div class="error-page__actions">
          @if (showSignIn()) {
            <p-button
              [label]="'auth.signIn' | translate"
              icon="pi pi-sign-in"
              [routerLink]="loginRoute"
            />
          } @else {
            <p-button
              [label]="'common.goHome' | translate"
              icon="pi pi-home"
              [routerLink]="homeRoute"
            />
          }

          <p-button
            [label]="'common.back' | translate"
            icon="pi pi-arrow-left"
            severity="secondary"
            [outlined]="true"
            (onClick)="goBack()"
          />
        </div>

        @if (settings.supportEmail) {
          <p class="error-page__support">
            <a [href]="'mailto:' + settings.supportEmail">{{ settings.supportEmail }}</a>
          </p>
        }
      </div>
    </div>
  `,
  styles: `
    .error-page {
      display: grid;
      min-height: 100vh;
      min-height: 100dvh;
      place-items: center;
      padding: var(--app-spacing-lg);
      background-color: var(--app-color-background);
    }

    .error-page__inner {
      display: flex;
      max-width: 30rem;
      flex-direction: column;
      align-items: center;
      gap: var(--app-spacing-xs);
      text-align: center;
    }

    .error-page__code {
      margin-top: var(--app-spacing-lg);
      color: var(--app-color-border-strong);
      font-size: 4.5rem;
      font-weight: var(--app-font-weight-bold);
      letter-spacing: var(--app-letter-spacing-tight);
      line-height: 1;
    }

    .error-page__icon {
      margin-top: calc(var(--app-spacing-lg) * -1);
      color: var(--app-color-primary);
      font-size: 1.5rem;
    }

    .error-page__title {
      margin-top: var(--app-spacing-sm);
      font-size: var(--app-font-size-xl);
    }

    .error-page__message {
      color: var(--app-color-text-secondary);
      font-size: var(--app-font-size-sm);
    }

    .error-page__actions {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: var(--app-spacing-xs);
      margin-top: var(--app-spacing-md);
    }

    .error-page__support {
      margin-top: var(--app-spacing-lg);
      color: var(--app-color-text-muted);
      font-size: var(--app-font-size-xs);
    }
  `,
})
export class ErrorPageComponent {
  private readonly location = inject(Location);
  private readonly auth = inject(AuthService);
  private readonly authConfig = inject(AUTH_CONFIG);

  protected readonly settings = inject(APP_SETTINGS);
  protected readonly loginRoute = this.authConfig.routes.login;
  protected readonly homeRoute = this.authConfig.routes.afterLogin;

  /** Bound from `data: { code: '404' }` on the route. */
  readonly code = input<ErrorCode>('404');

  protected readonly icon = computed(() => ICONS[this.code()] ?? ICONS['404']);
  protected readonly showSignIn = computed(
    () => this.code() === '401' || !this.auth.isAuthenticated(),
  );

  protected goBack(): void {
    this.location.back();
  }
}
