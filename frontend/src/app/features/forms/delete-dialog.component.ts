import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import type { AppHttpError } from '@core/errors/http-error.model';
import { CommandRunner } from './command-runner';

/**
 * Confirmação de exclusão definitiva.
 *
 * Exclusão e desativação resolvem problemas diferentes, e esta caixa existe
 * para que não sejam confundidas. Desativar encerra algo que existiu e mantém
 * o histórico; excluir apaga um cadastro que não deveria ter sido criado.
 *
 * Duas decisões merecem explicação:
 *
 * O nome do item precisa ser digitado. É atrito deliberado: sem ele, a
 * exclusão fica a um clique de distância do "desativar" logo ao lado, e a
 * operação irreversível seria justamente a mais fácil de disparar por engano.
 * Digitar obriga a olhar para o que se vai apagar.
 *
 * Quando o servidor recusa (409), a caixa não se limita a mostrar o motivo:
 * ela oferece a desativação ali mesmo. O 409 diz que existe histórico, e
 * histórico é exatamente o caso em que desativar é a resposta correta — fazer
 * o usuário fechar, procurar outro botão e recomeçar seria puni-lo por uma
 * escolha que o sistema já sabe qual é.
 */
@Component({
  selector: 'app-delete-dialog',
  imports: [Dialog, Button, InputText, Message, FormsModule, TranslatePipe],
  providers: [CommandRunner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [closable]="!runner.submitting()"
      [closeOnEscape]="!runner.submitting()"
      [style]="{ width: '34rem' }"
      [breakpoints]="{ '960px': '90vw' }"
      [header]="'actions.deleteTitle' | translate"
      [dismissableMask]="!runner.submitting()"
    >
      @if (blockers(); as message) {
        <!--
          Caminho da recusa: o servidor explicou o que impede. A alternativa
          correta aparece como ação, não como texto.
        -->
        <p-message severity="warn" styleClass="delete-dialog__blocked">{{ message }}</p-message>

        @if (canDeactivate()) {
          <p class="delete-dialog__hint">{{ 'actions.deleteBlockedHint' | translate }}</p>
        }
      } @else {
        <p class="delete-dialog__warning">
          {{ 'actions.deleteWarning' | translate }}
        </p>

        <p class="delete-dialog__target">{{ itemName() }}</p>

        <label class="delete-dialog__label" [attr.for]="inputId">
          {{ 'actions.deleteConfirmLabel' | translate }}
        </label>

        <input
          pInputText
          [id]="inputId"
          class="delete-dialog__input"
          [ngModel]="typed()"
          (ngModelChange)="typed.set($event)"
          [disabled]="runner.submitting()"
          autocomplete="off"
        />

        @if (runner.error(); as error) {
          <p-message severity="error" styleClass="delete-dialog__error">
            {{ error.message }}
          </p-message>
        }
      }

      <ng-template #footer>
        <p-button
          [label]="'common.cancel' | translate"
          severity="secondary"
          [text]="true"
          [disabled]="runner.submitting()"
          (onClick)="close()"
        />

        @if (blockers()) {
          @if (canDeactivate()) {
            <p-button
              [label]="'actions.deactivateInstead' | translate"
              severity="warn"
              icon="pi pi-ban"
              (onClick)="deactivateInstead.emit()"
            />
          }
        } @else {
          <p-button
            [label]="'actions.deleteConfirm' | translate"
            severity="danger"
            icon="pi pi-trash"
            [loading]="runner.submitting()"
            [disabled]="!nameMatches() || runner.submitting()"
            (onClick)="confirm()"
          />
        }
      </ng-template>
    </p-dialog>
  `,
  styles: `
    .delete-dialog__warning {
      margin: 0 0 0.75rem;
      color: var(--text-color-secondary);
    }

    .delete-dialog__target {
      margin: 0 0 1.25rem;
      padding: 0.6rem 0.85rem;
      border-radius: 6px;
      background: var(--surface-100);
      font-weight: 600;
      word-break: break-word;
    }

    .delete-dialog__label {
      display: block;
      margin-bottom: 0.4rem;
      font-size: 0.875rem;
    }

    .delete-dialog__input {
      width: 100%;
    }

    .delete-dialog__hint {
      margin: 0.75rem 0 0;
      color: var(--text-color-secondary);
    }

    :host ::ng-deep .delete-dialog__error,
    :host ::ng-deep .delete-dialog__blocked {
      display: block;
      margin-bottom: 0.75rem;
    }
  `,
})
export class DeleteDialogComponent {
  readonly runner = inject(CommandRunner);

  readonly visible = input.required<boolean>();

  /** Nome exibido e exigido na confirmação. */
  readonly itemName = input.required<string>();

  /** A operação de exclusão em si. */
  readonly action = input.required<() => Promise<void>>();

  /** Se a entidade admite desativação como alternativa ao ser recusada. */
  readonly canDeactivate = input(true);

  readonly deleted = output<void>();
  readonly cancelled = output<void>();
  readonly deactivateInstead = output<void>();

  protected readonly inputId = `delete-confirm-${Math.random().toString(36).slice(2, 8)}`;

  /**
   * Signal, e não propriedade comum: `nameMatches` é um `computed`, que só
   * reage a fontes rastreáveis. Com uma propriedade simples o botão de excluir
   * jamais destravaria, por mais que o nome fosse digitado corretamente.
   */
  protected readonly typed = signal('');

  private readonly _blockers = signal<string | null>(null);
  protected readonly blockers = this._blockers.asReadonly();

  /**
   * Comparação tolerante ao acidental — espaços nas pontas e diferença de
   * caixa não são o engano contra o qual esta confirmação protege.
   */
  protected readonly nameMatches = computed(
    () => this.typed().trim().toLocaleLowerCase() === this.itemName().trim().toLocaleLowerCase(),
  );

  protected async confirm(): Promise<void> {
    if (!this.nameMatches()) {
      return;
    }

    const outcome = await this.runner.run(() => this.action()(), {
      successMessage: 'feedback.deleted',
    });

    if (outcome.ok) {
      this.reset();
      this.deleted.emit();

      return;
    }

    const error = this.runner.error();

    // 409 não é falha de operação: é o servidor dizendo que existe histórico
    // e que o caminho é outro. Trocamos a mensagem de erro pela orientação.
    if (error?.status === 409) {
      this._blockers.set(this.messageOf(error));
    }
  }

  protected close(): void {
    this.reset();
    this.cancelled.emit();
  }

  protected onVisibleChange(value: boolean): void {
    if (!value) {
      this.close();
    }
  }

  private reset(): void {
    this.typed.set('');
    this._blockers.set(null);
    this.runner.reset();
  }

  private messageOf(error: AppHttpError): string {
    return error.message || 'Nao foi possivel excluir este registro.';
  }
}
