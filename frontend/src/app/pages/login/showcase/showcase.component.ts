import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { PlatformService } from '@core/services/platform.service';

interface ShowcaseSlide {
  readonly id: 'overview' | 'metrics' | 'workflow';
  readonly title: string;
  readonly description: string;
}

const SLIDE_INTERVAL_MS = 7000;

/**
 * Brand showcase shown beside the login form.
 *
 * Keep this component intentionally self-contained: projects using the template
 * can swap the slide data and SVG mockups without touching authentication code.
 */
@Component({
  selector: 'app-login-showcase',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './showcase.component.html',
  styleUrl: './showcase.component.scss',
})
export class LoginShowcaseComponent {
  private readonly platform = inject(PlatformService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly slides: readonly ShowcaseSlide[] = [
    {
      id: 'overview',
      title: 'auth.showcase.overview.title',
      description: 'auth.showcase.overview.description',
    },
    {
      id: 'metrics',
      title: 'auth.showcase.metrics.title',
      description: 'auth.showcase.metrics.description',
    },
    {
      id: 'workflow',
      title: 'auth.showcase.workflow.title',
      description: 'auth.showcase.workflow.description',
    },
  ];

  protected readonly index = signal(0);
  protected readonly current = computed(() => this.slides[this.index()]);

  private timer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    this.start();
    this.destroyRef.onDestroy(() => this.stop());
  }

  protected goTo(index: number): void {
    this.index.set(index);
    this.restart();
  }

  protected move(step: 1 | -1): void {
    const total = this.slides.length;
    this.index.update((current) => (current + step + total) % total);
    this.restart();
  }

  protected handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'ArrowRight') {
      this.move(1);
    } else if (event.key === 'ArrowLeft') {
      this.move(-1);
    } else {
      return;
    }
    event.preventDefault();
  }

  protected stop(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  protected resume(): void {
    this.start();
  }

  private start(): void {
    if (this.timer !== null || this.prefersReducedMotion()) {
      return;
    }
    this.timer = setInterval(
      () => this.index.update((current) => (current + 1) % this.slides.length),
      SLIDE_INTERVAL_MS,
    );
  }

  private restart(): void {
    this.stop();
    this.start();
  }

  private prefersReducedMotion(): boolean {
    return this.platform.matchMedia('(prefers-reduced-motion: reduce)')?.matches ?? false;
  }
}
