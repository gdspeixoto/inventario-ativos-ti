import { TestBed } from '@angular/core/testing';
import { CommandRunner } from './command-runner';
import { ToastService } from '@core/services/toast.service';

describe('CommandRunner', () => {
  let runner: CommandRunner;
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    toast = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      providers: [CommandRunner, { provide: ToastService, useValue: toast }],
    });

    runner = TestBed.inject(CommandRunner);
  });

  it('devolve o resultado e avisa o sucesso', async () => {
    const outcome = await runner.run(() => Promise.resolve({ id: 'abc' }), {
      successMessage: 'feedback.licenseCreated',
    });

    expect(outcome.ok).toBe(true);
    expect(outcome.value).toEqual({ id: 'abc' });
    expect(toast.success).toHaveBeenCalledWith('feedback.licenseCreated');
  });

  it('marca sucesso mesmo quando a resposta não tem corpo', async () => {
    // Nove operações do backend respondem 204. Se ausência de corpo fosse lida
    // como falha, o modal ficaria aberto após concluir e o usuário repetiria a
    // operação.
    const outcome = await runner.run(() => Promise.resolve(undefined));

    expect(outcome.ok).toBe(true);
  });

  it('distingue sucesso sem corpo de falha', async () => {
    const success = await runner.run(() => Promise.resolve(null));
    const failure = await runner.run(() => Promise.reject(new Error('erro')));

    expect(success.ok).toBe(true);
    expect(failure.ok).toBe(false);
  });

  it('marca o envio em andamento e o encerra ao final', async () => {
    expect(runner.submitting()).toBe(false);

    const pending = runner.run(() => Promise.resolve(null));
    expect(runner.submitting()).toBe(true);

    await pending;
    expect(runner.submitting()).toBe(false);
  });

  it('devolve null e guarda o erro quando a operação falha', async () => {
    const failure = {
      status: 422,
      code: null,
      message: 'A quantidade contratada deve ser maior que zero.',
      fieldErrors: {},
      traceId: null,
      url: null,
      translationKey: 'errors.http.422',
    };

    const outcome = await runner.run(() => Promise.reject(failure));

    expect(outcome.ok).toBe(false);
    expect(runner.error()?.message).toBe('A quantidade contratada deve ser maior que zero.');
  });

  it('não transforma a falha em toast', async () => {
    // O erro fica no modal, junto aos campos que o usuário vai corrigir; um
    // toast sumiria justamente durante a correção.
    await runner.run(() => Promise.reject(new Error('falhou')));

    expect(toast.error).not.toHaveBeenCalled();
  });

  it('libera o estado mesmo quando a operação falha', async () => {
    await runner.run(() => Promise.reject(new Error('falhou')));

    expect(runner.submitting()).toBe(false);
  });

  it('ignora a segunda chamada enquanto a primeira está em voo', async () => {
    // Um duplo clique no botão salvar não deve virar dois cadastros.
    let calls = 0;

    const slow = () =>
      new Promise<string>((resolve) => {
        calls++;
        setTimeout(() => resolve('ok'), 10);
      });

    const first = runner.run(slow);
    const second = await runner.run(slow);

    expect(second.ok).toBe(false);
    expect(calls).toBe(1);

    await first;
  });

  it('limpa o erro anterior ao iniciar nova tentativa', async () => {
    await runner.run(() => Promise.reject(new Error('primeira falha')));
    expect(runner.error()).not.toBeNull();

    await runner.run(() => Promise.resolve('ok'));
    expect(runner.error()).toBeNull();
  });

  it('reset limpa o erro para o modal reabrir sem a falha anterior', async () => {
    await runner.run(() => Promise.reject(new Error('falhou')));

    runner.reset();

    expect(runner.error()).toBeNull();
  });

  it('normaliza erro que não é HTTP', async () => {
    await runner.run(() => Promise.reject(new Error('rede indisponível')));

    expect(runner.error()?.status).toBe(0);
    expect(runner.error()?.message).toBe('rede indisponível');
  });

  it('não avisa sucesso quando nenhuma mensagem é informada', async () => {
    await runner.run(() => Promise.resolve('ok'));

    expect(toast.success).not.toHaveBeenCalled();
  });
});
