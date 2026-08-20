import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Menu } from 'primeng/menu';
import type { MenuItem } from 'primeng/api';
import { AuthService } from '@core/authentication/auth.service';
import { APP_SETTINGS } from '@core/app-settings';
import { TranslationService } from '@core/i18n/translation.service';
import { AvatarComponent } from '@shared/components/avatar/avatar.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * Avatar button in the navbar that opens the account menu.
 *
 * The trigger is a real `<button>` with `aria-haspopup`/`aria-expanded` so the
 * menu is reachable and announced correctly; PrimeNG's `p-menu` handles roving
 * focus and Escape once open.
 */
@Component({
  selector: 'app-user-menu',
  imports: [Menu, AvatarComponent, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="app-user-menu__trigger"
      [attr.aria-label]="'nav.openUserMenu' | translate"
      aria-haspopup="true"
      [attr.aria-expanded]="menu.overlayVisible"
      (click)="menu.toggle($event)"
    >
      <app-avatar [name]="user().name" [picture]="user().picture" size="sm" />
      <span class="app-user-menu__name">{{ user().name }}</span>
      <i class="pi pi-angle-down" aria-hidden="true"></i>
    </button>

    <p-menu
      #menu
      [model]="items()"
      [popup]="true"
      appendTo="body"
      styleClass="app-user-menu__panel"
    >
      <ng-template #start>
        <div class="app-user-menu__header">
          <app-avatar [name]="user().name" [picture]="user().picture" size="md" />
          <div class="app-user-menu__identity">
            <strong>{{ user().name }}</strong>
            <span>{{ user().email || user().username }}</span>
          </div>
        </div>
      </ng-template>
    </p-menu>
  `,
  styles: `
    .app-user-menu__trigger {
      display: flex;
      align-items: center;
      gap: var(--app-spacing-xs);
      padding: 0.25rem 0.5rem 0.25rem 0.25rem;
      border: 1px solid transparent;
      border-radius: var(--app-radius-pill);
      background: none;
      color: var(--app-color-text);
      cursor: pointer;
      transition:
        background-color var(--app-transition),
        border-color var(--app-transition);
    }

    .app-user-menu__trigger:hover {
      border-color: var(--app-color-border);
      background-color: var(--app-color-surface-hover);
    }

    .app-user-menu__name {
      max-width: 12rem;
      overflow: hidden;
      font-size: var(--app-font-size-sm);
      font-weight: var(--app-font-weight-medium);
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .app-user-menu__trigger i {
      color: var(--app-color-text-muted);
      font-size: 0.7rem;
    }

    /* On small screens the name would crowd the navbar; the avatar is enough. */
    @media (width < 48rem) {
      .app-user-menu__name,
      .app-user-menu__trigger i {
        display: none;
      }
    }

    .app-user-menu__header {
      display: flex;
      align-items: center;
      gap: var(--app-spacing-sm);
      padding: var(--app-spacing-md);
      border-bottom: 1px solid var(--app-color-border);
    }

    .app-user-menu__identity {
      display: flex;
      min-width: 0;
      flex-direction: column;
    }

    .app-user-menu__identity strong {
      overflow: hidden;
      font-size: var(--app-font-size-sm);
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .app-user-menu__identity span {
      overflow: hidden;
      color: var(--app-color-text-secondary);
      font-size: var(--app-font-size-xs);
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  `,
})
export class UserMenuComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly settings = inject(APP_SETTINGS);
  private readonly i18n = inject(TranslationService);

  readonly user = this.auth.user;

  readonly items = computed<MenuItem[]>(() => {
    const entries: MenuItem[] = [
      {
        label: this.i18n.translate('nav.settings'),
        icon: 'pi pi-cog',
        command: () => void this.router.navigate(['/settings']),
      },
    ];

    if (this.settings.documentationUrl) {
      entries.push({
        label: this.i18n.translate('nav.documentation'),
        icon: 'pi pi-book',
        url: this.settings.documentationUrl,
        target: '_blank',
      });
    }

    entries.push(
      { separator: true },
      {
        label: this.i18n.translate('auth.signOut'),
        icon: 'pi pi-sign-out',
        command: () => void this.auth.logout(),
      },
    );

    return entries;
  });
}
