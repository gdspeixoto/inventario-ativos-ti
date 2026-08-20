import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Toast } from 'primeng/toast';

/**
 * Root component.
 *
 * Holds only the router outlet and the two singleton overlay hosts that
 * `ToastService` and `ConfirmService` write to. Everything visual belongs to
 * `ShellComponent` (authenticated area) or to the standalone public pages, so
 * the login screen is not wrapped in application chrome.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, ConfirmDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <router-outlet />

    <p-toast position="top-right" [breakpoints]="{ '640px': { width: '90vw', right: '5vw' } }" />
    <p-confirmdialog [style]="{ width: 'min(26rem, 92vw)' }" />
  `,
})
export class App {}
