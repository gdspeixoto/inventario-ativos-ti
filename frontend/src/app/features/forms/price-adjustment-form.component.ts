import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { LicenseService } from '@app/features/services/asset.service';
import { ContractApiService } from '@app/features/services/business.service';
import type { ApplyPriceAdjustmentCommand, PriceAdjustmentResult } from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { CommandRunner } from './command-runner';
import { ADJUSTMENT_INDEX_OPTIONS } from './select-options';
import { differentFrom, toDateOnly } from './form-utils';

const MAX_REASON = 1000;

/** Onde o reajuste será aplicado. A rota muda; o formulário, não. */
export type AdjustmentTarget = 'license' | 'contract';

/**
 * Reajuste de valor de licença ou contrato.
 *
 * O usuário informa apenas o novo valor mensal — diferença, percentual e
 * impacto anual são responsabilidade do domínio. A prévia exibida aqui é
 * calculada no cliente só para orientar a decisão; o número que vale é o que o
 * servidor devolve, e é ele que aparece na timeline.
 */
@Component({
  selector: 'app-price-adjustment-form',
  imports: [
    ReactiveFormsModule,
    InputNumber,
    Textarea,
    Select,
    DatePicker,
    Message,
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
      title="forms.priceAdjustment"
      description="forms.priceAdjustmentHint"
      submitLabel="actions.apply"
      submitIcon="pi pi-percentage"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="40rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <p class="current">
        {{ 'forms.currentValue' | translate }}:
        <strong><app-money [amount]="currentAmount()" [currency]="currency()" /></strong>
        @if (targetName(); as name) {
          <span class="current__name">· {{ name }}</span>
        }
      </p>

      <form [formGroup]="form" class="grid">
        <app-form-field
          for="adj-amount"
          label="costs.newMonthlyAmount"
          [control]="form.controls.newMonthlyAmount"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="adj-amount"
            formControlName="newMonthlyAmount"
            mode="currency"
            [currency]="currency()"
            locale="pt-BR"
            [min]="0"
            [minFractionDigits]="2"
          />
        </app-form-field>

        <app-form-field
          for="adj-date"
          label="costs.effectiveDate"
          [control]="form.controls.effectiveDate"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="adj-date"
            formControlName="effectiveDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="adj-index"
          label="costs.index"
          [control]="form.controls.indexApplied"
        >
          <p-select
            inputId="adj-index"
            formControlName="indexApplied"
            [options]="indexOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="adj-reason"
          label="costs.reason"
          hint="forms.reasonHint"
          [control]="form.controls.reason"
          [required]="true"
          [submitted]="submitted()"
        >
          <textarea
            pTextarea
            id="adj-reason"
            formControlName="reason"
            rows="3"
            [maxlength]="maxReason"
          ></textarea>
        </app-form-field>
      </form>

      @if (preview(); as p) {
        <dl class="preview">
          <div>
            <dt>{{ 'forms.difference' | translate }}</dt>
            <dd [class.preview--up]="p.difference > 0" [class.preview--down]="p.difference < 0">
              <app-money [amount]="p.difference" [currency]="currency()" />
              ({{ p.percentage }}%)
            </dd>
          </div>
          <div>
            <dt>{{ 'forms.annualImpact' | translate }}</dt>
            <dd [class.preview--up]="p.annual > 0" [class.preview--down]="p.annual < 0">
              <app-money [amount]="p.annual" [currency]="currency()" />
            </dd>
          </div>
        </dl>

        @if (p.exceedsThreshold) {
          <p-message severity="warn" styleClass="preview__warning">
            {{ 'forms.aboveThreshold' | translate }}
          </p-message>
        }
      }
    </app-form-dialog>
  `,
  styles: `
    .current {
      margin: 0 0 1rem;
      padding-bottom: 0.75rem;
      border-bottom: 1px solid var(--app-color-border);
      font-size: 0.875rem;
      color: var(--app-color-text-muted);
    }

    .current__name {
      margin-left: 0.25rem;
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 0.75rem 1rem;
    }

    .grid__span-2 {
      grid-column: 1 / -1;
    }

    .preview {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 1rem;
      margin: 1rem 0 0;
      padding-top: 0.75rem;
      border-top: 1px solid var(--app-color-border);
    }

    .preview dt {
      font-size: 0.75rem;
      color: var(--app-color-text-muted);
    }

    .preview dd {
      margin: 0.125rem 0 0;
      font-weight: 600;
    }

    .preview--up {
      color: var(--app-color-danger);
    }

    .preview--down {
      color: var(--app-color-success);
    }

    :host ::ng-deep .preview__warning {
      width: 100%;
      margin-top: 0.75rem;
    }

    @media (max-width: 640px) {
      .grid,
      .preview {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class PriceAdjustmentFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly licenses = inject(LicenseService);
  private readonly contracts = inject(ContractApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly target = input.required<AdjustmentTarget>();
  readonly targetId = input.required<string>();
  readonly targetName = input<string | null>(null);
  readonly currentAmount = input.required<number>();
  readonly currency = input('BRL');

  readonly saved = output<PriceAdjustmentResult>();
  readonly closed = output<void>();

  protected readonly maxReason = MAX_REASON;
  protected readonly indexOptions = ADJUSTMENT_INDEX_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group({
    newMonthlyAmount: [
      0,
      [Validators.required, Validators.min(0), differentFrom(() => this.currentAmount())],
    ],
    effectiveDate: [new Date() as Date | null, Validators.required],
    indexApplied: ['NegociacaoManual' as const],
    reason: ['', [Validators.required, Validators.maxLength(MAX_REASON)]],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  private readonly amountValue = signal(0);

  constructor() {
    this.form.controls.newMonthlyAmount.valueChanges.subscribe((value) =>
      this.amountValue.set(value ?? 0),
    );

    // Ao abrir para outro alvo, parte do valor atual em vez de zero.
    effect(() => {
      if (this.visible()) {
        this.form.controls.newMonthlyAmount.setValue(this.currentAmount());
      }
    });
  }

  /**
   * Prévia local do impacto.
   *
   * Reproduz a conta do domínio para dar retorno imediato enquanto a pessoa
   * digita. Não substitui o cálculo do servidor — é ele que persiste.
   */
  protected readonly preview = computed(() => {
    const current = this.currentAmount();
    const next = this.amountValue();

    if (!next || next === current) {
      return null;
    }

    const difference = next - current;
    const percentage = current === 0 ? 0 : (difference / current) * 100;

    return {
      difference,
      annual: difference * 12,
      percentage: percentage.toFixed(2).replace('.', ','),
      exceedsThreshold: Math.abs(percentage) > 10,
    };
  });

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const command: ApplyPriceAdjustmentCommand = {
      newMonthlyAmount: value.newMonthlyAmount,
      effectiveDate: toDateOnly(value.effectiveDate)!,
      reason: value.reason.trim(),
      indexApplied: value.indexApplied,
    };

    const id = this.targetId();

    const outcome = await this.runner.run(
      () =>
        this.target() === 'license'
          ? this.licenses.applyPriceAdjustment(id, command)
          : this.contracts.applyPriceAdjustment(id, command),
      { successMessage: 'feedback.adjustmentApplied' },
    );

    if (outcome.ok) {
      this.saved.emit(outcome.value!);
      this.reset();
    }
  }

  protected close(): void {
    this.closed.emit();
    this.reset();
  }

  private reset(): void {
    this.form.reset({
      newMonthlyAmount: this.currentAmount(),
      effectiveDate: new Date(),
      indexApplied: 'NegociacaoManual',
      reason: '',
    });
    this.submittedState.set(false);
    this.runner.reset();
  }
}
