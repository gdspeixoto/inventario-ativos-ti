import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { ContractApiService } from '@app/features/services/business.service';
import type {
  CreateContractCommand,
  OrganizationalUnitTree,
  SupplierListItem,
} from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { CommandRunner } from './command-runner';
import {
  ADJUSTMENT_INDEX_OPTIONS,
  CONTRACT_CATEGORY_OPTIONS,
  CURRENCY_OPTIONS,
} from './select-options';
import { flattenUnits } from './license-form.component';
import { dateAfter, toDateOnly, trimmedOrNull } from './form-utils';

const MAX_NUMBER = 60;
const MAX_NAME = 200;

const PERIODICITY_OPTIONS = [
  { label: 'Nenhuma', value: 'Nenhuma' as const },
  { label: 'Mensal', value: 'Mensal' as const },
  { label: 'Trimestral', value: 'Trimestral' as const },
  { label: 'Semestral', value: 'Semestral' as const },
  { label: 'Anual', value: 'Anual' as const },
];

@Component({
  selector: 'app-contract-form',
  imports: [
    ReactiveFormsModule,
    InputText,
    InputNumber,
    Textarea,
    Select,
    DatePicker,
    FormDialogComponent,
    FormFieldComponent,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.newContract"
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
          for="ctr-number"
          label="contracts.number"
          [control]="form.controls.number"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="ctr-number" formControlName="number" [maxlength]="maxNumber" />
        </app-form-field>

        <app-form-field
          for="ctr-name"
          label="contracts.name"
          [control]="form.controls.name"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="ctr-name" formControlName="name" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="ctr-supplier"
          label="contracts.supplier"
          [control]="form.controls.supplierId"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="ctr-supplier"
            formControlName="supplierId"
            [options]="supplierOptions()"
            optionLabel="label"
            optionValue="value"
            [filter]="true"
            filterBy="label"
            appendTo="body"
            placeholder="—"
          />
        </app-form-field>

        <app-form-field
          for="ctr-unit"
          label="contracts.unit"
          [control]="form.controls.organizationalUnitId"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="ctr-unit"
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
          for="ctr-category"
          label="contracts.category"
          [control]="form.controls.category"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="ctr-category"
            formControlName="category"
            [options]="categoryOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="ctr-amount"
          label="contracts.monthlyAmount"
          [control]="form.controls.monthlyAmount"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="ctr-amount"
            formControlName="monthlyAmount"
            mode="currency"
            [currency]="form.controls.currency.value"
            locale="pt-BR"
            [min]="0"
            [minFractionDigits]="2"
          />
        </app-form-field>

        <app-form-field
          for="ctr-start"
          label="contracts.startDate"
          [control]="form.controls.startDate"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="ctr-start"
            formControlName="startDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="ctr-end"
          label="contracts.endDate"
          [control]="form.controls.endDate"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="ctr-end"
            formControlName="endDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="ctr-index"
          label="contracts.adjustmentIndex"
          [control]="form.controls.adjustmentIndex"
        >
          <p-select
            inputId="ctr-index"
            formControlName="adjustmentIndex"
            [options]="indexOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="ctr-periodicity"
          label="contracts.periodicity"
          [control]="form.controls.adjustmentPeriodicity"
        >
          <p-select
            inputId="ctr-periodicity"
            formControlName="adjustmentPeriodicity"
            [options]="periodicityOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field for="ctr-currency" label="costs.currency" [control]="form.controls.currency">
          <p-select
            inputId="ctr-currency"
            formControlName="currency"
            [options]="currencyOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="ctr-description"
          label="contracts.description"
          [control]="form.controls.description"
        >
          <textarea pTextarea id="ctr-description" formControlName="description" rows="2"></textarea>
        </app-form-field>
      </form>
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

    @media (max-width: 640px) {
      .grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class ContractFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ContractApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly units = input<readonly OrganizationalUnitTree[]>([]);
  readonly suppliers = input<readonly SupplierListItem[]>([]);

  readonly saved = output<string>();
  readonly closed = output<void>();

  protected readonly maxNumber = MAX_NUMBER;
  protected readonly maxName = MAX_NAME;
  protected readonly categoryOptions = CONTRACT_CATEGORY_OPTIONS;
  protected readonly indexOptions = ADJUSTMENT_INDEX_OPTIONS;
  protected readonly currencyOptions = CURRENCY_OPTIONS;
  protected readonly periodicityOptions = PERIODICITY_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group(
    {
      number: ['', [Validators.required, Validators.maxLength(MAX_NUMBER)]],
      name: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
      supplierId: ['', Validators.required],
      organizationalUnitId: ['', Validators.required],
      category: ['Licenciamento' as const, Validators.required],
      startDate: [null as Date | null, Validators.required],
      endDate: [null as Date | null, Validators.required],
      monthlyAmount: [0, [Validators.required, Validators.min(0)]],
      currency: ['BRL'],
      description: [''],
      adjustmentIndex: ['NegociacaoManual' as const],
      adjustmentPeriodicity: ['Anual' as const],
    },
    { validators: dateAfter('startDate', 'endDate') },
  );

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  protected readonly unitOptions = computed(() => flattenUnits(this.units()));

  protected readonly supplierOptions = computed(() =>
    this.suppliers().map((supplier) => ({ label: supplier.name, value: supplier.id })),
  );

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const command: CreateContractCommand = {
      number: value.number.trim(),
      name: value.name.trim(),
      supplierId: value.supplierId,
      organizationalUnitId: value.organizationalUnitId,
      category: value.category,
      startDate: toDateOnly(value.startDate)!,
      endDate: toDateOnly(value.endDate)!,
      monthlyAmount: value.monthlyAmount,
      currency: value.currency,
      description: trimmedOrNull(value.description),
      adjustmentIndex: value.adjustmentIndex,
      adjustmentPeriodicity: value.adjustmentPeriodicity,
    };

    const outcome = await this.runner.run(() => this.api.create(command), {
      successMessage: 'feedback.contractCreated',
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
    this.form.reset({
      currency: 'BRL',
      category: 'Licenciamento',
      adjustmentIndex: 'NegociacaoManual',
      adjustmentPeriodicity: 'Anual',
      monthlyAmount: 0,
    });
    this.submittedState.set(false);
    this.runner.reset();
  }
}
