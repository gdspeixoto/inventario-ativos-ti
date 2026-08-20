import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Message } from 'primeng/message';
import { Textarea } from 'primeng/textarea';
import { FormDialogComponent } from '@shared/components/form-dialog/form-dialog.component';
import { FormFieldComponent } from '@shared/components/form-field/form-field.component';
import { CommandRunner } from './command-runner';

const MAX_REASON = 1000;

/**
 * Confirmação com justificativa obrigatória.
 *
 * Atende as ações que o backend só aceita acompanhadas de motivo — desativar
 * servidor, encerrar contrato, resolver ou ignorar alerta. São operações que
 * mudam a situação de um item e ficam na timeline; sem o motivo, o registro
 * diria *o que* aconteceu sem dizer *por quê*, que é o que importa numa
 * auditoria meses depois.
 *
 * Um único componente porque a diferença entre esses casos é só o texto e a
 * chamada — replicar o formulário quatro vezes criaria quatro lugares para
 * divergir.
 */
@Component({
  selector: 'app-reason-form',
  imports: [ReactiveFormsModule, Textarea, Message, FormDialogComponent, FormFieldComponent],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-form-dialog
      [visible]="visible()"
      [title]="title()"
      [description]="description()"
      [submitLabel]="submitLabel()"
      [submitIcon]="submitIcon()"
      [submitting]="runner.submitting()"
      [error]="runner.error()"
      width="36rem"
      (confirmed)="save()"
      (cancelled)="close()"
      (visibleChange)="close()"
    >
      @if (warning(); as text) {
        <p-message severity="warn" styleClass="reason__warning">{{ text }}</p-message>
      }

      <form [formGroup]="form">
        <app-form-field
          [for]="'reason-' + fieldId()"
          [label]="fieldLabel()"
          hint="forms.reasonHint"
          [control]="form.controls.reason"
          [required]="true"
          [submitted]="submitted()"
        >
          <textarea
            pTextarea
            [id]="'reason-' + fieldId()"
            formControlName="reason"
            rows="4"
            [maxlength]="maxReason"
          ></textarea>
        </app-form-field>
      </form>
    </app-form-dialog>
  `,
  styles: `
    :host ::ng-deep .reason__warning {
      width: 100%;
      margin-bottom: 1rem;
    }
  `,
})
export class ReasonFormComponent {
  private readonly formBuilder = inject(FormBuilder);

  protected readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly fieldLabel = input('costs.reason');
  readonly fieldId = input('generic');
  readonly submitLabel = input('actions.confirm');
  readonly submitIcon = input('pi pi-check');
  readonly warning = input<string | null>(null);
  readonly successMessage = input<string | null>(null);

  /** A ação em si. Mantida fora do componente: ele não conhece a API. */
  readonly action = input.required<(reason: string) => Promise<unknown>>();

  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly maxReason = MAX_REASON;

  protected readonly form = this.formBuilder.nonNullable.group({
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

    const outcome = await this.runner.run(
      () => this.action()(this.form.getRawValue().reason.trim()),
      { successMessage: this.successMessage() ?? undefined },
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
