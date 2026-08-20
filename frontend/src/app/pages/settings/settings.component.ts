import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectButton } from 'primeng/selectbutton';
import { Tag } from 'primeng/tag';
import { AuthService } from '@core/authentication/auth.service';
import { AuthStore } from '@core/authentication/auth.store';
import { ThemeService } from '@core/theme/theme.service';
import { TranslationService } from '@core/i18n/translation.service';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { ThemeSwitcherComponent } from '@shared/components/theme-switcher/theme-switcher.component';
import type { ThemeDensity } from '@core/theme/theme.model';

/**
 * User preferences: appearance and session details.
 *
 * Everything here is stored client-side, so the page works before any backend
 * exists. Server-persisted preferences would replace `ThemeService`'s storage
 * layer, not this component.
 */
@Component({
  selector: 'app-settings',
  imports: [
    DatePipe,
    FormsModule,
    SelectButton,
    Tag,
    TranslatePipe,
    AppCardComponent,
    PageHeaderComponent,
    ThemeSwitcherComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent {
  private readonly authStore = inject(AuthStore);
  protected readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  protected readonly i18n = inject(TranslationService);

  protected readonly user = this.auth.user;
  protected readonly tokenExpiresAt = this.authStore.expiresAt;

  protected readonly densityOptions = computed(() => [
    { label: this.i18n.translate('theme.comfortable'), value: 'comfortable' as ThemeDensity },
    { label: this.i18n.translate('theme.compact'), value: 'compact' as ThemeDensity },
  ]);

  protected readonly languageOptions = computed(() =>
    this.i18n.locales.map((locale) => ({ label: locale.label, value: locale.code })),
  );

  protected onDensityChange(density: ThemeDensity): void {
    this.theme.setDensity(density);
  }

  protected onLanguageChange(code: string): void {
    void this.i18n.use(code);
  }
}
