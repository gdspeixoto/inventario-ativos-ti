import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { Tooltip } from 'primeng/tooltip';
import { APP_SETTINGS } from '@core/app-settings';
import { AUTH_CONFIG } from '@core/authentication/auth.tokens';
import { LayoutStore } from '../layout.store';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { BrandLogoComponent } from '@shared/components/brand-logo/brand-logo.component';
import { LanguageSwitcherComponent } from '@shared/components/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '@shared/components/theme-switcher/theme-switcher.component';
import { UserMenuComponent } from '@shared/components/user-menu/user-menu.component';

/**
 * Top bar: menu toggle, brand, global search and the account controls.
 *
 * The search box is a placeholder wired to an output, so a project can hook up
 * its own command palette or backend search without restyling the navbar.
 */
@Component({
  selector: 'app-navbar',
  imports: [
    RouterLink,
    FormsModule,
    Button,
    IconField,
    InputIcon,
    InputText,
    Tooltip,
    TranslatePipe,
    BrandLogoComponent,
    LanguageSwitcherComponent,
    ThemeSwitcherComponent,
    UserMenuComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
  protected readonly layout = inject(LayoutStore);
  protected readonly settings = inject(APP_SETTINGS);
  protected readonly homeRoute = inject(AUTH_CONFIG).routes.afterLogin;

  protected readonly searchTerm = signal('');

  onSearch(): void {
    const term = this.searchTerm().trim();
    if (!term) {
      return;
    }
    // Placeholder: wire this to the project's search route or command palette.
    this.searchTerm.set('');
  }
}
