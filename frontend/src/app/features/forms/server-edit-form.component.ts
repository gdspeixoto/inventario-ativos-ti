import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { ServerService } from '@app/features/services/asset.service';
import type { ServerDetail, UpdateServerCommand } from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { CommandRunner } from './command-runner';
import { ENVIRONMENT_OPTIONS, SERVER_TYPE_OPTIONS } from './select-options';
import { trimmedOrNull } from './form-utils';

const MAX_NAME = 200;
const MAX_CODE = 60;
const MAX_HOSTNAME = 253;

/**
 * Correção dos dados cadastrais de um servidor.
 *
 * Não inclui custos nem recursos: ambos mudam por operações próprias, que
 * exigem justificativa e alimentam o histórico financeiro. Trazê-los para cá
 * faria com que corrigir um hostname reenviasse valores que ninguém pediu para
 * alterar.
 *
 * Sobre o ambiente: mudá-lo aqui significa que o servidor foi cadastrado na
 * coluna errada. Se a máquina de fato migrou de homologação para produção,
 * isso é um evento operacional e merece registro próprio — não uma correção
 * silenciosa.
 */
@Component({
  selector: 'app-server-edit-form',
  imports: [
    ReactiveFormsModule,
    InputText,
    Textarea,
    Select,
    FormDialogComponent,
    FormFieldComponent,
    TranslatePipe,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.editServer"
      description="forms.editServerHint"
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
          for="srv-edit-name"
          label="servers.name"
          [control]="form.controls.name"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="srv-edit-name" formControlName="name" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="srv-edit-code"
          label="servers.code"
          [control]="form.controls.code"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="srv-edit-code" formControlName="code" [maxlength]="maxCode" />
        </app-form-field>

        <app-form-field
          for="srv-edit-hostname"
          label="servers.hostname"
          [control]="form.controls.hostname"
          [required]="true"
          [submitted]="submitted()"
        >
          <input
            pInputText
            id="srv-edit-hostname"
            formControlName="hostname"
            [maxlength]="maxHostname"
          />
        </app-form-field>

        <app-form-field
          for="srv-edit-type"
          label="servers.type"
          [control]="form.controls.serverType"
        >
          <p-select
            inputId="srv-edit-type"
            formControlName="serverType"
            [options]="serverTypeOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="srv-edit-env"
          label="servers.environment"
          hint="forms.environmentHint"
          [control]="form.controls.environment"
        >
          <p-select
            inputId="srv-edit-env"
            formControlName="environment"
            [options]="environmentOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field for="srv-edit-os" label="servers.os" [control]="form.controls.operatingSystem">
          <input pInputText id="srv-edit-os" formControlName="operatingSystem" />
        </app-form-field>

        <app-form-field
          for="srv-edit-os-version"
          label="servers.osVersion"
          [control]="form.controls.operatingSystemVersion"
        >
          <input pInputText id="srv-edit-os-version" formControlName="operatingSystemVersion" />
        </app-form-field>

        <app-form-field
          for="srv-edit-provider"
          label="servers.provider"
          [control]="form.controls.provider"
        >
          <input pInputText id="srv-edit-provider" formControlName="provider" />
        </app-form-field>

        <app-form-field
          for="srv-edit-region"
          label="servers.region"
          [control]="form.controls.regionOrDatacenter"
        >
          <input pInputText id="srv-edit-region" formControlName="regionOrDatacenter" />
        </app-form-field>

        <app-form-field for="srv-edit-ip" label="servers.primaryIp" [control]="form.controls.primaryIp">
          <input pInputText id="srv-edit-ip" formControlName="primaryIp" />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="srv-edit-description"
          label="servers.description"
          [control]="form.controls.description"
        >
          <textarea
            pTextarea
            id="srv-edit-description"
            formControlName="description"
            rows="2"
          ></textarea>
        </app-form-field>
      </form>

      <p class="note">{{ 'forms.editServerHint' | translate }}</p>
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
export class ServerEditFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ServerService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly server = input.required<ServerDetail>();

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxName = MAX_NAME;
  protected readonly maxCode = MAX_CODE;
  protected readonly maxHostname = MAX_HOSTNAME;
  protected readonly serverTypeOptions = SERVER_TYPE_OPTIONS;
  protected readonly environmentOptions = ENVIRONMENT_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
    code: ['', [Validators.required, Validators.maxLength(MAX_CODE)]],
    hostname: ['', [Validators.required, Validators.maxLength(MAX_HOSTNAME)]],
    serverType: ['Virtual' as UpdateServerCommand['serverType'], Validators.required],
    environment: ['Producao' as UpdateServerCommand['environment'], Validators.required],
    description: [''],
    operatingSystem: [''],
    operatingSystemVersion: [''],
    provider: [''],
    regionOrDatacenter: [''],
    primaryIp: [''],
  });

  private readonly submittedState = this.formBuilder.nonNullable.control(false);
  protected readonly submitted = computed(() => this.submittedState.value);

  constructor() {
    // Recarrega do registro atual a cada abertura: uma edição cancelada não
    // pode sobreviver para a próxima vez que o modal abrir.
    effect(() => {
      const current = this.server();

      if (!this.visible()) {
        return;
      }

      this.form.reset({
        name: current.name,
        code: current.code,
        hostname: current.hostname,
        serverType: current.serverType,
        environment: current.environment,
        description: current.description ?? '',
        operatingSystem: current.operatingSystem ?? '',
        operatingSystemVersion: current.operatingSystemVersion ?? '',
        provider: current.provider ?? '',
        regionOrDatacenter: current.regionOrDatacenter ?? '',
        primaryIp: current.primaryIp ?? '',
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

    const command: UpdateServerCommand = {
      name: value.name.trim(),
      code: value.code.trim(),
      hostname: value.hostname.trim(),
      serverType: value.serverType,
      environment: value.environment,
      description: trimmedOrNull(value.description),
      operatingSystem: trimmedOrNull(value.operatingSystem),
      operatingSystemVersion: trimmedOrNull(value.operatingSystemVersion),
      provider: trimmedOrNull(value.provider),
      regionOrDatacenter: trimmedOrNull(value.regionOrDatacenter),
      primaryIp: trimmedOrNull(value.primaryIp),
    };

    const outcome = await this.runner.run(() => this.api.update(this.server().id, command), {
      successMessage: 'feedback.serverUpdated',
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
