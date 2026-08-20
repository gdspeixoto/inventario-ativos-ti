import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { InitialsPipe } from '@shared/pipes/initials.pipe';

export type AvatarSize = 'sm' | 'md' | 'lg';

/**
 * User avatar with an initials fallback.
 *
 * Not PrimeNG's `p-avatar`: this one handles a broken image URL (common with
 * identity providers that return an expired Graph link) by falling back to
 * initials, and derives a stable background color from the name so users stay
 * visually distinguishable in lists.
 */
@Component({
  selector: 'app-avatar',
  imports: [InitialsPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="app-avatar"
      [class]="'app-avatar--' + size()"
      [style.background-color]="showImage() ? null : backgroundColor()"
      [attr.aria-label]="name()"
      role="img"
    >
      @if (showImage()) {
        <img [src]="picture()" [alt]="name()" (error)="onImageError()" />
      } @else {
        <span aria-hidden="true">{{ name() | initials }}</span>
      }
    </span>
  `,
  styles: `
    .app-avatar {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      flex: 0 0 auto;
      overflow: hidden;
      border-radius: var(--app-radius-pill);
      color: #fff;
      font-weight: var(--app-font-weight-semibold);
      line-height: 1;
      user-select: none;
    }

    .app-avatar img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .app-avatar--sm {
      width: 1.75rem;
      height: 1.75rem;
      font-size: var(--app-font-size-2xs);
    }

    .app-avatar--md {
      width: 2.25rem;
      height: 2.25rem;
      font-size: var(--app-font-size-xs);
    }

    .app-avatar--lg {
      width: 4rem;
      height: 4rem;
      font-size: var(--app-font-size-xl);
    }
  `,
})
export class AvatarComponent {
  readonly name = input<string>('');
  readonly picture = input<string | null>(null);
  readonly size = input<AvatarSize>('md');

  private imageFailed = false;

  readonly showImage = computed(() => !!this.picture() && !this.imageFailed);

  /**
   * Deterministic hue from the name: the same user always gets the same color,
   * with fixed saturation/lightness so contrast against white text holds.
   */
  readonly backgroundColor = computed(() => {
    const source = this.name() || '?';
    let hash = 0;
    for (let index = 0; index < source.length; index++) {
      hash = source.charCodeAt(index) + ((hash << 5) - hash);
    }
    return `hsl(${Math.abs(hash) % 360} 45% 38%)`;
  });

  onImageError(): void {
    this.imageFailed = true;
  }
}
