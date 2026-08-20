import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Checkbox } from 'primeng/checkbox';
import { DatePicker } from 'primeng/datepicker';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { LicenseService } from '@app/features/services/asset.service';
import { OrganizationApiService } from '@app/features/services/business.service';
import type {
  ManagementAssignment,
  OrganizationalUnitTree,
  OrganizationalUnitType,
} from '@app/features/models';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { CommandRunner } from './command-runner';
import { MANAGEMENT_ROLE_OPTIONS, UNIT_TYPE_OPTIONS } from './select-options';
import { flattenUnits } from './license-form.component';
import { toDateOnly, trimmedOrNull } from './form-utils';

const MAX_NAME = 200;
const MAX_CODE = 40;

/** Código de unidade: o domínio aceita apenas letras, números, hífen e underline. */
const UNIT_CODE_PATTERN = /^[A-Za-z0-9\-_]+$/u;

@Component({
  selector: 'app-unit-form',
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
      title="forms.newUnit"
      submitLabel="actions.create"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="42rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="grid">
        <app-form-field
          for="unit-name"
          label="organization.unitName"
          [control]="form.controls.name"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="unit-name" formControlName="name" [maxlength]="maxName" />
        </app-form-field>

        <app-form-field
          for="unit-code"
          label="organization.unitCode"
          hint="organization.unitCodeHint"
          [control]="form.controls.code"
          [required]="true"
          [submitted]="submitted()"
        >
          <input pInputText id="unit-code" formControlName="code" [maxlength]="maxCode" />
        </app-form-field>

        <app-form-field
          for="unit-parent"
          label="organization.parent"
          [control]="form.controls.parentId"
        >
          <p-select
            inputId="unit-parent"
            formControlName="parentId"
            [options]="unitOptions()"
            optionLabel="label"
            optionValue="value"
            [filter]="true"
            filterBy="label"
            [showClear]="true"
            appendTo="body"
            placeholder="—"
          />
        </app-form-field>

        <app-form-field
          for="unit-type"
          label="organization.unitType"
          [control]="form.controls.type"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="unit-type"
            formControlName="type"
            [options]="typeOptions()"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          class="grid__span-2"
          for="unit-description"
          label="organization.description"
          [control]="form.controls.description"
        >
          <textarea pTextarea id="unit-description" formControlName="description" rows="2"></textarea>
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
export class UnitFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(OrganizationApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly units = input<readonly OrganizationalUnitTree[]>([]);

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxName = MAX_NAME;
  protected readonly maxCode = MAX_CODE;

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(MAX_NAME)]],
    code: [
      '',
      [Validators.required, Validators.maxLength(MAX_CODE), Validators.pattern(UNIT_CODE_PATTERN)],
    ],
    type: ['Negocio' as OrganizationalUnitType, Validators.required],
    parentId: [null as string | null],
    description: [''],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  private readonly parentValue = signal<string | null>(null);

  protected readonly unitOptions = computed(() => flattenUnits(this.units()));

  /**
   * Tipos possíveis dado o pai escolhido.
   *
   * Sem pai, o domínio exige raiz (Matriz ou Filial). Com pai, exige um nível
   * estritamente mais profundo. Filtrar aqui poupa o usuário de descobrir a
   * regra por tentativa e erro.
   */
  protected readonly typeOptions = computed(() => {
    const parentId = this.parentValue();

    if (!parentId) {
      return UNIT_TYPE_OPTIONS.filter(
        (option) => option.value === 'Matriz' || option.value === 'Filial',
      );
    }

    const parent = findUnit(this.units(), parentId);

    if (!parent) {
      return UNIT_TYPE_OPTIONS;
    }

    const parentIndex = UNIT_TYPE_OPTIONS.findIndex((option) => option.value === parent.type);
    return UNIT_TYPE_OPTIONS.slice(parentIndex + 1);
  });

  constructor() {
    this.form.controls.parentId.valueChanges.subscribe((value) => {
      this.parentValue.set(value);

      // O tipo atual pode ter deixado de ser válido para o novo pai.
      const allowed = this.typeOptions();

      if (!allowed.some((option) => option.value === this.form.controls.type.value)) {
        this.form.controls.type.setValue(allowed[0]?.value ?? 'Negocio');
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
        this.api.createUnit({
          name: value.name.trim(),
          code: value.code.trim(),
          type: value.type,
          parentId: value.parentId,
          description: trimmedOrNull(value.description),
        }),
      { successMessage: 'feedback.unitCreated' },
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
    this.form.reset({ type: 'Matriz', parentId: null });
    this.submittedState.set(false);
    this.runner.reset();
  }
}

function findUnit(
  units: readonly OrganizationalUnitTree[],
  id: string,
): OrganizationalUnitTree | null {
  for (const unit of units) {
    if (unit.id === id) {
      return unit;
    }

    const found = findUnit(unit.children ?? [], id);

    if (found) {
      return found;
    }
  }

  return null;
}

/** Criação de vínculo de gestão. */
@Component({
  selector: 'app-assignment-form',
  imports: [
    ReactiveFormsModule,
    Select,
    DatePicker,
    Checkbox,
    FormDialogComponent,
    FormFieldComponent,
    TranslatePipe,
  ],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.newAssignment"
      submitLabel="actions.create"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="42rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="grid">
        <app-form-field
          for="asg-user"
          label="organization.user"
          [control]="form.controls.userId"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="asg-user"
            formControlName="userId"
            [options]="userOptions()"
            optionLabel="label"
            optionValue="value"
            [filter]="true"
            filterBy="label"
            appendTo="body"
            placeholder="—"
          />
        </app-form-field>

        <app-form-field
          for="asg-unit"
          label="organization.parent"
          [control]="form.controls.organizationalUnitId"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="asg-unit"
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
          for="asg-role"
          label="organization.role"
          [control]="form.controls.role"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-select
            inputId="asg-role"
            formControlName="role"
            [options]="roleOptions"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field
          for="asg-start"
          label="organization.startDate"
          [control]="form.controls.startDate"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="asg-start"
            formControlName="startDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <div class="checks grid__span-2">
          <div class="check">
            <p-checkbox formControlName="isPrimary" [binary]="true" inputId="asg-primary" />
            <label for="asg-primary">{{ 'organization.isPrimary' | translate }}</label>
          </div>

          <div class="check">
            <p-checkbox
              formControlName="includesDescendants"
              [binary]="true"
              inputId="asg-descendants"
            />
            <label for="asg-descendants">
              {{ 'organization.includesDescendants' | translate }}
            </label>
          </div>
        </div>
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

    .checks {
      display: flex;
      flex-wrap: wrap;
      gap: 1.25rem;
      padding-top: 0.25rem;
    }

    .check {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .check label {
      font-size: 0.875rem;
      cursor: pointer;
    }

    @media (max-width: 640px) {
      .grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class AssignmentFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(OrganizationApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly units = input<readonly OrganizationalUnitTree[]>([]);

  /**
   * Usuários elegíveis.
   *
   * Não existe endpoint de listagem de usuários, então a origem são os vínculos
   * já existentes — quem nunca teve vínculo não aparece. É uma limitação da
   * API, não uma escolha de interface.
   */
  readonly assignments = input<readonly ManagementAssignment[]>([]);

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly roleOptions = MANAGEMENT_ROLE_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group({
    userId: ['', Validators.required],
    organizationalUnitId: ['', Validators.required],
    role: ['Gerente' as const, Validators.required],
    startDate: [new Date() as Date | null, Validators.required],
    isPrimary: [false],
    includesDescendants: [true],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  protected readonly unitOptions = computed(() => flattenUnits(this.units()));

  protected readonly userOptions = computed(() => {
    const seen = new Map<string, string>();

    for (const assignment of this.assignments()) {
      seen.set(assignment.userId, assignment.userDisplayName);
    }

    return [...seen].map(([value, label]) => ({ label, value }));
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
        this.api.createAssignment({
          userId: value.userId,
          organizationalUnitId: value.organizationalUnitId,
          role: value.role,
          startDate: toDateOnly(value.startDate)!,
          isPrimary: value.isPrimary,
          includesDescendants: value.includesDescendants,
        }),
      { successMessage: 'feedback.assignmentCreated' },
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
    this.form.reset({
      role: 'Gerente',
      startDate: new Date(),
      isPrimary: false,
      includesDescendants: true,
    });
    this.submittedState.set(false);
    this.runner.reset();
  }
}

/** Encerramento de vínculo de gestão. */
@Component({
  selector: 'app-finish-assignment-form',
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
      title="forms.finishAssignment"
      submitLabel="actions.finish"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="36rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <form [formGroup]="form" class="stack">
        <app-form-field
          for="fin-date"
          label="organization.endDate"
          [control]="form.controls.endDate"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-datepicker
            inputId="fin-date"
            formControlName="endDate"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            appendTo="body"
          />
        </app-form-field>

        <app-form-field for="fin-notes" label="organization.notes" [control]="form.controls.notes">
          <textarea pTextarea id="fin-notes" formControlName="notes" rows="3"></textarea>
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
export class FinishAssignmentFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(OrganizationApiService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly assignmentId = input.required<string>();

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly form = this.formBuilder.nonNullable.group({
    endDate: [new Date() as Date | null, Validators.required],
    notes: [''],
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
        this.api.finishAssignment(this.assignmentId(), {
          endDate: toDateOnly(value.endDate)!,
          notes: trimmedOrNull(value.notes),
        }),
      { successMessage: 'feedback.assignmentFinished' },
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
    this.form.reset({ endDate: new Date(), notes: '' });
    this.submittedState.set(false);
    this.runner.reset();
  }
}

/** Atualização da quantidade de licenças em uso. */
@Component({
  selector: 'app-usage-form',
  imports: [ReactiveFormsModule, InputNumber, FormDialogComponent, FormFieldComponent, TranslatePipe],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      title="forms.updateUsage"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="32rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      <p class="context">
        {{ 'licenses.contracted' | translate }}: <strong>{{ contractedQuantity() }}</strong>
      </p>

      <form [formGroup]="form">
        <app-form-field
          for="usg-used"
          label="licenses.used"
          [control]="form.controls.usedQuantity"
          [required]="true"
          [submitted]="submitted()"
        >
          <p-inputnumber
            inputId="usg-used"
            formControlName="usedQuantity"
            [min]="0"
            [max]="contractedQuantity()"
            [showButtons]="true"
          />
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
  `,
})
export class UsageFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(LicenseService);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly licenseId = input.required<string>();
  readonly contractedQuantity = input.required<number>();
  readonly currentUsage = input(0);

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly form = this.formBuilder.nonNullable.group({
    usedQuantity: [0, [Validators.required, Validators.min(0)]],
  });

  private readonly submittedState = signal(false);
  protected readonly submitted = this.submittedState.asReadonly();

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.form.controls.usedQuantity.setValue(this.currentUsage());
        this.form.controls.usedQuantity.setValidators([
          Validators.required,
          Validators.min(0),
          Validators.max(this.contractedQuantity()),
        ]);
        this.form.controls.usedQuantity.updateValueAndValidity();
      }
    });
  }

  protected async save(): Promise<void> {
    this.submittedState.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const outcome = await this.runner.run(
      () => this.api.updateUsage(this.licenseId(), this.form.getRawValue().usedQuantity),
      { successMessage: 'feedback.usageUpdated' },
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
    this.form.reset({ usedQuantity: this.currentUsage() });
    this.submittedState.set(false);
    this.runner.reset();
  }
}
