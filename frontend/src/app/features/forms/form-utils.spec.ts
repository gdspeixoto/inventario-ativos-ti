import { FormControl, FormGroup } from '@angular/forms';
import { dateAfter, differentFrom, fromDateOnly, toDateOnly, trimmedOrNull } from './form-utils';

describe('toDateOnly', () => {
  it('formata no padrão que a API aceita', () => {
    expect(toDateOnly(new Date(2026, 2, 15))).toBe('2026-03-15');
  });

  it('preenche mês e dia com zero à esquerda', () => {
    expect(toDateOnly(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('usa a data local, e não UTC', () => {
    // Uma data escolhida à noite em fuso negativo vira o dia seguinte quando
    // convertida por toISOString(); o usuário veria salvo um dia diferente do
    // que selecionou.
    const lateNight = new Date(2026, 2, 15, 23, 30);

    expect(toDateOnly(lateNight)).toBe('2026-03-15');
  });

  it('trata início do dia sem recuar a data', () => {
    expect(toDateOnly(new Date(2026, 2, 15, 0, 0))).toBe('2026-03-15');
  });

  it('devolve null para ausência de data', () => {
    expect(toDateOnly(null)).toBeNull();
    expect(toDateOnly(undefined)).toBeNull();
  });
});

describe('fromDateOnly', () => {
  it('reconstrói a data sem deslocamento de fuso', () => {
    const date = fromDateOnly('2026-03-15');

    expect(date?.getFullYear()).toBe(2026);
    expect(date?.getMonth()).toBe(2);
    expect(date?.getDate()).toBe(15);
  });

  it('é o inverso de toDateOnly', () => {
    expect(toDateOnly(fromDateOnly('2026-12-31'))).toBe('2026-12-31');
  });

  it('devolve null para entrada vazia ou inválida', () => {
    expect(fromDateOnly(null)).toBeNull();
    expect(fromDateOnly('')).toBeNull();
    expect(fromDateOnly('texto')).toBeNull();
  });
});

describe('trimmedOrNull', () => {
  it('converte string em branco para null', () => {
    // O backend trata string vazia como ausência; enviar '' gravaria um valor
    // que a tela exibiria como um campo preenchido com nada.
    expect(trimmedOrNull('   ')).toBeNull();
    expect(trimmedOrNull('')).toBeNull();
    expect(trimmedOrNull(null)).toBeNull();
  });

  it('remove espaços das bordas', () => {
    expect(trimmedOrNull('  Microsoft  ')).toBe('Microsoft');
  });
});

describe('differentFrom', () => {
  it('recusa valor igual ao atual', () => {
    const control = new FormControl(100, differentFrom(() => 100));

    expect(control.errors).toEqual({ sameValue: true });
  });

  it('aceita valor diferente', () => {
    const control = new FormControl(120, differentFrom(() => 100));

    expect(control.errors).toBeNull();
  });

  it('não valida enquanto não houver referência', () => {
    const control = new FormControl(100, differentFrom(() => null));

    expect(control.errors).toBeNull();
  });

  it('aceita zero como valor válido e diferente', () => {
    const control = new FormControl(0, differentFrom(() => 100));

    expect(control.errors).toBeNull();
  });
});

describe('dateAfter', () => {
  function build(start: Date | null, end: Date | null): FormGroup {
    return new FormGroup(
      { start: new FormControl(start), end: new FormControl(end) },
      { validators: dateAfter('start', 'end') },
    );
  }

  it('recusa data final anterior à inicial', () => {
    const group = build(new Date(2026, 5, 1), new Date(2026, 4, 1));

    expect(group.errors).toEqual({ dateOrder: true });
  });

  it('recusa datas iguais, pois o domínio exige posterior', () => {
    const group = build(new Date(2026, 5, 1), new Date(2026, 5, 1));

    expect(group.errors).toEqual({ dateOrder: true });
  });

  it('aceita data final posterior', () => {
    const group = build(new Date(2026, 5, 1), new Date(2027, 5, 1));

    expect(group.errors).toBeNull();
  });

  it('marca o erro no campo final, que é onde o usuário corrige', () => {
    const group = build(new Date(2026, 5, 1), new Date(2026, 4, 1));

    expect(group.get('end')?.errors).toEqual({ dateOrder: true });
    expect(group.get('start')?.errors).toBeNull();
  });

  it('não valida enquanto uma das datas estiver vazia', () => {
    expect(build(new Date(2026, 5, 1), null).errors).toBeNull();
    expect(build(null, new Date(2026, 5, 1)).errors).toBeNull();
  });
});
