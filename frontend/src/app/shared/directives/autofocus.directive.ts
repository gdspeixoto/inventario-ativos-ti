import { AfterViewInit, Directive, ElementRef, inject, input } from '@angular/core';

/**
 * Focuses an element once the view is ready.
 *
 * The native `autofocus` attribute is unreliable in single-page applications:
 * the element often already exists when the route renders, so the browser never
 * fires it. Applied deliberately — moving focus without reason is a WCAG 3.2.1
 * problem, so this belongs on the primary field of a form the user just opened.
 */
@Directive({ selector: '[appAutofocus]' })
export class AutofocusDirective implements AfterViewInit {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Set to false to skip focusing conditionally. */
  readonly appAutofocus = input(true, {
    transform: (value: string | boolean) => value !== false && value !== 'false',
  });

  /** Delay in milliseconds, for elements animated into place. */
  readonly focusDelay = input(0);

  ngAfterViewInit(): void {
    if (!this.appAutofocus()) {
      return;
    }
    const focus = () => this.element.nativeElement.focus();
    if (this.focusDelay() > 0) {
      setTimeout(focus, this.focusDelay());
    } else {
      queueMicrotask(focus);
    }
  }
}
