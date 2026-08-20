import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { LicenseService } from '@app/features/services/asset.service';
import type { LicenseDetail, UpdateLicenseCommand } from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { CommandRunner } from './command-runner';
import { BILLING_TYPE_OPTIONS } from './select-options';
import { dateAfter, fromDateOnly, toDateOnly, trimmedOrNull } from './form-utils';

const MAX_NAME = 200;
const MAX_CODE = 60;
const MAX_MANUFACTURER = 120;

/**
 * Correção dos dados cadastrais de uma licença.
 *
 * É um formulário separado do de cadastro, e isso é proposital. Criar exige
 * quantidade, preço e unidade; corrigir não deve tocar em nenhum dos três —
 * eles mudam por operações próprias, que registram motivo e alimentam o
 * histórico financeiro. Um formulário único carregaria esses campos na tela e
 * os reenviaria a cada correção de digitação, transformando o conserto de um
 * nome numa alteração silenciosa de valores.
 *
 * Por isso aqui não há preço, quantidade nem unidade organizacional.
 */
@Component({
  selector: 'app-license-edit-form',
  imports: [
    ReactiveFormsModule,
    InputText,
    Textarea,
    Select,
    DatePicker,
    FormDialogComponent,
    FormFieldComponent,
    TranslatePipe,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.editLicense"
      description="forms.editLicenseHint"
      submitLabel="actions.save"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="52rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="grid">
        <app-form-field
          class="grid__span-2"
          for="lic-edit-name"
          label="licenses.name"
          [control]="form.controls.name"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="lic-edit-name" formControlName="name" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="lic-edit-code"
          label="licenses.code"
          hint="licenses.codeHint"
          [control]="form.controls.code"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="lic-edit-code" formControlName="code" [maxlength]="maxCode" />
        </app-form-field>

        <app-form-field
          for="lic-edit-manufacturer"
          label="licenses.manufacturer"
          [control]="form.controls.manufacturer"
          [required]="true"
          [submitted]="submitted()"
        >
          <input
            pInputText
            id="lic-edit-manufacturer"
            formControlName="manufacturer"
            [maxlength]="maxManufacturer"
          />
        </app-form-field>

        <app-form-field
          for="lic-edit-product"
          label="licenses.product"
          [control]="form.controls.product"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="lic-edit-product" formControlName="product" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field for="lic-edit-plan" label="licenses.plan" [control]="form.controls.plan">
          <input pInputText id="lic-edit-plan" formControlName="plan" />
        </app-form-field>

        <app-form-field
          for="lic-edit-billing"
          label="licenses.billingType"
          [control]="form.controls.billingType"
        >
          <p-select
            inputId="lic-edit-billing"
            formControlName="billingType"
            [options]="billingOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="lic-edit-start"
          label="licenses.startDate"
          [control]="form.controls.startDate"
        >
          <p-datepicker
            inputId="lic-edit-start"
            formControlName="startDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="lic-edit-renewal"
          label="licenses.renewal"
          [control]="form.controls.renewalDate"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="lic-edit-renewal"
            formControlName="renewalDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="lic-edit-description"
          label="licenses.description"
          [control]="form.controls.description"
        >
          <textarea
            pTextarea
            id="lic-edit-description"
            formControlName="description"
            rows="2"
          ></textarea>
        </app-form-field>
      </form>

      <p class="note">{{ 'forms.editLicenseHint' | translate }}</p>
    </app-form-dialog>
  `,
  styles: `
    .grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 0.75rem 1rem;
    }

    .grid__span-2 {
      grid-column: 1 / -1;
    }

    .note {
      margin: 1rem 0 0;
      padding-top: 0.75rem;
      border-top: 1px solid var(--app-color-border);
      color: var(--app-color-text-muted);
      font-size: 0.8125rem;
    }

    @media (max-width: 640px) {
      .grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class LicenseEditFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(LicenseService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly license = input.required<LicenseDetail>();

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxName = MAX_NAME;
  protected readonly maxCode = MAX_CODE;
  protected readonly maxManufacturer = MAX_MANUFACTURER;
  protected readonly billingOptions = BILLING_TYPE_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group(
    {
      name: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
      code: ['', [Validators.required, Validators.maxLength(MAX_CODE)]],
      manufacturer: ['', [Validators.required, Validators.maxLength(MAX_MANUFACTURER)]],
      product: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
      plan: [''],
      description: [''],
      billingType: ['Mensal' as UpdateLicenseCommand['billingType']],
      startDate: [null as Date | null],
      renewalDate: [null as Date | null],
    },
    { validators: dateAfter('startDate', 'renewalDate') },
  );

  private readonly submittedState = this.formBuilder.nonNullable.control(false);
  protected readonly submitted = computed(() => this.submittedState.value);

  constructor() {
    // O formulário é preenchido a partir do registro atual sempre que o modal
    // reabre: sem isto, uma edição cancelada deixaria os campos alterados na
    // próxima abertura, e o usuário salvaria mudanças que achava descartadas.
    effect(() => {
      const current = this.license();

      if (!this.visible()) {
        return;
      }

      this.form.reset({
        name: current.name,
        code: current.code,
        manufacturer: current.manufacturer,
        product: current.product,
        plan: current.plan ?? '',
        description: current.description ?? '',
        billingType: current.billingType,
        startDate: fromDateOnly(current.startDate),
        renewalDate: fromDateOnly(current.renewalDate),
      });

      this.submittedState.setValue(false);
      this.runner.reset();
    });
  }

  protected async save(): Promise<void> {
    this.submittedState.setValue(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();

      return;
    }

    const value = this.form.getRawValue();

    const command: UpdateLicenseCommand = {
      name: value.name.trim(),
      code: value.code.trim(),
      manufacturer: value.manufacturer.trim(),
      product: value.product.trim(),
      plan: trimmedOrNull(value.plan),
      description: trimmedOrNull(value.description),
      billingType: value.billingType,
      startDate: toDateOnly(value.startDate),
      renewalDate: toDateOnly(value.renewalDate),
    };

    const outcome = await this.runner.run(() => this.api.update(this.license().id, command), {
      successMessage: 'feedback.licenseUpdated',
    });

    if (outcome.ok) {
      this.saved.emit();
    }
  }

  protected close(): void {
    this.runner.reset();
    this.closed.emit();
  }
}
