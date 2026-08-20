import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Aviso de que a tela está exibindo dados fictícios.
 *
 * Existe para que ninguém confunda demonstração com operação real: números
 * plausíveis sem identificação são a forma mais rápida de alguém tomar uma
 * decisão financeira sobre dados que não existem.
 */
@Component({
  selector: 'app-demo-notice',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="demo-notice" role="status">
      <i class="pi pi-info-circle" aria-hidden="true"></i>
      Dados de demonstração. Conecte a API para ver as informações reais.
    </p>
  `,
  styles: `
    .demo-notice {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin: 0 0 1rem;
      padding: 0.65rem 0.9rem;
      border: 1px dashed var(--app-color-warning, #d97706);
      border-radius: var(--app-radius-md);
      background-color: var(--app-color-warning-subtle, #fffbeb);
      color: var(--app-color-warning, #b45309);
      font-size: var(--app-font-size-sm);
    }
  `,
})
export class DemoNoticeComponent {}
