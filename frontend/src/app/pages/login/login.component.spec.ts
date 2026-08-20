import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import { beforeEach, describe, expect, it } from 'vitest';

import { LoginComponent } from './login.component';
import { APP_SETTINGS } from '@core/app-settings';
import { AUTH_CONFIG } from '@core/authentication/auth.tokens';
import { I18N_CONFIG } from '@core/i18n/i18n.tokens';
import { THEME_CONFIG } from '@core/theme/theme.tokens';
import { CorporatePreset } from '@core/theme/primeng-preset';
import { appSettings } from '@config/app.settings';
import { authConfig } from '@config/auth.config';
import { i18nConfig } from '@config/i18n.config';
import { themeConfig } from '@config/theme.config';
import type { AuthConfig } from '@core/authentication/models/auth-config.model';

/**
 * Rendering smoke test.
 *
 * Beyond "does it not throw", this pins the behaviour the login screen is
 * supposed to derive from configuration: which affordances appear for which
 * combination of credentials login and enabled providers.
 */
function configure(authOverrides: Partial<AuthConfig> = {}) {
  TestBed.configureTestingModule({
    imports: [LoginComponent],
    providers: [
      provideRouter([]),
      provideHttpClient(),
      provideHttpClientTesting(),
      providePrimeNG({ theme: { preset: CorporatePreset } }),
      { provide: APP_SETTINGS, useValue: appSettings },
      { provide: THEME_CONFIG, useValue: themeConfig },
      { provide: I18N_CONFIG, useValue: i18nConfig },
      { provide: AUTH_CONFIG, useValue: { ...authConfig, ...authOverrides } },
    ],
  });
}

function render(): ComponentFixture<LoginComponent> {
  const fixture = TestBed.createComponent(LoginComponent);
  fixture.detectChanges();
  return fixture;
}

describe('LoginComponent', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('renders the credentials form and the enabled providers', () => {
    configure({ credentials: { ...authConfig.credentials, enabled: true } });
    const element = render().nativeElement as HTMLElement;

    expect(element.querySelector('#username')).not.toBeNull();
    expect(element.querySelector('#password')).not.toBeNull();
    // `auth.config.ts` enables Keycloak only.
    expect(element.querySelectorAll('.login__provider')).toHaveLength(1);
  });

  it('shows the separator only when both sign-in paths are available', () => {
    configure({ credentials: { ...authConfig.credentials, enabled: true } });
    expect(
      (render().nativeElement as HTMLElement).querySelector('.login__separator'),
    ).not.toBeNull();
  });

  it('hides the form and the separator when credentials login is disabled', () => {
    configure({ credentials: { ...authConfig.credentials, enabled: false } });
    const element = render().nativeElement as HTMLElement;

    expect(element.querySelector('#username')).toBeNull();
    expect(element.querySelector('.login__separator')).toBeNull();
    expect(element.querySelectorAll('.login__provider')).toHaveLength(1);
  });

  it('requires a username before submitting', async () => {
    configure({ credentials: { ...authConfig.credentials, enabled: true } });
    const fixture = render();
    const component = fixture.componentInstance;

    component['form'].setValue({ username: '', password: 'secret123' });
    expect(component['form'].invalid).toBe(true);

    component['form'].setValue({ username: 'ana@empresa.com.br', password: 'secret123' });
    expect(component['form'].valid).toBe(true);
  });

  it('accepts a plain username, not only an email address', async () => {
    // A credencial de emergencia e um login curto (`admin.local`); exigir
    // formato de e-mail impediria justamente o acesso que ela existe para dar.
    configure({ credentials: { ...authConfig.credentials, enabled: true } });
    const fixture = render();
    const component = fixture.componentInstance;

    component['form'].setValue({ username: 'admin.local', password: 'secret123' });

    expect(component['form'].valid).toBe(true);
  });
});
