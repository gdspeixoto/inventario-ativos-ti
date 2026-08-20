import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { LicenseService, ServerService } from '@app/features/services/asset.service';
import { ContractApiService } from '@app/features/services/business.service';
import type { ContractStatus } from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { CommandRunner } from './command-runner';
import { CONTRACT_TERMINATION_OPTIONS, CURRENCY_OPTIONS } from './select-options';
import { differentFrom, toDateOnly, trimmedOrNull } from './form-utils';

const MAX_REASON = 1000;

/** Alteração da quantidade contratada de uma licença. */
@Component({
  selector: 'app-quantity-form',
  imports: [
    ReactiveFormsModule,
    InputNumber,
    Textarea,
    FormDialogComponent,
    FormFieldComponent,
    TranslatePipe,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.changeQuantity"
      submitLabel="actions.apply"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="36rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <p class="context">
        {{ 'licenses.contracted' | translate }}: <strong>{{ currentQuantity() }}</strong>
        · {{ 'licenses.used' | translate }}: <strong>{{ usedQuantity() }}</strong>
      </p>

      <form [formGroup]="form" class="stack">
        <app-form-field
          for="qty-new"
          label="licenses.newQuantity"
          [control]="form.controls.newQuantity"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="qty-new"
            formControlName="newQuantity"
            [min]="usedQuantity()"
            [showButtons]="true"
          />
        </app-form-field>

        <app-form-field
          for="qty-reason"
          label="costs.reason"
          hint="forms.reasonHint"
          [control]="form.controls.reason"
          [required]="true"
          [submitted]="submitted()"
        >
          <textarea pTextarea id="qty-reason" formControlName="reason" rows="3" [maxlength]="maxReason"></textarea>
        </app-form-field>
      </form>
    </app-form-dialog>
  `,
  styles: `
    .context {
      margin: 0 0 1rem;
      padding-bottom: 0.75rem;
      border-bottom: 1px solid var(--app-color-border);
      font-size: 0.875rem;
      color: var(--app-color-text-muted);
    }

    .stack {
      display: grid;
      gap: 0.75rem;
    }
  `,
})
export class QuantityFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(LicenseService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly licenseId = input.required<string>();
  readonly currentQuantity = input.required<number>();
  readonly usedQuantity = input(0);

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxReason = MAX_REASON;

  protected readonly form = this.formBuilder.nonNullable.group({
    newQuantity: [
      1,
      [Validators.required, Validators.min(1), differentFrom(() => this.currentQuantity())],
    ],
    reason: ['', [Validators.required, Validators.maxLength(MAX_REASON)]],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.form.controls.newQuantity.setValue(this.currentQuantity());
      }
    });
  }

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const outcome = await this.runner.run(
      () =>
        this.api.changeQuantity(this.licenseId(), {
          newQuantity: value.newQuantity,
          reason: value.reason.trim(),
        }),
      { successMessage: 'feedback.quantityChanged' },
    );

    if (outcome.ok) {
      this.saved.emit();
      this.reset();
    }
  }

  protected close(): void {
    this.closed.emit();
    this.reset();
  }

  private reset(): void {
    this.form.reset({ newQuantity: this.currentQuantity(), reason: '' });
    this.submittedState.set(false);
    this.runner.reset();
  }
}

/**
 * Atualização dos custos de um servidor.
 *
 * Os quatro valores substituem os atuais — não são incrementos. A moeda vem do
 * próprio servidor: enviar outra faria o domínio recusar a soma entre moedas
 * diferentes.
 */
@Component({
  selector: 'app-server-costs-form',
  imports: [
    ReactiveFormsModule,
    InputNumber,
    Textarea,
    Select,
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
      title="forms.updateCosts"
      description="forms.updateCostsHint"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="42rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="grid">
        <app-form-field
          for="cost-infra"
          label="servers.infrastructureCost"
          [control]="form.controls.infrastructureMonthlyCost"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="cost-infra"
            formControlName="infrastructureMonthlyCost"
            mode="currency"
            [currency]="form.controls.currency.value"
            locale="pt-BR"
            [min]="0"
            [minFractionDigits]="2"
          />
        </app-form-field>

        <app-form-field
          for="cost-license"
          label="servers.licenseCost"
          [control]="form.controls.licenseMonthlyCost"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="cost-license"
            formControlName="licenseMonthlyCost"
            mode="currency"
            [currency]="form.controls.currency.value"
            locale="pt-BR"
            [min]="0"
            [minFractionDigits]="2"
          />
        </app-form-field>

        <app-form-field
          for="cost-support"
          label="servers.supportCost"
          [control]="form.controls.supportMonthlyCost"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="cost-support"
            formControlName="supportMonthlyCost"
            mode="currency"
            [currency]="form.controls.currency.value"
            locale="pt-BR"
            [min]="0"
            [minFractionDigits]="2"
          />
        </app-form-field>

        <app-form-field
          for="cost-backup"
          label="servers.backupCost"
          [control]="form.controls.backupMonthlyCost"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="cost-backup"
            formControlName="backupMonthlyCost"
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
          class="grid__span-2"
          for="cost-reason"
          label="costs.reason"
          hint="forms.reasonHint"
          [control]="form.controls.reason"
          [required]="true"
          [submitted]="submitted()"
        >
          <textarea pTextarea id="cost-reason" formControlName="reason" rows="3" [maxlength]="maxReason"></textarea>
        </app-form-field>
      </form>

      <p class="total">
        {{ 'servers.totalCost' | translate }}:
        <strong><app-money [amount]="total()" [currency]="form.controls.currency.value" /></strong>
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
      font-size: 0.875rem;
      color: var(--app-color-text-muted);
    }

    @media (max-width: 640px) {
      .grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class ServerCostsFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ServerService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly serverId = input.required<string>();
  readonly currency = input('BRL');
  readonly current = input<{
    infrastructure: number;
    license: number;
    support: number;
    backup: number;
  } | null>(null);

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxReason = MAX_REASON;
  protected readonly currencyOptions = CURRENCY_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group({
    infrastructureMonthlyCost: [0, [Validators.required, Validators.min(0)]],
    licenseMonthlyCost: [0, [Validators.required, Validators.min(0)]],
    supportMonthlyCost: [0, [Validators.required, Validators.min(0)]],
    backupMonthlyCost: [0, [Validators.required, Validators.min(0)]],
    currency: ['BRL'],
    reason: ['', [Validators.required, Validators.maxLength(MAX_REASON)]],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  private readonly totals = signal({ i: 0, l: 0, s: 0, b: 0 });

  protected readonly total = computed(() => {
    const { i, l, s, b } = this.totals();
    return i + l + s + b;
  });

  constructor() {
    this.form.valueChanges.subscribe((value) =>
      this.totals.set({
        i: value.infrastructureMonthlyCost ?? 0,
        l: value.licenseMonthlyCost ?? 0,
        s: value.supportMonthlyCost ?? 0,
        b: value.backupMonthlyCost ?? 0,
      }),
    );

    effect(() => {
      const current = this.current();

      if (this.visible() && current) {
        this.form.patchValue({
          infrastructureMonthlyCost: current.infrastructure,
          licenseMonthlyCost: current.license,
          supportMonthlyCost: current.support,
          backupMonthlyCost: current.backup,
          currency: this.currency(),
        });
      }
    });
  }

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const outcome = await this.runner.run(
      () =>
        this.api.updateCosts(this.serverId(), {
          infrastructureMonthlyCost: value.infrastructureMonthlyCost,
          licenseMonthlyCost: value.licenseMonthlyCost,
          supportMonthlyCost: value.supportMonthlyCost,
          backupMonthlyCost: value.backupMonthlyCost,
          currency: value.currency,
          reason: value.reason.trim(),
        }),
      { successMessage: 'feedback.costsUpdated' },
    );

    if (outcome.ok) {
      this.saved.emit();
      this.reset();
    }
  }

  protected close(): void {
    this.closed.emit();
    this.reset();
  }

  private reset(): void {
    this.form.reset({ currency: this.currency(), reason: '' });
    this.submittedState.set(false);
    this.runner.reset();
  }
}

/** Redimensionamento de servidor: campos vazios mantêm o valor atual. */
@Component({
  selector: 'app-resize-form',
  imports: [
    ReactiveFormsModule,
    InputNumber,
    Textarea,
    FormDialogComponent,
    FormFieldComponent,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.resizeServer"
      description="forms.resizeServerHint"
      submitLabel="actions.apply"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="38rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="grid">
        <app-form-field for="rsz-cpu" label="servers.cpu" [control]="form.controls.cpuCores">
          <p-inputnumber inputId="rsz-cpu" formControlName="cpuCores" [min]="0" [showButtons]="true" />
        </app-form-field>

        <app-form-field for="rsz-memory" label="servers.memory" [control]="form.controls.memoryGb">
          <p-inputnumber inputId="rsz-memory" formControlName="memoryGb" [min]="0" [showButtons]="true" />
        </app-form-field>

        <app-form-field for="rsz-storage" label="servers.storage" [control]="form.controls.storageGb">
          <p-inputnumber inputId="rsz-storage" formControlName="storageGb" [min]="0" [showButtons]="true" />
        </app-form-field>

        <app-form-field
          class="grid__span-full"
          for="rsz-reason"
          label="costs.reason"
          hint="forms.reasonHint"
          [control]="form.controls.reason"
          [required]="true"
          [submitted]="submitted()"
        >
          <textarea pTextarea id="rsz-reason" formControlName="reason" rows="3" [maxlength]="maxReason"></textarea>
        </app-form-field>
      </form>
    </app-form-dialog>
  `,
  styles: `
    .grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 0.75rem 1rem;
    }

    .grid__span-full {
      grid-column: 1 / -1;
    }

    @media (max-width: 640px) {
      .grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class ResizeFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ServerService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly serverId = input.required<string>();
  readonly current = input<{ cpu: number | null; memory: number | null; storage: number | null } | null>(
    null,
  );

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxReason = MAX_REASON;

  protected readonly form = this.formBuilder.nonNullable.group({
    cpuCores: [null as number | null],
    memoryGb: [null as number | null],
    storageGb: [null as number | null],
    reason: ['', [Validators.required, Validators.maxLength(MAX_REASON)]],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  constructor() {
    effect(() => {
      const current = this.current();

      if (this.visible() && current) {
        this.form.patchValue({
          cpuCores: current.cpu,
          memoryGb: current.memory,
          storageGb: current.storage,
        });
      }
    });
  }

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const outcome = await this.runner.run(
      () =>
        this.api.resize(this.serverId(), {
          cpuCores: value.cpuCores,
          memoryGb: value.memoryGb,
          storageGb: value.storageGb,
          reason: value.reason.trim(),
        }),
      { successMessage: 'feedback.serverResized' },
    );

    if (outcome.ok) {
      this.saved.emit();
      this.reset();
    }
  }

  protected close(): void {
    this.closed.emit();
    this.reset();
  }

  private reset(): void {
    this.form.reset({ reason: '' });
    this.submittedState.set(false);
    this.runner.reset();
  }
}

/** Renovação de contrato: estende a vigência sem tocar em valores. */
@Component({
  selector: 'app-renew-contract-form',
  imports: [
    ReactiveFormsModule,
    DatePicker,
    Textarea,
    FormDialogComponent,
    FormFieldComponent,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.renewContract"
      description="forms.renewContractHint"
      submitLabel="actions.renew"
      submitIcon="pi pi-refresh"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="36rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="stack">
        <app-form-field
          for="rnw-date"
          label="contracts.newEndDate"
          [control]="form.controls.newEndDate"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="rnw-date"
            formControlName="newEndDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            [minDate]="minDate()"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field for="rnw-reason" label="costs.reason" [control]="form.controls.reason">
          <textarea pTextarea id="rnw-reason" formControlName="reason" rows="3" [maxlength]="maxReason"></textarea>
        </app-form-field>
      </form>
    </app-form-dialog>
  `,
  styles: `
    .stack {
      display: grid;
      gap: 0.75rem;
    }
  `,
})
export class RenewContractFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ContractApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly contractId = input.required<string>();

  /** Fim da vigência atual: a nova data precisa ser posterior a ela. */
  readonly currentEndDate = input<string | null>(null);

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxReason = MAX_REASON;

  protected readonly form = this.formBuilder.nonNullable.group({
    newEndDate: [null as Date | null, Validators.required],
    reason: [''],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  protected readonly minDate = computed(() => {
    const current = this.currentEndDate();

    if (!current) {
      return new Date();
    }

    const [year, month, day] = current.split('-').map(Number);
    const date = new Date(year, month - 1, day);
    date.setDate(date.getDate() + 1);

    return date;
  });

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const outcome = await this.runner.run(
      () =>
        this.api.renew(this.contractId(), {
          newEndDate: toDateOnly(value.newEndDate)!,
          reason: trimmedOrNull(value.reason),
        }),
      { successMessage: 'feedback.contractRenewed' },
    );

    if (outcome.ok) {
      this.saved.emit();
      this.reset();
    }
  }

  protected close(): void {
    this.closed.emit();
    this.reset();
  }

  private reset(): void {
    this.form.reset({ newEndDate: null, reason: '' });
    this.submittedState.set(false);
    this.runner.reset();
  }
}

/**
 * Encerramento de contrato.
 *
 * A lista de situações é menor que `ContractStatus` de propósito: o domínio só
 * aceita Encerrado, Cancelado ou Suspenso, e oferecer as demais levaria a um
 * erro previsível.
 */
@Component({
  selector: 'app-terminate-contract-form',
  imports: [
    ReactiveFormsModule,
    Select,
    Textarea,
    FormDialogComponent,
    FormFieldComponent,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.terminateContract"
      submitLabel="actions.terminate"
      submitIcon="pi pi-ban"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="36rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="stack">
        <app-form-field
          for="trm-status"
          label="contracts.status"
          [control]="form.controls.status"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="trm-status"
            formControlName="status"
            [options]="statusOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="trm-reason"
          label="costs.reason"
          hint="forms.reasonHint"
          [control]="form.controls.reason"
          [required]="true"
          [submitted]="submitted()"
        >
          <textarea pTextarea id="trm-reason" formControlName="reason" rows="3" [maxlength]="maxReason"></textarea>
        </app-form-field>
      </form>
    </app-form-dialog>
  `,
  styles: `
    .stack {
      display: grid;
      gap: 0.75rem;
    }
  `,
})
export class TerminateContractFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ContractApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly contractId = input.required<string>();

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxReason = MAX_REASON;
  protected readonly statusOptions = CONTRACT_TERMINATION_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group({
    status: ['Encerrado' as ContractStatus, Validators.required],
    reason: ['', [Validators.required, Validators.maxLength(MAX_REASON)]],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const outcome = await this.runner.run(
      () =>
        this.api.terminate(this.contractId(), {
          status: value.status,
          reason: value.reason.trim(),
        }),
      { successMessage: 'feedback.contractTerminated' },
    );

    if (outcome.ok) {
      this.saved.emit();
      this.reset();
    }
  }

  protected close(): void {
    this.closed.emit();
    this.reset();
  }

  private reset(): void {
    this.form.reset({ status: 'Encerrado', reason: '' });
    this.submittedState.set(false);
    this.runner.reset();
  }
}
