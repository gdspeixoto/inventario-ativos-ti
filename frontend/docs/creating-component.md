# Criando um componente

---

## 1. Onde colocar

| Usado por              | Local                               |
| ---------------------- | ----------------------------------- |
| Uma única página       | `pages/<pagina>/components/<nome>/` |
| Duas ou mais páginas   | `shared/components/<nome>/`         |
| A moldura da aplicação | `core/layout/<nome>/`               |

Se um componente de feature começar a ser copiado para outra, **promova-o a `shared/`** — não importe entre features.

---

## 2. Esqueleto

```ts
// shared/components/status-badge/status-badge.component.ts
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@core/i18n/translate.pipe';

export type StatusKind = 'active' | 'inactive' | 'pending';

/**
 * Selo de status com cor semântica.
 *
 * Usa os tokens de estado (`--app-color-success` e afins), de modo que o
 * contraste continua correto nos dois temas sem qualquer condicional.
 */
@Component({
  selector: 'app-status-badge',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="status-badge" [class]="'status-badge--' + status()">
      <span class="status-badge__dot" aria-hidden="true"></span>
      {{ 'status.' + status() | translate }}
    </span>
  `,
  styles: `
    .status-badge {
      display: inline-flex;
      align-items: center;
      gap: var(--app-spacing-2xs);
      padding: 0.125rem var(--app-spacing-xs);
      border-radius: var(--app-radius-pill);
      font-size: var(--app-font-size-xs);
      font-weight: var(--app-font-weight-medium);
    }

    .status-badge__dot {
      width: 0.375rem;
      height: 0.375rem;
      border-radius: var(--app-radius-pill);
      background-color: currentcolor;
    }

    .status-badge--active {
      background-color: var(--app-color-success-subtle);
      color: var(--app-color-success);
    }

    .status-badge--inactive {
      background-color: var(--app-color-surface-sunken);
      color: var(--app-color-text-muted);
    }

    .status-badge--pending {
      background-color: var(--app-color-warning-subtle);
      color: var(--app-color-warning);
    }
  `,
})
export class StatusBadgeComponent {
  readonly status = input.required<StatusKind>();
}
```

> **Cuidado com crases em `template:` e `styles:`.** Um comentário contendo <code>`md`</code> encerra o template literal e derruba o build. Use aspas simples em comentários dentro desses blocos.

---

## 3. Inputs e outputs

```ts
readonly user = input.required<User>();           // obrigatório
readonly compact = input(false);                  // com padrão
readonly maxItems = input(10, { alias: 'limit' }); // nome externo diferente

readonly selected = output<User>();
readonly closed = output<void>();

readonly expanded = model(false);                 // two-way: [(expanded)]

// Transform: aceita string vinda do template
readonly disabled = input(false, {
  transform: (value: string | boolean) => value !== false && value !== 'false',
});
```

---

## 4. Projeção de conteúdo

```html
<div class="painel">
  <header>
    <h2>{{ title() }}</h2>
    <ng-content select="[panel-actions]" />
  </header>

  <div class="painel__corpo">
    <ng-content />
  </div>
</div>
```

Uso:

```html
<app-painel title="Relatórios">
  <p-button panel-actions icon="pi pi-download" />
  <p>Conteúdo padrão.</p>
</app-painel>
```

---

## 5. Regras de estilo

- **Nenhum hexadecimal.** Sempre `var(--app-*)` ou os auxiliares de `tokens`.
- `:host { display: block; }` — o padrão `inline` de um elemento customizado quase nunca é o desejado.
- Classes em BEM prefixadas pelo componente: `.status-badge__dot`.
- Para arquivos separados, importe os auxiliares:

```scss
@use 'tokens' as tokens;

:host {
  display: block;
}

.card {
  padding: tokens.spacing(lg);
  border-radius: tokens.radius(lg);
  background: tokens.color(card);
}
```

- Estilizar internos do PrimeNG: `:host ::ng-deep .p-datatable-thead { … }` — sempre escopado.

---

## 6. Acessibilidade

| Situação                     | O que fazer                                |
| ---------------------------- | ------------------------------------------ |
| Elemento clicável            | `<button>` ou `<a>`, nunca `<div (click)>` |
| Botão só com ícone           | `[ariaLabel]` ou `aria-label`              |
| Ícone decorativo             | `aria-hidden="true"`                       |
| Imagem decorativa            | `alt=""`                                   |
| Conteúdo que muda sozinho    | `role="status"` + `aria-live="polite"`     |
| Erro                         | `role="alert"`                             |
| Elemento expansível          | `aria-expanded`                            |
| Abre menu/diálogo            | `aria-haspopup`                            |
| Item de navegação atual      | `aria-current="page"`                      |
| Texto só para leitor de tela | `class="sr-only"`                          |

---

## 7. Teste

```ts
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { StatusBadgeComponent } from './status-badge.component';

describe('StatusBadgeComponent', () => {
  it('aplica a classe do status recebido', () => {
    TestBed.configureTestingModule({ imports: [StatusBadgeComponent] });
    const fixture = TestBed.createComponent(StatusBadgeComponent);
    fixture.componentRef.setInput('status', 'active');
    fixture.detectChanges();

    const badge = (fixture.nativeElement as HTMLElement).querySelector('.status-badge');
    expect(badge?.classList).toContain('status-badge--active');
  });
});
```

---

## 8. Componentes já disponíveis

Antes de criar, verifique se já existe:

| Componente                  | Seletor                   | Para quê                          |
| --------------------------- | ------------------------- | --------------------------------- |
| `PageHeaderComponent`       | `<app-page-header>`       | Cabeçalho padrão de página        |
| `AppCardComponent`          | `<app-card>`              | Superfície com cabeçalho e rodapé |
| `LoadingComponent`          | `<app-loading>`           | Spinner com anúncio acessível     |
| `EmptyStateComponent`       | `<app-empty-state>`       | Lista vazia com ação              |
| `ErrorStateComponent`       | `<app-error-state>`       | Falha com retry e trace id        |
| `AvatarComponent`           | `<app-avatar>`            | Avatar com fallback de iniciais   |
| `BreadcrumbComponent`       | `<app-breadcrumb>`        | Trilha de navegação               |
| `ThemeSwitcherComponent`    | `<app-theme-switcher>`    | Alternância de tema               |
| `LanguageSwitcherComponent` | `<app-language-switcher>` | Troca de idioma                   |
| `UserMenuComponent`         | `<app-user-menu>`         | Menu da conta                     |
| `BrandLogoComponent`        | `<app-brand-logo>`        | Marca do produto                  |

E as diretivas `*appHasRole`, `*appHasPermission`, `appAutofocus`; os pipes `translate`, `initials`, `timeAgo`.

---

## Checklist

- [ ] Standalone e OnPush
- [ ] `input()` / `output()` em vez de decorators
- [ ] `:host { display: … }` definido
- [ ] Nenhum hexadecimal; só tokens
- [ ] Nenhuma string fixa; tudo via i18n
- [ ] Acessível por teclado e com rótulos
- [ ] Testado nos dois temas
- [ ] Responsivo
- [ ] TSDoc explicando o propósito e as decisões não óbvias
- [ ] Spec cobrindo o comportamento principal
