# Padrões de código

Convenções obrigatórias. O que dá para automatizar está no ESLint e no Prettier; o restante depende de revisão.

---

## 1. TypeScript

`strict` habilitado, mais `noImplicitOverride`, `noPropertyAccessFromIndexSignature`, `noImplicitReturns` e `noFallthroughCasesInSwitch`.

**`interface` para formatos de dado, `type` para uniões e utilitários.**

```ts
interface User {
  id: string;
  name: string;
}
type Status = 'idle' | 'loading' | 'success' | 'error';
```

**`any` é proibido** (aviso no ESLint, erro em revisão). Use `unknown` e estreite o tipo:

```ts
function handle(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
```

**Modelos são imutáveis.** `readonly` nas propriedades e `Object.freeze` no que atravessa camadas.

**Nada de `enum`.** Use uniões literais ou objetos `as const` — melhor tree-shaking e sem o runtime extra.

```ts
export const THEME_MODES = ['light', 'dark', 'system'] as const;
export type ThemeMode = (typeof THEME_MODES)[number];
```

---

## 2. Componentes

Todo componente é **standalone** e **OnPush**.

```ts
@Component({
  selector: 'app-user-card',
  imports: [Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-card.component.html',
  styleUrl: './user-card.component.scss',
})
export class UserCardComponent {
  private readonly api = inject(ApiService);

  readonly user = input.required<User>();
  readonly compact = input(false);
  readonly selected = output<User>();

  protected readonly fullName = computed(() => `${this.user().name}`);
}
```

Regras:

- `inject()` em vez de constructor injection;
- `input()` / `output()` / `model()` em vez dos decorators;
- membros `private` para dependências, `protected` para o que o template usa, `readonly` sempre que possível;
- **sem lógica de negócio no componente** — ela vive em serviços e stores.

### Template inline ou arquivo separado

| Tamanho        | Onde                 |
| -------------- | -------------------- |
| Até ~40 linhas | Inline (`template:`) |
| Acima disso    | `.html` separado     |

O mesmo vale para estilos. Componentes pequenos e reutilizáveis (`Avatar`, `Loading`) ficam mais legíveis inline; telas ficam melhores em arquivos separados.

> Cuidado com **crases dentro de `template:` / `styles:`** — quebram o template literal. Um comentário com <code>`md`</code> já derruba o build.

### Control flow

Use `@if` / `@for` / `@switch`. `*ngIf` e `*ngFor` são bloqueados por lint.

```html
@if (loading()) {
<app-loading />
} @else { @for (user of users(); track user.id) {
<app-user-card [user]="user" />
} @empty {
<app-empty-state />
} }
```

`track` é obrigatório e deve ser um identificador estável — nunca `$index` para listas que mudam de ordem.

---

## 3. Signals

```ts
// Estado interno
private readonly _items = signal<Item[]>([]);
readonly items = this._items.asReadonly();

// Derivado — sempre computed, nunca recalculado à mão
readonly total = computed(() => this._items().length);
readonly hasItems = computed(() => this.total() > 0);
```

**`effect()` é o último recurso.** Ele serve para efeitos colaterais fora do Angular (DOM, storage, log). Nunca use `effect` para derivar estado — é para isso que existe `computed`.

Quando `effect()` for criado fora de um contexto de injeção, passe o `Injector` explicitamente:

```ts
effect(() => { … }, { injector: this.injector });
```

---

## 4. Serviços e stores

```ts
@Injectable({ providedIn: 'root' })
export class UserStore {
  private readonly _users = signal<User[]>([]);
  readonly users = this._users.asReadonly();
  readonly activeCount = computed(() => this._users().filter((u) => u.active).length);

  setUsers(users: User[]): void {
    this._users.set(users);
  }
}
```

- **Store** = estado + derivações, síncrono, sem I/O.
- **Service** = efeitos colaterais, HTTP, timers, DOM.

Serviços de feature usam `ApiService`, nunca `HttpClient` diretamente.

---

## 5. Assíncrono

`Promise` por padrão; `Observable` quando houver cancelamento, múltiplas emissões ou composição temporal.

```ts
// Bom
const users = await this.api.get<User[]>('/users');

// Também bom — há cancelamento
readonly results$ = this.term$.pipe(
  debounceTime(300),
  distinctUntilChanged(),
  switchMap((term) => this.api.get$<User[]>('/users', { params: { term } })),
);
```

Toda inscrição manual precisa de `takeUntilDestroyed()`.

Use `asyncSignal<T>()` (`@shared/models/async-state.model`) para telas com carregamento:

```ts
readonly users = asyncSignal<User[]>();
ngOnInit(): void { void this.users.run(() => this.api.get<User[]>('/users')); }
```

---

## 6. Estilos

**Nunca escreva um valor hexadecimal fora de `_palette.scss`.**

```scss
// Errado
.card {
  background: #ffffff;
  padding: 24px;
  border-radius: 8px;
}

// Certo
@use 'tokens' as tokens;
.card {
  background: tokens.color(card);
  padding: tokens.spacing(lg);
  border-radius: tokens.radius(lg);
}
```

- Nomes de classe em BEM com prefixo do componente: `.user-card__title`, `.user-card--compact`.
- `::ng-deep` só para estilizar internos do PrimeNG, sempre escopado (`:host ::ng-deep`).
- `!important` apenas para vencer estilos inline de terceiros, com comentário explicando.
- Media queries com sintaxe de range: `@media (width < 48rem)`.

---

## 7. Acessibilidade

Não negociável:

- todo controle interativo é `<button>` ou `<a>` — nunca um `<div>` com `(click)`;
- todo input tem `<label>` associado (`for`/`id`);
- botões só com ícone têm `aria-label`;
- imagens decorativas: `alt=""` + `aria-hidden="true"`;
- ícones dentro de botões com texto: `aria-hidden="true"`;
- ordem de foco segue a ordem visual;
- estado dinâmico usa `aria-live` (veja `LoadingComponent`);
- contraste mínimo 4.5:1 para texto normal, 3:1 para texto grande.

O ESLint roda `@angular-eslint/template/accessibility`, que pega parte disso automaticamente.

---

## 8. Internacionalização

Nenhuma string visível fica no código.

```html
<!-- Errado -->
<h1>Usuários</h1>

<!-- Certo -->
<h1>{{ 'users.title' | translate }}</h1>
```

```ts
this.toast.success('users.created');
this.i18n.translate('users.count', { total: 12 });
```

Chaves em `camelCase` agrupadas por domínio: `users.list.emptyMessage`. Ao adicionar uma chave, adicione-a em **todos** os bundles de `public/i18n/`.

---

## 9. Comentários

Comente **por que**, não **o que**.

```ts
// Ruim
// incrementa o contador
count++;

// Bom
// O `state` é de uso único: consumimos antes de qualquer await para que um
// callback duplicado não consiga repetir a troca do código.
this.storage.clearRequestState();
```

Use TSDoc em APIs públicas (serviços, componentes compartilhados, funções exportadas), descrevendo contrato e casos-limite.

---

## 10. Testes

Nomeie o comportamento, não a implementação:

```ts
// Ruim
it('should call service', …);

// Bom
it('rejects a replayed nonce', …);
it('resolves `system` against the OS preference', …);
```

Estrutura: arrange / act / assert. Uma asserção conceitual por teste. Sem mocks de coisas que você não controla — teste o contrato.

---

## 11. Ferramentas

| Ferramenta   | Configuração           | Quando roda                  |
| ------------ | ---------------------- | ---------------------------- |
| ESLint       | `eslint.config.js`     | `npm run lint`, pre-commit   |
| Prettier     | `.prettierrc`          | `npm run format`, pre-commit |
| lint-staged  | `.lintstagedrc.json`   | pre-commit                   |
| commitlint   | `commitlint.config.js` | commit-msg                   |
| EditorConfig | `.editorconfig`        | no editor                    |

Regras de lint específicas do template: `prefer-standalone`, `prefer-on-push-component-change-detection`, `prefer-control-flow`, `prefer-self-closing-tags`, `explicit-member-accessibility`, `consistent-type-definitions`, `eqeqeq`, `curly`, `no-console` (exceto no `LoggerService`).

---

## 12. Checklist de revisão

- [ ] Componente standalone e OnPush
- [ ] Nenhum `any`
- [ ] Nenhum valor hexadecimal fora da paleta
- [ ] Nenhuma string visível fora do i18n
- [ ] Interativos acessíveis por teclado, com rótulo
- [ ] Estado lido pela view é signal
- [ ] Inscrições com `takeUntilDestroyed()`
- [ ] Regra de dependência respeitada (`pages → shared → core`)
- [ ] Comentários explicam o porquê
- [ ] Testes cobrem o comportamento novo
