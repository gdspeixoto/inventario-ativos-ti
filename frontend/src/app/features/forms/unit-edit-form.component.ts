import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { OrganizationApiService } from '@app/features/services/business.service';
import type { OrganizationalUnitDetail, UpdateOrganizationalUnitCommand } from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { CommandRunner } from './command-runner';
import { trimmedOrNull } from './form-utils';

const MAX_NAME = 200;

/**
 * Correção do cadastro de uma unidade organizacional.
 *
 * Código e tipo ficam de fora, e não por esquecimento. O código compõe o
 * caminho materializado de toda a subárvore (`/CONTOSO/NEG-TEC/AT-DIGITAL/`),
 * e o tipo define a posição na hierarquia — alterar qualquer um exigiria
 * reescrever os caminhos de todos os descendentes. Isso é uma reorganização,
 * não uma correção de digitação, e merece operação própria.
 */
@Component({
  selector: 'app-unit-edit-form',
  imports: [
    ReactiveFormsModule,
    InputText,
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
      title="forms.editUnit"
      description="forms.editUnitHint"
      submitLabel="actions.save"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="40rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="stack">
        <app-form-field
          for="unit-edit-name"
          label="organization.unitName"
          [control]="form.controls.name"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="unit-edit-name" formControlName="name" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="unit-edit-description"
          label="organization.unitDescription"
          [control]="form.controls.description"
        >
          <textarea
            pTextarea
            id="unit-edit-description"
            formControlName="description"
            rows="3"
          ></textarea>
        </app-form-field>
      </form>

      <p class="note">{{ 'forms.editUnitFixedHint' | translate }}</p>
    </app-form-dialog>
  `,
  styles: `
    .stack {
      display: grid;
      gap: 0.75rem;
    }

    .note {
      margin: 1rem 0 0;
      padding-top: 0.75rem;
      border-top: 1px solid var(--app-color-border);
      color: var(--app-color-text-muted);
      font-size: 0.8125rem;
    }
  `,
})
export class UnitEditFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(OrganizationApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly unit = input.required<OrganizationalUnitDetail>();

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxName = MAX_NAME;

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
    description: [''],
  });

  private readonly submittedState = this.formBuilder.nonNullable.control(false);
  protected readonly submitted = computed(() => this.submittedState.value);

  constructor() {
    effect(() => {
      const current = this.unit();

      if (!this.visible()) {
        return;
      }

      this.form.reset({
        name: current.name,
        description: current.description ?? '',
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

    const command: UpdateOrganizationalUnitCommand = {
      name: value.name.trim(),
      description: trimmedOrNull(value.description),
    };

    const outcome = await this.runner.run(() => this.api.updateUnit(this.unit().id, command), {
      successMessage: 'feedback.unitUpdated',
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
