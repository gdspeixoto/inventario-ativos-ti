import { Component, signal } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { providePrimeNG } from 'primeng/config';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DeleteDialogComponent } from './delete-dialog.component';
import { ToastService } from '@core/services/toast.service';
import { I18N_CONFIG } from '@core/i18n/i18n.tokens';
import { CorporatePreset } from '@core/theme/primeng-preset';
import { i18nConfig } from '@config/i18n.config';
import type { AppHttpError } from '@core/errors/http-error.model';

/**
 * Host que reproduz o uso real: quem abre o diálogo controla a visibilidade e
 * fornece a ação. Testar o componente isolado não exercitaria a fiação que de
 * fato quebra.
 */
@Component({
  imports: [DeleteDialogComponent],
  template: `
    <app-delete-dialog
      [visible]="visible()"
      [itemName]="itemName()"
      [action]="action"
      (deleted)="deleted = deleted + 1"
      (cancelled)="cancelled = cancelled + 1"
      (deactivateInstead)="deactivate = deactivate + 1"
    />
  `,
})
class HostComponent {
  readonly visible = signal(true);
  readonly itemName = signal('Microsoft 365 E3');

  deleted = 0;
  cancelled = 0;
  deactivate = 0;

  action: () => Promise<void> = () => Promise.resolve();
}

function conflictError(message: string): AppHttpError {
  return Object.freeze({
    status: 409,
    code: null,
    message,
    fieldErrors: Object.freeze({}),
    traceId: null,
    url: null,
    translationKey: 'errors.http.conflict',
  }) as AppHttpError;
}

describe('DeleteDialogComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  const confirmButton = (): HTMLButtonElement | null => {
    const buttons = Array.from(
      document.querySelectorAll<HTMLButtonElement>('.p-dialog button'),
    );

    return buttons.find((button) => button.textContent?.includes('actions.deleteConfirm')) ?? null;
  };

  const typeName = (value: string): void => {
    const input = document.querySelector<HTMLInputElement>('.delete-dialog__input');

    if (!input) {
      throw new Error('campo de confirmação não encontrado');
    }

    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        providePrimeNG({ theme: { preset: CorporatePreset } }),
        { provide: I18N_CONFIG, useValue: i18nConfig },
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    });

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('mantém a exclusão bloqueada enquanto o nome não for digitado', () => {
    // A proteção contra o clique acidental: excluir fica ao lado de operações
    // reversíveis, e é a única que não tem volta.
    expect(confirmButton()?.disabled).toBe(true);
  });

  it('libera a exclusão quando o nome confere', () => {
    typeName('Microsoft 365 E3');

    expect(confirmButton()?.disabled).toBe(false);
  });

  it('continua bloqueada quando o nome digitado é de outro item', () => {
    typeName('Microsoft 365 E5');

    expect(confirmButton()?.disabled).toBe(true);
  });

  it('aceita diferença de caixa e espaços nas pontas', () => {
    // Exigir a grafia exata puniria quem digitou certo; o engano contra o qual
    // isto protege é apagar o item errado, não errar o shift.
    typeName('  microsoft 365 e3  ');

    expect(confirmButton()?.disabled).toBe(false);
  });

  it('executa a ação e avisa a exclusão', async () => {
    const action = vi.fn(() => Promise.resolve());
    host.action = action;
    fixture.detectChanges();

    typeName('Microsoft 365 E3');
    confirmButton()?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(action).toHaveBeenCalledOnce();
    expect(host.deleted).toBe(1);
  });

  it('não executa a ação sem a confirmação digitada', async () => {
    const action = vi.fn(() => Promise.resolve());
    host.action = action;
    fixture.detectChanges();

    confirmButton()?.click();
    await fixture.whenStable();

    expect(action).not.toHaveBeenCalled();
    expect(host.deleted).toBe(0);
  });

  it('mostra o que impede a exclusão quando o servidor recusa', async () => {
    host.action = () =>
      Promise.reject(
        conflictError('Esta unidade possui 3 unidade(s) subordinada(s), 2 ativo(s) vinculado(s).'),
      );
    fixture.detectChanges();

    typeName('Microsoft 365 E3');
    confirmButton()?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    const texto = document.querySelector('.p-dialog')?.textContent ?? '';

    expect(texto).toContain('3 unidade(s) subordinada(s)');
    expect(host.deleted).toBe(0);
  });

  it('oferece a desativação como saída após a recusa', async () => {
    // O 409 significa que há histórico — exatamente o caso em que desativar é
    // a resposta certa. Obrigar o usuário a fechar e procurar outro botão
    // seria puni-lo por uma escolha que o sistema já conhece.
    host.action = () => Promise.reject(conflictError('Possui 2 ativo(s) vinculado(s).'));
    fixture.detectChanges();

    typeName('Microsoft 365 E3');
    confirmButton()?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    const buttons = Array.from(document.querySelectorAll<HTMLButtonElement>('.p-dialog button'));
    const deactivate = buttons.find((button) =>
      button.textContent?.includes('actions.deactivateInstead'),
    );

    expect(deactivate).toBeTruthy();

    deactivate?.click();
    fixture.detectChanges();

    expect(host.deactivate).toBe(1);
  });

  it('esconde o botão de excluir depois da recusa', async () => {
    host.action = () => Promise.reject(conflictError('Possui histórico.'));
    fixture.detectChanges();

    typeName('Microsoft 365 E3');
    confirmButton()?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    // Insistir no botão que acabou de falhar não levaria a lugar nenhum.
    expect(confirmButton()).toBeNull();
  });

  it('limpa a confirmação ao cancelar', async () => {
    typeName('Microsoft 365 E3');
    expect(confirmButton()?.disabled).toBe(false);

    const buttons = Array.from(document.querySelectorAll<HTMLButtonElement>('.p-dialog button'));
    buttons.find((button) => button.textContent?.includes('common.cancel'))?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(host.cancelled).toBe(1);

    // Reabrir com a confirmação ainda válida deixaria a exclusão a um clique
    // de distância — exatamente o que a digitação existe para impedir.
    expect(confirmButton()?.disabled).toBe(true);
  });
});
