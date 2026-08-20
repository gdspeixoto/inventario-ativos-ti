import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { SupplierApiService } from '@app/features/services/business.service';
import type {
  CreateSupplierCommand,
  SupplierDetail,
  UpdateSupplierCommand,
} from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { CommandRunner } from './command-runner';
import { CONTRACT_CATEGORY_OPTIONS, SUPPLIER_STATUS_OPTIONS } from './select-options';
import { trimmedOrNull } from './form-utils';

const MAX_NAME = 200;

/**
 * Cadastro e edição de fornecedor.
 *
 * Na edição, razão social e documento aparecem bloqueados porque o backend não
 * os aceita em alteração — exibi-los editáveis prometeria algo que a API não
 * cumpre.
 *
 * Contato, e-mail, telefone e site são enviados sempre juntos: o servidor faz
 * substituição total desses quatro, então omitir um apagaria o dado gravado.
 * Por isso o formulário de edição já vem preenchido com os valores atuais.
 */
@Component({
  selector: 'app-supplier-form',
  imports: [
    ReactiveFormsModule,
    InputText,
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
      [title]="isEdit() ? 'forms.editSupplier' : 'forms.newSupplier'"
      [description]="isEdit() ? 'forms.editSupplierHint' : null"
      [submitLabel]="isEdit() ? 'common.save' : 'actions.create'"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="46rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="grid">
        <app-form-field
          class="grid__span-2"
          for="sup-name"
          label="suppliers.name"
          [control]="form.controls.name"
          [required]="!isEdit()"
          [submitted]="submitted()"
        >
          <input pInputText id="sup-name" formControlName="name" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="sup-document"
          label="suppliers.documentNumber"
          hint="suppliers.documentHint"
          [control]="form.controls.documentNumber"
        >
          <input pInputText id="sup-document" formControlName="documentNumber" />
        </app-form-field>

        <app-form-field
          for="sup-category"
          label="suppliers.category"
          [control]="form.controls.category"
        >
          <p-select
            inputId="sup-category"
            formControlName="category"
            [options]="categoryOptions"
            optionLabel="label"
            optionValue="value"
            [showClear]="true"
            appendTo="body"
            placeholder="—"
          />
        </app-form-field>

        <app-form-field for="sup-contact" label="suppliers.contact" [control]="form.controls.mainContactName">
          <input pInputText id="sup-contact" formControlName="mainContactName" />
        </app-form-field>

        <app-form-field for="sup-email" label="suppliers.email" [control]="form.controls.email">
          <input pInputText id="sup-email" type="email" formControlName="email" />
        </app-form-field>

        <app-form-field for="sup-phone" label="suppliers.phone" [control]="form.controls.phone">
          <input pInputText id="sup-phone" formControlName="phone" />
        </app-form-field>

        <app-form-field for="sup-website" label="suppliers.website" [control]="form.controls.website">
          <input pInputText id="sup-website" formControlName="website" />
        </app-form-field>

        @if (isEdit()) {
          <app-form-field for="sup-status" label="suppliers.status" [control]="form.controls.status">
            <p-select
              inputId="sup-status"
              formControlName="status"
              [options]="statusOptions"
              optionLabel="label"
              optionValue="value"
              appendTo="body"
            />
          </app-form-field>
        }

        <app-form-field
          class="grid__span-2"
          for="sup-sla"
          label="suppliers.sla"
          [control]="form.controls.slaDescription"
        >
          <textarea pTextarea id="sup-sla" formControlName="slaDescription" rows="2"></textarea>
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
export class SupplierFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(SupplierApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();

  /** Presente apenas na edição; ausente significa cadastro novo. */
  readonly supplier = input<SupplierDetail | null>(null);

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxName = MAX_NAME;
  protected readonly categoryOptions = CONTRACT_CATEGORY_OPTIONS;
  protected readonly statusOptions = SUPPLIER_STATUS_OPTIONS;

  protected readonly isEdit = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
    documentNumber: [''],
    category: [null as string | null],
    mainContactName: [''],
    email: [''],
    phone: [''],
    website: [''],
    slaDescription: [''],
    status: ['Ativo' as string],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  constructor() {
    effect(() => {
      const supplier = this.supplier();

      if (!this.visible()) {
        return;
      }

      this.isEdit.set(supplier !== null);

      if (supplier) {
        this.form.patchValue({
          name: supplier.name,
          documentNumber: supplier.documentNumber ?? '',
          category: supplier.category ?? null,
          mainContactName: supplier.mainContactName ?? '',
          email: supplier.email ?? '',
          phone: supplier.phone ?? '',
          website: supplier.website ?? '',
          slaDescription: supplier.slaDescription ?? '',
          status: supplier.status,
        });

        // O backend não altera esses dois; travá-los evita prometer o que não cumpre.
        this.form.controls.name.disable();
        this.form.controls.documentNumber.disable();
      } else {
        this.form.controls.name.enable();
        this.form.controls.documentNumber.enable();
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
    const supplier = this.supplier();

    const outcome = supplier
      ? await this.runner.run(
          () =>
            this.api.update(supplier.id, {
              mainContactName: trimmedOrNull(value.mainContactName),
              email: trimmedOrNull(value.email),
              phone: trimmedOrNull(value.phone),
              website: trimmedOrNull(value.website),
              category: value.category,
              slaDescription: trimmedOrNull(value.slaDescription),
              status: value.status,
            } as UpdateSupplierCommand),
          { successMessage: 'feedback.supplierUpdated' },
        )
      : await this.runner.run(
          () =>
            this.api.create({
              name: value.name.trim(),
              documentNumber: trimmedOrNull(value.documentNumber),
              category: value.category,
              mainContactName: trimmedOrNull(value.mainContactName),
              email: trimmedOrNull(value.email),
              phone: trimmedOrNull(value.phone),
              website: trimmedOrNull(value.website),
              slaDescription: trimmedOrNull(value.slaDescription),
            } as CreateSupplierCommand),
          { successMessage: 'feedback.supplierCreated' },
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
    this.form.enable();
    this.form.reset({ status: 'Ativo', category: null });
    this.submittedState.set(false);
    this.runner.reset();
  }
}
