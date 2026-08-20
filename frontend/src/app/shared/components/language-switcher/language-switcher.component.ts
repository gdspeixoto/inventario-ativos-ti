import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Button } from 'primeng/button';
import { Menu } from 'primeng/menu';
import { Tooltip } from 'primeng/tooltip';
import type { MenuItem } from 'primeng/api';
import { TranslationService } from '@core/i18n/translation.service';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * Language picker driven entirely by `i18n.config.ts` — adding a locale never
 * touches this component.
 */
@Component({
  selector: 'app-language-switcher',
  imports: [Button, Menu, Tooltip, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-button
      type="button"
      severity="secondary"
      [text]="true"
      [rounded]="true"
      icon="pi pi-globe"
      [ariaLabel]="'language.change' | translate"
      [pTooltip]="'language.change' | translate"
      tooltipPosition="bottom"
      (onClick)="menu.toggle($event)"
    />
    <p-menu #menu [model]="items()" [popup]="true" appendTo="body" />
  `,
})
export class LanguageSwitcherComponent {
  private readonly i18n = inject(TranslationService);

  readonly items = computed<MenuItem[]>(() =>
    this.i18n.locales.map((locale) => ({
      label: locale.label,
      icon: locale.code === this.i18n.locale() ? 'pi pi-check' : 'pi pi-fw',
      command: () => void this.i18n.use(locale.code),
    })),
  );
}
