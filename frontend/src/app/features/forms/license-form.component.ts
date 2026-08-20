import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { LicenseService } from '@app/features/services/asset.service';
import type { CreateLicenseCommand, OrganizationalUnitTree } from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { CommandRunner } from './command-runner';
import { BILLING_TYPE_OPTIONS, CURRENCY_OPTIONS } from './select-options';
import { dateAfter, toDateOnly, trimmedOrNull } from './form-utils';

/** Limites espelhados do domínio, para barrar o erro antes da viagem ao servidor. */
const MAX_NAME = 200;
const MAX_CODE = 60;
const MAX_MANUFACTURER = 120;

@Component({
  selector: 'app-license-form',
  imports: [
    ReactiveFormsModule,
    InputText,
    InputNumber,
    Textarea,
    Select,
    DatePicker,
    FormDialogComponent,
    FormFieldComponent,
    MoneyComponent,
    TranslatePipe,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.newLicense"
      description="forms.newLicenseHint"
      submitLabel="actions.create"
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
          for="lic-name"
          label="licenses.name"
          [control]="form.controls.name"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="lic-name" formControlName="name" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="lic-code"
          label="licenses.code"
          hint="licenses.codeHint"
          [control]="form.controls.code"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="lic-code" formControlName="code" [maxlength]="maxCode" />
        </app-form-field>

        <app-form-field
          for="lic-unit"
          label="licenses.unit"
          [control]="form.controls.organizationalUnitId"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="lic-unit"
            formControlName="organizationalUnitId"
            [options]="unitOptions()"
            optionLabel="label"
            optionValue="value"
            [filter]="true"
            filterBy="label"
            appendTo="body"
            placeholder="—"
          />
        </app-form-field>

        <app-form-field
          for="lic-manufacturer"
          label="licenses.manufacturer"
          [control]="form.controls.manufacturer"
          [required]="true"
          [submitted]="submitted()"
        >
          <input
            pInputText
            id="lic-manufacturer"
            formControlName="manufacturer"
            [maxlength]="maxManufacturer"
          />
        </app-form-field>

        <app-form-field
          for="lic-product"
          label="licenses.product"
          [control]="form.controls.product"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="lic-product" formControlName="product" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field for="lic-plan" label="licenses.plan" [control]="form.controls.plan">
          <input pInputText id="lic-plan" formControlName="plan" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="lic-billing"
          label="licenses.billingType"
          [control]="form.controls.billingType"
        >
          <p-select
            inputId="lic-billing"
            formControlName="billingType"
            [options]="billingOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="lic-quantity"
          label="licenses.contracted"
          [control]="form.controls.contractedQuantity"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="lic-quantity"
            formControlName="contractedQuantity"
            [min]="1"
            [showButtons]="true"
mode="decimal"
          />
        </app-form-field>

        <app-form-field
          for="lic-price"
          label="licenses.unitPrice"
          [control]="form.controls.unitPrice"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="lic-price"
            formControlName="unitPrice"
            mode="currency"
            [currency]="form.controls.currency.value"
            locale="pt-BR"
            [min]="0"
            [minFractionDigits]="2"
          />
        </app-form-field>

        <app-form-field for="lic-currency" label="costs.currency" [control]="form.controls.currency">
          <p-select
            inputId="lic-currency"
            formControlName="currency"
            [options]="currencyOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field for="lic-start" label="licenses.startDate" [control]="form.controls.startDate">
          <p-datepicker
            inputId="lic-start"
            formControlName="startDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="lic-renewal"
          label="licenses.renewal"
          [control]="form.controls.renewalDate"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="lic-renewal"
            formControlName="renewalDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="lic-description"
          label="licenses.description"
          [control]="form.controls.description"
        >
          <textarea pTextarea id="lic-description" formControlName="description" rows="2"></textarea>
        </app-form-field>
      </form>

      <p class="total">
        {{ 'licenses.monthly' | translate }}:
        <strong><app-money [amount]="monthlyAmount()" [currency]="form.controls.currency.value" /></strong>
      </p>
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

    .total {
      margin: 1rem 0 0;
      padding-top: 0.75rem;
      border-top: 1px solid var(--app-color-border);
      text-align: right;
      color: var(--app-color-text-muted);
      font-size: 0.875rem;
    }

    @media (max-width: 640px) {
      .grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class LicenseFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(LicenseService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly units = input<readonly OrganizationalUnitTree[]>([]);

  readonly saved = output<string>();
  readonly closed = output<void>();

  protected readonly maxName = MAX_NAME;
  protected readonly maxCode = MAX_CODE;
  protected readonly maxManufacturer = MAX_MANUFACTURER;
  protected readonly billingOptions = BILLING_TYPE_OPTIONS;
  protected readonly currencyOptions = CURRENCY_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group(
    {
      name: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
      code: ['', [Validators.required, Validators.maxLength(MAX_CODE)]],
      manufacturer: ['', [Validators.required, Validators.maxLength(MAX_MANUFACTURER)]],
      product: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
      plan: [''],
      description: [''],
      organizationalUnitId: ['', Validators.required],
      contractedQuantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      currency: ['BRL'],
      billingType: ['Mensal' as const],
      startDate: [null as Date | null],
      renewalDate: [null as Date | null],
    },
    { validators: dateAfter('startDate', 'renewalDate') },
  );

  private readonly submittedState = this.formBuilder.nonNullable.control(false);
  protected readonly submitted = computed(() => this.submittedState.value);

  /** Espelha o cálculo do servidor, para o usuário ver o total antes de salvar. */
  protected readonly monthlyAmount = computed(() => {
    const { contractedQuantity, unitPrice } = this.form.getRawValue();
    return (contractedQuantity ?? 0) * (unitPrice ?? 0);
  });

  /** Achata a árvore de unidades: o select é uma lista, a hierarquia vira recuo. */
  protected readonly unitOptions = computed(() => flattenUnits(this.units()));

  protected async save(): Promise<void> {
    this.submittedState.setValue(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const command: CreateLicenseCommand = {
      name: value.name.trim(),
      code: value.code.trim(),
      manufacturer: value.manufacturer.trim(),
      product: value.product.trim(),
      plan: trimmedOrNull(value.plan),
      description: trimmedOrNull(value.description),
      organizationalUnitId: value.organizationalUnitId,
      contractedQuantity: value.contractedQuantity,
      unitPrice: value.unitPrice,
      currency: value.currency,
      billingType: value.billingType,
      startDate: toDateOnly(value.startDate),
      renewalDate: toDateOnly(value.renewalDate),
    };

    const outcome = await this.runner.run(() => this.api.create(command), {
      successMessage: 'feedback.licenseCreated',
    });

    if (outcome.ok) {
      this.saved.emit(outcome.value!.id);
      this.reset();
    }
  }

  protected close(): void {
    this.closed.emit();
    this.reset();
  }

  private reset(): void {
    this.form.reset({ currency: 'BRL', billingType: 'Mensal', contractedQuantity: 1, unitPrice: 0 });
    this.submittedState.setValue(false);
    this.runner.reset();
  }
}

/** Converte a árvore em lista, indicando a profundidade com espaços no rótulo. */
export function flattenUnits(
  units: readonly OrganizationalUnitTree[],
  depth = 0,
): { label: string; value: string }[] {
  return units.flatMap((unit) => [
    { label: `${'\u00A0\u00A0'.repeat(depth)}${unit.name} (${unit.code})`, value: unit.id },
    ...flattenUnits(unit.children ?? [], depth + 1),
  ]);
}
