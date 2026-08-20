import { TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { MoneyComponent } from './money.component';

/**
 * Formatação monetária em pt-BR.
 *
 * Este teste existe porque a ausência do registro do locale não falha no build:
 * ela só aparece em runtime, como NG0701, e derruba a renderização da tela
 * inteira — não apenas do valor mal formatado.
 */
describe('MoneyComponent', () => {
  beforeEach(() => {
    registerLocaleData(localePt, 'pt-BR');
    TestBed.configureTestingModule({
      providers: [{ provide: LOCALE_ID, useValue: 'pt-BR' }],
    });
  });

  it('formata no padrão brasileiro, com milhar em ponto e decimal em vírgula', () => {
    const fixture = TestBed.createComponent(MoneyComponent);
    fixture.componentRef.setInput('amount', 12600);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('12.600,00');
    expect(text).toContain('R$');
  });

  it('formata centavos sem arredondar para inteiro', () => {
    const fixture = TestBed.createComponent(MoneyComponent);
    fixture.componentRef.setInput('amount', 2695.5);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('2.695,50');
  });
});
