import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MoneyComponent } from '@shared/components/money/money.component';
import type { OrganizationalUnitTree } from '@app/features/models';
import { UNIT_TYPE_LABELS } from '@app/features/models/enums';

/**
 * Nó da árvore organizacional, recursivo.
 *
 * A versão anterior desta tela desenhava dois níveis fixos — raiz e filhos
 * diretos. A hierarquia da instituição tem cinco (Matriz, Negócio, Área
 * Tecnológica, Unidade Operacional, Núcleo de Suporte), então tudo abaixo do
 * segundo simplesmente não aparecia: uma unidade operacional com trinta ativos
 * ficava invisível, e o custo dela não batia com nenhuma soma na tela.
 *
 * Sendo recursivo, o componente desenha a profundidade que existir.
 *
 * Os nós começam fechados a partir do terceiro nível. Abrir tudo de uma vez
 * numa árvore grande entrega uma parede de texto onde nada se distingue; o
 * usuário abre o ramo que lhe interessa.
 */
@Component({
  selector: 'app-unit-tree-node',
  imports: [RouterLink, MoneyComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="node" [class.node--inactive]="!node().isActive">
      <div class="node__row" [style.padding-left.rem]="indent()">
        @if (hasChildren()) {
          <button
            type="button"
            class="node__toggle"
            [attr.aria-expanded]="expanded()"
            [attr.aria-label]="expanded() ? 'Recolher ' + node().name : 'Expandir ' + node().name"
            (click)="expanded.set(!expanded())"
          >
            <i class="pi" [class.pi-chevron-down]="expanded()" [class.pi-chevron-right]="!expanded()"></i>
          </button>
        } @else {
          <span class="node__toggle node__toggle--empty" aria-hidden="true"></span>
        }

        <a class="node__name" [routerLink]="['/organization/units', node().id]">
          {{ node().name }}
        </a>

        <span class="node__type">{{ typeLabel() }}</span>

        @if (!node().isActive) {
          <span class="node__badge">inativa</span>
        }

        <span class="node__spacer"></span>

        @if (node().assetCount > 0) {
          <span class="node__assets">{{ node().assetCount }} ativos</span>
        }

        <span class="node__cost">
          <app-money [amount]="node().monthlyCost" />
        </span>
      </div>

      @if (expanded()) {
        @for (child of node().children; track child.id) {
          <app-unit-tree-node [node]="child" [depth]="depth() + 1" />
        }
      }
    </div>
  `,
  styles: `
    .node__row {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.4rem 0.75rem 0.4rem 0;
      border-radius: 6px;
      min-height: 2.25rem;
    }

    .node__row:hover {
      background: var(--surface-100);
    }

    .node__toggle {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 1.5rem;
      height: 1.5rem;
      flex: 0 0 1.5rem;
      border: 0;
      border-radius: 4px;
      background: transparent;
      color: var(--text-color-secondary);
      cursor: pointer;
      font-size: 0.75rem;
    }

    .node__toggle:hover {
      background: var(--surface-200);
    }

    .node__toggle--empty {
      cursor: default;
    }

    .node__name {
      color: inherit;
      font-weight: 600;
      text-decoration: none;
    }

    .node__name:hover {
      text-decoration: underline;
    }

    .node__type {
      color: var(--text-color-secondary);
      font-size: 0.75rem;
      white-space: nowrap;
    }

    .node__badge {
      padding: 0.05rem 0.4rem;
      border-radius: 999px;
      background: var(--surface-200);
      color: var(--text-color-secondary);
      font-size: 0.6875rem;
    }

    .node__spacer {
      flex: 1 1 auto;
    }

    .node__assets {
      color: var(--text-color-secondary);
      font-size: 0.75rem;
      white-space: nowrap;
    }

    .node__cost {
      min-width: 7rem;
      text-align: right;
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
    }

    /* Unidade inativa continua visível, mas recuada da atenção. */
    .node--inactive > .node__row .node__name {
      color: var(--text-color-secondary);
      font-weight: 500;
    }

    @media (max-width: 720px) {
      .node__type,
      .node__assets {
        display: none;
      }
    }
  `,
})
export class UnitTreeNodeComponent {
  readonly node = input.required<OrganizationalUnitTree>();
  readonly depth = input(0);

  readonly selected = output<string>();

  /**
   * Fechado a partir do terceiro nível: uma árvore inteiramente aberta é uma
   * parede de texto onde nada se distingue.
   *
   * `linkedSignal` porque o estado tem valor inicial derivado da profundidade,
   * mas passa a pertencer ao usuário assim que ele clica — e não deve ser
   * desfeito a cada nova renderização.
   */
  protected readonly expanded = linkedSignal(() => this.depth() < 2);

  protected readonly hasChildren = computed(() => this.node().children.length > 0);

  /** O recuo cresce com a profundidade, que é o que torna a hierarquia legível. */
  protected readonly indent = computed(() => this.depth() * 1.35);

  protected readonly typeLabel = computed(() => UNIT_TYPE_LABELS[this.node().type] ?? this.node().type);
}
