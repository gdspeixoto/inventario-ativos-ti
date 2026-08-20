import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { TranslatePipe } from '@core/i18n/translate.pipe';

/**
 * Rótulo, marcação de obrigatório e mensagem de erro de um campo.
 *
 * O erro só aparece depois que o campo foi tocado ou o formulário enviado:
 * acusar "campo obrigatório" antes de a pessoa ter chance de preencher é
 * ruído, não ajuda.
 */
@Component({
  selector: 'app-form-field',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="field" [class.field--invalid]="showError()">
      <label [for]="for()">
        {{ label() | translate }}
        @if (required()) {
          <span class="field__required" aria-hidden="true">*</span>
        }
      </label>

      <ng-content />

      @if (hint(); as text) {
        <small class="field__hint">{{ text | translate }}</small>
      }

      <small class="field__error" role="alert" [id]="for() + '-error'">
        @if (showError()) {
          {{ errorText() | translate: errorParams() }}
        }
      </small>
    </div>
  `,
  styles: `
    .field {
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
    }

    label {
      font-size: 0.8125rem;
      font-weight: 600;
      color: var(--app-color-text);
    }

    .field__required {
      margin-left: 0.125rem;
      color: var(--app-color-danger);
    }

    .field__hint,
    .field__error {
      font-size: 0.75rem;
      line-height: 1.3;
      min-height: 1rem;
    }

    .field__hint {
      color: var(--app-color-text-muted);
    }

    .field__error {
      color: var(--app-color-danger);
    }

    :host ::ng-deep .field--invalid input,
    :host ::ng-deep .field--invalid textarea,
    :host ::ng-deep .field--invalid .p-select,
    :host ::ng-deep .field--invalid .p-inputnumber-input,
    :host ::ng-deep .field--invalid .p-datepicker-input {
      border-color: var(--app-color-danger);
    }
  `,
})
export class FormFieldComponent {
  readonly for = input.required<string>();
  readonly label = input.required<string>();
  readonly control = input.required<AbstractControl>();
  readonly required = input(false);
  readonly hint = input<string | null>(null);

  /** Força a exibição do erro quando o usuário tenta enviar sem tocar no campo. */
  readonly submitted = input(false);

  protected readonly showError = computed(() => {
    const control = this.control();
    return control.invalid && (control.touched || control.dirty || this.submitted());
  });

  protected readonly errorText = computed(() => {
    const errors = this.control().errors;

    if (!errors) {
      return '';
    }
    if (errors['required']) {
      return 'validation.required';
    }
    if (errors['min']) {
      return 'validation.min';
    }
    if (errors['max']) {
      return 'validation.max';
    }
    if (errors['maxlength']) {
      return 'validation.maxLength';
    }
    if (errors['minlength']) {
      return 'validation.minLength';
    }
    if (errors['email']) {
      return 'validation.email';
    }
    if (errors['pattern']) {
      return 'validation.pattern';
    }
    if (errors['sameValue']) {
      return 'validation.sameValue';
    }
    if (errors['dateOrder']) {
      return 'validation.dateOrder';
    }
    return 'validation.invalid';
  });

  protected readonly errorParams = computed<Record<string, string | number>>(() => {
    const errors = this.control().errors ?? {};
    const params: Record<string, string | number> = {};

    if (errors['min']) {
      params['min'] = errors['min'].min as number;
    }
    if (errors['max']) {
      params['max'] = errors['max'].max as number;
    }
    if (errors['maxlength']) {
      params['max'] = errors['maxlength'].requiredLength as number;
    }
    if (errors['minlength']) {
      params['min'] = errors['minlength'].requiredLength as number;
    }

    return params;
  });
}
