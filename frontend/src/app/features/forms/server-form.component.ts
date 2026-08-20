import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { ServerService } from '@app/features/services/asset.service';
import type { CreateServerCommand, OrganizationalUnitTree } from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { CommandRunner } from './command-runner';
import { CURRENCY_OPTIONS, ENVIRONMENT_OPTIONS, SERVER_TYPE_OPTIONS } from './select-options';
import { flattenUnits } from './license-form.component';
import { trimmedOrNull } from './form-utils';

const MAX_NAME = 200;
const MAX_CODE = 60;
const MAX_HOSTNAME = 253;

@Component({
  selector: 'app-server-form',
  imports: [
    ReactiveFormsModule,
    InputText,
    InputNumber,
    Textarea,
    Select,
    FormDialogComponent,
    FormFieldComponent,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.newServer"
      description="forms.newServerHint"
      submitLabel="actions.create"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="54rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="grid">
        <app-form-field
          for="srv-name"
          label="servers.name"
          [control]="form.controls.name"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="srv-name" formControlName="name" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="srv-code"
          label="servers.code"
          [control]="form.controls.code"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="srv-code" formControlName="code" [maxlength]="maxCode" />
        </app-form-field>

        <app-form-field
          for="srv-hostname"
          label="servers.hostname"
          [control]="form.controls.hostname"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="srv-hostname" formControlName="hostname" [maxlength]="maxHostname" />
        </app-form-field>

        <app-form-field
          for="srv-unit"
          label="servers.unit"
          [control]="form.controls.organizationalUnitId"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="srv-unit"
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
          for="srv-type"
          label="servers.type"
          [control]="form.controls.serverType"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="srv-type"
            formControlName="serverType"
            [options]="typeOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="srv-env"
          label="servers.environment"
          [control]="form.controls.environment"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="srv-env"
            formControlName="environment"
            [options]="environmentOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="srv-cost"
          label="servers.infrastructureCost"
          [control]="form.controls.infrastructureMonthlyCost"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="srv-cost"
            formControlName="infrastructureMonthlyCost"
            mode="currency"
            [currency]="form.controls.currency.value"
            locale="pt-BR"
            [min]="0"
            [minFractionDigits]="2"
          />
        </app-form-field>

        <app-form-field for="srv-currency" label="costs.currency" [control]="form.controls.currency">
          <p-select
            inputId="srv-currency"
            formControlName="currency"
            [options]="currencyOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field for="srv-cpu" label="servers.cpu" [control]="form.controls.cpuCores">
          <p-inputnumber inputId="srv-cpu" formControlName="cpuCores" [min]="0" [showButtons]="true" />
        </app-form-field>

        <app-form-field for="srv-memory" label="servers.memory" [control]="form.controls.memoryGb">
          <p-inputnumber inputId="srv-memory" formControlName="memoryGb" [min]="0" [showButtons]="true" />
        </app-form-field>

        <app-form-field for="srv-storage" label="servers.storage" [control]="form.controls.storageGb">
          <p-inputnumber
            inputId="srv-storage"
            formControlName="storageGb"
            [min]="0"
            [showButtons]="true"
          />
        </app-form-field>

        <app-form-field for="srv-ip" label="servers.primaryIp" [control]="form.controls.primaryIp">
          <input pInputText id="srv-ip" formControlName="primaryIp" />
        </app-form-field>

        <app-form-field for="srv-os" label="servers.operatingSystem" [control]="form.controls.operatingSystem">
          <input pInputText id="srv-os" formControlName="operatingSystem" />
        </app-form-field>

        <app-form-field for="srv-osv" label="servers.osVersion" [control]="form.controls.operatingSystemVersion">
          <input pInputText id="srv-osv" formControlName="operatingSystemVersion" />
        </app-form-field>

        <app-form-field for="srv-provider" label="servers.provider" [control]="form.controls.provider">
          <input pInputText id="srv-provider" formControlName="provider" />
        </app-form-field>

        <app-form-field for="srv-region" label="servers.region" [control]="form.controls.regionOrDatacenter">
          <input pInputText id="srv-region" formControlName="regionOrDatacenter" />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="srv-description"
          label="servers.description"
          [control]="form.controls.description"
        >
          <textarea pTextarea id="srv-description" formControlName="description" rows="2"></textarea>
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
export class ServerFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ServerService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly units = input<readonly OrganizationalUnitTree[]>([]);

  readonly saved = output<string>();
  readonly closed = output<void>();

  protected readonly maxName = MAX_NAME;
  protected readonly maxCode = MAX_CODE;
  protected readonly maxHostname = MAX_HOSTNAME;
  protected readonly typeOptions = SERVER_TYPE_OPTIONS;
  protected readonly environmentOptions = ENVIRONMENT_OPTIONS;
  protected readonly currencyOptions = CURRENCY_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
    code: ['', [Validators.required, Validators.maxLength(MAX_CODE)]],
    hostname: ['', [Validators.required, Validators.maxLength(MAX_HOSTNAME)]],
    organizationalUnitId: ['', Validators.required],
    serverType: ['Virtual' as const, Validators.required],
    environment: ['Producao' as const, Validators.required],
    infrastructureMonthlyCost: [0, [Validators.required, Validators.min(0)]],
    currency: ['BRL'],
    description: [''],
    operatingSystem: [''],
    operatingSystemVersion: [''],
    provider: [''],
    regionOrDatacenter: [''],
    primaryIp: [''],
    cpuCores: [null as number | null],
    memoryGb: [null as number | null],
    storageGb: [null as number | null],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  protected readonly unitOptions = computed(() => flattenUnits(this.units()));

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const command: CreateServerCommand = {
      name: value.name.trim(),
      code: value.code.trim(),
      hostname: value.hostname.trim(),
      organizationalUnitId: value.organizationalUnitId,
      serverType: value.serverType,
      environment: value.environment,
      infrastructureMonthlyCost: value.infrastructureMonthlyCost,
      currency: value.currency,
      description: trimmedOrNull(value.description),
      operatingSystem: trimmedOrNull(value.operatingSystem),
      operatingSystemVersion: trimmedOrNull(value.operatingSystemVersion),
      provider: trimmedOrNull(value.provider),
      regionOrDatacenter: trimmedOrNull(value.regionOrDatacenter),
      primaryIp: trimmedOrNull(value.primaryIp),
      cpuCores: value.cpuCores,
      memoryGb: value.memoryGb,
      storageGb: value.storageGb,
    };

    const outcome = await this.runner.run(() => this.api.create(command), {
      successMessage: 'feedback.serverCreated',
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
      serverType: 'Virtual',
      environment: 'Producao',
      infrastructureMonthlyCost: 0,
    });
    this.submittedState.set(false);
    this.runner.reset();
  }
}
