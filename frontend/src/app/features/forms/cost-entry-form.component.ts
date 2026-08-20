import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { CostApiService } from '@app/features/services/business.service';
import type { CreateCostEntryCommand, OrganizationalUnitTree } from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { CommandRunner } from './command-runner';
import { COST_CATEGORY_OPTIONS, COST_TYPE_OPTIONS, CURRENCY_OPTIONS } from './select-options';
import { toDateOnly } from './form-utils';
import { flattenUnits } from './license-form.component';

const MAX_DESCRIPTION = 400;

/**
 * Lançamento de um custo avulso.
 *
 * Os ativos já carregam seus valores recorrentes; isto existe para o que não
 * cabe neles — uma consultoria pontual, uma nota de telecom, um serviço sob
 * demanda. Sem este registro, esses gastos simplesmente não apareceriam nos
 * relatórios, e o total mensal seria menor que a realidade.
 *
 * Competência e data efetiva são campos distintos de propósito: uma nota paga
 * em setembro pode se referir a agosto. É a competência que faz o fechamento
 * mensal bater; a data efetiva registra quando de fato ocorreu. O servidor
 * normaliza a competência para o primeiro dia do mês.
 */
@Component({
  selector: 'app-cost-entry-form',
  imports: [
    ReactiveFormsModule,
    InputText,
    InputNumber,
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
      title="costs.newEntry"
      description="costs.newEntryHint"
      submitLabel="actions.create"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="48rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="grid">
        <app-form-field
          class="grid__span-2"
          for="cost-unit"
          label="costs.unit"
          [control]="form.controls.organizationalUnitId"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="cost-unit"
            formControlName="organizationalUnitId"
            [options]="unitOptions()"
            optionLabel="label"
            optionValue="value"
            [filter]="true"
            filterBy="label"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="cost-type"
          label="costs.type"
          [control]="form.controls.type"
          [required]="true"
        >
          <p-select
            inputId="cost-type"
            formControlName="type"
            [options]="typeOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="cost-category"
          label="costs.category"
          [control]="form.controls.category"
          [required]="true"
        >
          <p-select
            inputId="cost-category"
            formControlName="category"
            [options]="categoryOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="cost-amount"
          label="costs.amount"
          [control]="form.controls.amount"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="cost-amount"
            formControlName="amount"
            mode="currency"
            [currency]="form.controls.currency.value"
            locale="pt-BR"
            [min]="0"
            [minFractionDigits]="2"
          />
        </app-form-field>

        <app-form-field for="cost-currency" label="costs.currency" [control]="form.controls.currency">
          <p-select
            inputId="cost-currency"
            formControlName="currency"
            [options]="currencyOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="cost-competence"
          label="costs.competence"
          hint="costs.competenceHint"
          [control]="form.controls.competenceMonth"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="cost-competence"
            formControlName="competenceMonth"
            view="month"
            dateFormat="mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="cost-effective"
          label="costs.effectiveDate"
          hint="costs.effectiveDateHint"
          [control]="form.controls.effectiveDate"
        >
          <p-datepicker
            inputId="cost-effective"
            formControlName="effectiveDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="cost-description"
          label="costs.description"
          [control]="form.controls.description"
          [required]="true"
          [submitted]="submitted()"
        >
          <input
            pInputText
            id="cost-description"
            formControlName="description"
            [maxlength]="maxDescription"
          />
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
export class CostEntryFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(CostApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly units = input<readonly OrganizationalUnitTree[]>([]);

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxDescription = MAX_DESCRIPTION;
  protected readonly typeOptions = COST_TYPE_OPTIONS;
  protected readonly categoryOptions = COST_CATEGORY_OPTIONS;
  protected readonly currencyOptions = CURRENCY_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group({
    organizationalUnitId: ['', Validators.required],
    type: ['Pontual' as CreateCostEntryCommand['type'], Validators.required],
    category: ['Outros' as CreateCostEntryCommand['category'], Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    currency: ['BRL'],
    competenceMonth: [new Date() as Date | null, Validators.required],
    effectiveDate: [null as Date | null],
    description: ['', [Validators.required, Validators.maxLength(MAX_DESCRIPTION)]],
  });

  private readonly submittedState = this.formBuilder.nonNullable.control(false);
  protected readonly submitted = computed(() => this.submittedState.value);

  protected readonly unitOptions = computed(() => flattenUnits(this.units()));

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      // Mês corrente como padrão: é a competência da esmagadora maioria dos
      // lançamentos feitos no dia a dia.
      this.form.reset({
        organizationalUnitId: '',
        type: 'Pontual',
        category: 'Outros',
        amount: 0,
        currency: 'BRL',
        competenceMonth: new Date(),
        effectiveDate: null,
        description: '',
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

    const command: CreateCostEntryCommand = {
      organizationalUnitId: value.organizationalUnitId,
      type: value.type,
      category: value.category,
      amount: value.amount,
      currency: value.currency,
      // `toDateOnly` usa os componentes locais da data: `toISOString` deslocaria
      // o dia em fusos negativos, e um lançamento do dia 1 cairia no mês anterior.
      competenceMonth: toDateOnly(value.competenceMonth) ?? '',
      effectiveDate: toDateOnly(value.effectiveDate),
      description: value.description.trim(),
    };

    const outcome = await this.runner.run(() => this.api.createEntry(command), {
      successMessage: 'feedback.costEntryCreated',
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
