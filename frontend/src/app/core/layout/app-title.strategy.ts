import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { APP_SETTINGS } from '../app-settings';
import { TranslationService } from '../i18n/translation.service';
import { LayoutStore } from './layout.store';

/**
 * Sets the document title from the route's `title`, treating it as an i18n key.
 *
 * Result: `Dashboard · Controle de Ativos`. Routes with no title fall back to
 * the application name alone. The resolved title is also pushed into
 * `LayoutStore` so the chrome can display it without re-resolving anything.
 */
@Injectable()
export class AppTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly settings = inject(APP_SETTINGS);
  private readonly i18n = inject(TranslationService);
  private readonly layout = inject(LayoutStore);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const key = this.buildTitle(snapshot);
    const translated = key ? this.i18n.translate(key) : null;

    this.layout.setPageTitle(translated);
    this.title.setTitle(
      translated
        ? `${translated}${this.settings.titleSeparator}${this.settings.name}`
        : this.settings.name,
    );
  }
}
