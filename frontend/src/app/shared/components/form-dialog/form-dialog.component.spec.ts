import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { providePrimeNG } from 'primeng/config';
import { beforeEach, describe, expect, it } from 'vitest';

import { FormDialogComponent } from './form-dialog.component';
import { I18N_CONFIG } from '@core/i18n/i18n.tokens';
import { CorporatePreset } from '@core/theme/primeng-preset';
import { i18nConfig } from '@config/i18n.config';

/**
 * Hospedeiro que reproduz o uso real das páginas: elas controlam `visible` e
 * fecham o diálogo ao receber os eventos.
 *
 * O teste existe por causa de um comportamento com consequência concreta: se o
 * modal não fechar após confirmar, o usuário clica de novo e cria o registro
 * duas vezes.
 */
@Component({
  imports: [FormDialogComponent],
  template: `
    <app-form-dialog
      [visible]="open()"
      title="forms.newLicense"
      [submitting]="submitting()"
      (confirmed)="onConfirm()"
      (cancelled)="open.set(false)"
      (visibleChange)="open.set($event)"
    >
      <p>conteúdo</p>
    </app-form-dialog>
  `,
})
class HostComponent {
  readonly open = signal(true);
  readonly submitting = signal(false);
  confirmCount = 0;

  onConfirm(): void {
    this.confirmCount++;
    this.open.set(false);
  }
}

describe('FormDialogComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        providePrimeNG({ theme: { preset: CorporatePreset } }),
        { provide: I18N_CONFIG, useValue: i18nConfig },
      ],
    });

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  /** O diálogo do PrimeNG renderiza fora do host, no document. */
  function buttonByText(text: string): HTMLButtonElement | undefined {
    return [...document.querySelectorAll<HTMLButtonElement>('button')].find((element) =>
      element.textContent?.includes(text),
    );
  }

  it('emite confirmação e permite que a página feche o modal', async () => {
    buttonByText('common.save')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.confirmCount).toBe(1);
    expect(host.open()).toBe(false);
  });

  it('fecha ao cancelar', async () => {
    buttonByText('common.cancel')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.open()).toBe(false);
  });

  it('desabilita o cancelar durante o envio', async () => {
    host.submitting.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(buttonByText('common.cancel')?.disabled).toBe(true);
  });
});
