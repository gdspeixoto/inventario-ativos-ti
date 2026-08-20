import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Button } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Password } from 'primeng/password';
import { APP_SETTINGS } from '@core/app-settings';
import { AuthService } from '@core/authentication/auth.service';
import { AUTH_CONFIG } from '@core/authentication/auth.tokens';
import { AuthError } from '@core/authentication/auth.errors';
import { TranslationService } from '@core/i18n/translation.service';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { AutofocusDirective } from '@shared/directives/autofocus.directive';
import { BrandLogoComponent } from '@shared/components/brand-logo/brand-logo.component';
import { LanguageSwitcherComponent } from '@shared/components/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '@shared/components/theme-switcher/theme-switcher.component';
import { LoginShowcaseComponent } from './showcase/showcase.component';

const MIN_PASSWORD_LENGTH = 6;

/**
 * Sign-in screen.
 *
 * The layout adapts to how the tenant is configured, with no code change:
 *  - credentials only          → form, no separator, no provider buttons;
 *  - single provider, no form  → one large provider button (optionally skipped
 *    entirely when `autoRedirectSingleProvider` is on);
 *  - both                      → form, "or continue with" separator, buttons.
 */
@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    Button,
    IconField,
    InputIcon,
    InputText,
    Message,
    Password,
    TranslatePipe,
    AutofocusDirective,
    BrandLogoComponent,
    LanguageSwitcherComponent,
    LoginShowcaseComponent,
    ThemeSwitcherComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly authConfig = inject(AUTH_CONFIG);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly i18n = inject(TranslationService);

  protected readonly settings = inject(APP_SETTINGS);
  protected readonly minPasswordLength = MIN_PASSWORD_LENGTH;

  protected readonly providers = this.auth.availableProviders;
  protected readonly showCredentials = this.auth.isCredentialsLoginEnabled;
  protected readonly showSeparator = this.auth.showProviderSeparator;
  protected readonly forgotPasswordUrl = this.authConfig.credentials.forgotPasswordUrl;

  /** Provider whose redirect is currently in flight, for the button spinner. */
  protected readonly pendingProvider = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly isBusy = computed(() => this.submitting() || this.pendingProvider() !== null);

  protected readonly form = this.formBuilder.nonNullable.group({
    // Aceita nome de usuario ou e-mail: a credencial de emergencia e um
    // login curto (`admin.local`), que `Validators.email` rejeitaria.
    username: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)]],
  });

  ngOnInit(): void {
    // `completeLogin` redirects here with `?error=` when the callback fails.
    const errorCode = this.route.snapshot.queryParamMap.get('error');
    if (errorCode) {
      this.errorKey.set(`auth.errors.${errorCode}`);
    }

    if (this.shouldAutoRedirect()) {
      void this.loginWith(this.providers()[0].id);
    }
  }

  protected providerLabel(label: string): string {
    return this.i18n.translate('auth.signInWith', { provider: label });
  }

  protected async loginWith(providerId: string): Promise<void> {
    this.errorKey.set(null);
    this.pendingProvider.set(providerId);
    try {
      await this.auth.login(providerId);
    } catch (error) {
      this.pendingProvider.set(null);
      this.errorKey.set(toTranslationKey(error));
    }
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.isBusy()) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorKey.set(null);
    this.submitting.set(true);
    try {
      const { username, password } = this.form.getRawValue();
      await this.auth.loginWithCredentials(username, password);
    } catch (error) {
      this.errorKey.set(toTranslationKey(error));
    } finally {
      this.submitting.set(false);
    }
  }

  protected hasError(control: 'username' | 'password', error: string): boolean {
    const field = this.form.controls[control];
    return field.touched && field.hasError(error);
  }

  private shouldAutoRedirect(): boolean {
    return (
      this.authConfig.autoRedirectSingleProvider &&
      !this.showCredentials &&
      this.providers().length === 1 &&
      !this.errorKey()
    );
  }
}

function toTranslationKey(error: unknown): string {
  return error instanceof AuthError ? error.translationKey : 'auth.errors.unknown';
}
