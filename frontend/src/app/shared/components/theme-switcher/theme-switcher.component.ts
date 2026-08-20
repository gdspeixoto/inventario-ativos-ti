import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Button } from 'primeng/button';
import { SelectButton } from 'primeng/selectbutton';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { ThemeService } from '@core/theme/theme.service';
import { TranslationService } from '@core/i18n/translation.service';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import type { ThemeMode } from '@core/theme/theme.model';

/**
 * Theme control, in two shapes:
 *
 *  - `icon` (default) — a single toggle for the navbar;
 *  - `segmented` — light / dark / system, for the settings page, where the
 *    "follow the system" option must be discoverable.
 */
@Component({
  selector: 'app-theme-switcher',
  imports: [Button, SelectButton, Tooltip, FormsModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (variant() === 'segmented') {
      <p-selectbutton
        [options]="options()"
        [ngModel]="theme.mode()"
        (ngModelChange)="onModeChange($event)"
        optionLabel="label"
        optionValue="value"
        [allowEmpty]="false"
        [ariaLabelledBy]="'theme-switcher-label'"
      />
    } @else {
      <p-button
        type="button"
        severity="secondary"
        [text]="true"
        [rounded]="true"
        [icon]="theme.isDark() ? 'pi pi-sun' : 'pi pi-moon'"
        [ariaLabel]="'theme.toggle' | translate"
        [pTooltip]="'theme.toggle' | translate"
        tooltipPosition="bottom"
        (onClick)="theme.toggle()"
      />
    }
  `,
})
export class ThemeSwitcherComponent {
  protected readonly theme = inject(ThemeService);
  private readonly i18n = inject(TranslationService);

  readonly variant = input<'icon' | 'segmented'>('icon');

  readonly options = computed(() => [
    { label: this.i18n.translate('theme.light'), value: 'light' as ThemeMode, icon: 'pi pi-sun' },
    { label: this.i18n.translate('theme.dark'), value: 'dark' as ThemeMode, icon: 'pi pi-moon' },
    {
      label: this.i18n.translate('theme.system'),
      value: 'system' as ThemeMode,
      icon: 'pi pi-desktop',
    },
  ]);

  onModeChange(mode: ThemeMode): void {
    this.theme.setMode(mode);
  }
}
