import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LoadingService } from '@core/services/loading.service';
import { LayoutStore } from '../layout.store';
import { NavbarComponent } from '../navbar/navbar.component';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { FooterComponent } from '../footer/footer.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * Authenticated application frame.
 *
 * Owns the chrome — navbar, sidebar, footer, progress bar — and leaves the
 * routed page as its only variable part. Feature pages therefore never think
 * about layout.
 *
 * Two accessibility details worth keeping: the skip link is the first focusable
 * element on the page, and `<main>` carries `tabindex="-1"` so the skip link can
 * actually move focus into it.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, NavbarComponent, SidebarComponent, FooterComponent, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  protected readonly layout = inject(LayoutStore);
  protected readonly loading = inject(LoadingService);
}
