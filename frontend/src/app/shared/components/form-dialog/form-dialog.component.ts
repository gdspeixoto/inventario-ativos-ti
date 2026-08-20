import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Message } from 'primeng/message';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import type { AppHttpError } from '@core/errors/http-error.model';

/**
 * Moldura comum a todo formulário em modal.
 *
 * Concentra o que, repetido em cada tela, acabaria divergindo: o botão de
 * salvar desabilitado enquanto a requisição está em voo, a exibição do erro
 * vindo do servidor e o bloqueio do fechamento durante o envio — fechar o
 * modal no meio de um POST deixaria o usuário sem saber se a operação
 * aconteceu.
 */
@Component({
  selector: 'app-form-dialog',
  imports: [Dialog, Button, Message, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [closable]="!submitting()"
      [closeOnEscape]="!submitting()"
      [style]="{ width: width() }"
      [breakpoints]="{ '960px': '90vw' }"
      [header]="title() | translate"
      [dismissableMask]="!submitting()"
    >
      @if (description(); as text) {
        <p class="form-dialog__description">{{ text | translate }}</p>
      }

      @if (errorMessage(); as message) {
        <p-message severity="error" styleClass="form-dialog__error">{{ message }}</p-message>
      }

      <div class="form-dialog__body">
        <ng-content />
      </div>

      <ng-template #footer>
        <p-button
          [label]="'common.cancel' | translate"
          severity="secondary"
          [text]="true"
          [disabled]="submitting()"
          (onClick)="cancelled.emit()"
        />
        <p-button
          [label]="submitLabel() | translate"
          [icon]="submitIcon()"
          [loading]="submitting()"
          [disabled]="submitDisabled()"
          (onClick)="confirmed.emit()"
        />
      </ng-template>
    </p-dialog>
  `,
  styles: `
    .form-dialog__description {
      margin: 0 0 1rem;
      color: var(--app-color-text-muted);
      font-size: 0.875rem;
      line-height: 1.5;
    }

    .form-dialog__body {
      display: grid;
      gap: 1rem;
    }

    :host ::ng-deep .form-dialog__error {
      width: 100%;
      margin-bottom: 1rem;
    }
  `,
})
export class FormDialogComponent {
  readonly visible = input.required<boolean>();
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly submitting = input(false);
  readonly submitDisabled = input(false);
  readonly submitLabel = input('common.save');
  readonly submitIcon = input('pi pi-check');
  readonly width = input('42rem');

  /** Erro da última tentativa de envio, já normalizado pelo interceptor. */
  readonly error = input<AppHttpError | null>(null);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
  readonly visibleChange = output<boolean>();

  /**
   * Mensagem do servidor, quando existe.
   *
   * O backend responde 422 com texto pronto para leitura (`detail` do
   * ProblemDetails). Traduzir por cima disso substituiria a explicação real da
   * regra por uma genérica, que ajuda menos.
   */
  protected readonly errorMessage = computed(() => this.error()?.message ?? null);

  protected onVisibleChange(value: boolean): void {
    if (!value && this.submitting()) {
      return;
    }
    this.visibleChange.emit(value);
  }
}
