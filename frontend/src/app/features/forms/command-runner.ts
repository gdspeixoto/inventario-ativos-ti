import { Injectable, inject, signal } from '@angular/core';
import { ToastService } from '@core/services/toast.service';
import { isAppHttpError, type AppHttpError } from '@core/errors/http-error.model';

/**
 * Desfecho de um comando.
 *
 * `ok` separa "concluiu" de "devolveu valor": um 204 conclui sem corpo, e
 * confundir os dois manteria o modal aberto após o sucesso.
 */
export interface CommandOutcome<T> {
  readonly ok: boolean;
  readonly value: T | null;
}

/**
 * Executa um comando de escrita e cuida do que cerca a chamada.
 *
 * Sem isto, cada modal repetiria — e cedo ou tarde divergiria em — quatro
 * coisas: marcar o envio em andamento, exibir o erro do servidor no próprio
 * formulário, avisar o sucesso e devolver o controle a quem precisa recarregar
 * a tela.
 *
 * O erro **não** vira toast. Ele fica dentro do modal, ao lado dos campos que
 * o usuário ainda tem aberto: um toast desapareceria justamente enquanto a
 * pessoa tenta corrigir o que errou. Só o sucesso é notificado, porque aí o
 * modal fecha e não há mais onde mostrar.
 */
@Injectable()
export class CommandRunner {
  private readonly toast = inject(ToastService);

  private readonly _submitting = signal(false);
  private readonly _error = signal<AppHttpError | null>(null);

  readonly submitting = this._submitting.asReadonly();
  readonly error = this._error.asReadonly();

  /**
   * Roda o comando e informa se ele concluiu.
   *
   * O sucesso é sinalizado por `ok`, e não pela presença de um valor: boa parte
   * dos endpoints responde 204 sem corpo, e tratar `null` como falha faria o
   * modal permanecer aberto depois de uma operação bem-sucedida — o usuário
   * clicaria de novo e criaria o registro em duplicidade.
   */
  async run<T>(
    command: () => Promise<T>,
    options: { successMessage?: string } = {},
  ): Promise<CommandOutcome<T>> {
    if (this._submitting()) {
      // Um duplo clique não deve virar dois cadastros.
      return { ok: false, value: null };
    }

    this._submitting.set(true);
    this._error.set(null);

    try {
      const value = await command();

      if (options.successMessage) {
        this.toast.success(options.successMessage);
      }

      return { ok: true, value };
    } catch (error) {
      this._error.set(this.normalize(error));
      return { ok: false, value: null };
    } finally {
      this._submitting.set(false);
    }
  }

  /** Limpa o erro ao reabrir o modal, para não mostrar a falha anterior. */
  reset(): void {
    this._error.set(null);
  }

  private normalize(error: unknown): AppHttpError {
    if (isAppHttpError(error)) {
      return error;
    }

    return Object.freeze({
      status: 0,
      code: null,
      message: error instanceof Error ? error.message : 'Falha ao executar a operação.',
      fieldErrors: Object.freeze({}),
      traceId: null,
      url: null,
      translationKey: 'errors.http.unknown',
    });
  }
}
