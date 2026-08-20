# Layout

O layout corporativo vive em `src/app/core/layout`. Ele é a moldura fixa da aplicação autenticada; as páginas só preenchem o miolo.

Referências visuais: OpenMetadata, Backstage, Azure Portal e GitLab — interface densa, pouco arredondamento, muito espaço em branco e hierarquia construída por peso tipográfico, não por tamanho.

---

## 1. Estrutura

```
┌──────────────────────────────────────────────────────────┐
│ Navbar   [☰] logo | busca |        [🌐] [🌓] [avatar ▾]  │ 56px, sticky
├──────────┬───────────────────────────────────────────────┤
│          │  ← barra de progresso (durante requisições)    │
│ Sidebar  │  ┌─────────────────────────────────────────┐  │
│ 240px    │  │ Breadcrumb                              │  │
│ (56px    │  │ Título da página        [ações]         │  │ ← PageHeader
│  rail)   │  ├─────────────────────────────────────────┤  │
│          │  │ <router-outlet />                       │  │
│          │  └─────────────────────────────────────────┘  │
│          │  Footer — versão do sistema e do Angular       │
└──────────┴───────────────────────────────────────────────┘
```

Componentes:

| Arquivo                       | Responsabilidade                                         |
| ----------------------------- | -------------------------------------------------------- |
| `shell/shell.component.*`     | Monta a moldura, skip link, barra de progresso, backdrop |
| `navbar/navbar.component.*`   | Toggle, marca, busca, idioma, tema, menu do usuário      |
| `sidebar/sidebar.component.*` | Navegação filtrada por permissão, rail e drawer          |
| `footer/footer.component.ts`  | Versões                                                  |
| `layout.store.ts`             | Estado do chrome                                         |
| `menu.service.ts`             | Filtra o menu pelo usuário atual                         |
| `breadcrumb.service.ts`       | Monta a trilha a partir do `data.breadcrumb` das rotas   |
| `app-title.strategy.ts`       | Título do documento a partir do `title` da rota          |

---

## 2. Responsividade

| Faixa               | Sidebar                       | Navbar                        |
| ------------------- | ----------------------------- | ----------------------------- |
| ≥ 1024px (desktop)  | Fixa, 240px (ou rail de 56px) | Completa                      |
| 768–1023px (tablet) | Fixa, 240px (ou rail)         | Busca oculta                  |
| < 768px (celular)   | Drawer sobreposto + backdrop  | Busca oculta, avatar sem nome |

`LayoutStore` separa dois conceitos que costumam ser confundidos:

- `collapsed` — modo rail no desktop; **é preferência do usuário e é persistida**;
- `mobileOpen` — drawer no celular; **é transitório e nunca persistido**.

Um `effect` fecha o drawer automaticamente ao sair da faixa mobile, evitando um overlay órfão ao girar o dispositivo.

Os breakpoints vêm de `BreakpointService` (CDK) e usam exatamente os mesmos valores da escala Tailwind — componente e folha de estilo nunca discordam sobre o que é "mobile".

---

## 3. Navegação

O menu é **dado**, declarado em `config/menu.config.ts`:

```ts
export const menuSections: AppMenuSection[] = [
  {
    id: 'main',
    label: 'nav.sections.main',
    items: [
      {
        id: 'dashboard',
        label: 'nav.dashboard',
        icon: 'pi pi-home',
        route: '/dashboard',
        exact: true,
      },
      {
        id: 'cadastros',
        label: 'nav.registrations',
        icon: 'pi pi-folder',
        children: [{ id: 'users', label: 'nav.users', route: '/users', roles: ['admin'] }],
      },
      { id: 'docs', label: 'nav.documentation', icon: 'pi pi-book', url: 'https://…' },
    ],
  },
];
```

| Campo         | Efeito                                          |
| ------------- | ----------------------------------------------- |
| `route`       | Link interno                                    |
| `url`         | Link externo (abre em nova aba)                 |
| `children`    | Grupo expansível                                |
| `roles`       | Oculta o item sem pelo menos uma das roles      |
| `permissions` | Oculta o item sem pelo menos uma das permissões |
| `exact`       | Ativo apenas na rota exata (use no dashboard)   |
| `badge`       | Contador à direita                              |
| `visible`     | `false` remove o item (útil com feature flag)   |

`MenuService` filtra recursivamente e descarta grupos que ficaram vazios — nenhum expansor sem conteúdo sobra na tela.

> Ocultar um item **não** protege a rota. Aplique também `roleGuard`/`permissionGuard`.

---

## 4. Breadcrumb e título

Ambos saem dos metadados da rota:

```ts
{
  path: 'usuarios',
  title: 'users.title',              // → título do documento (chave i18n)
  data: { breadcrumb: 'nav.users' }, // → trilha
  loadComponent: () => …,
}
```

Rotas sem `breadcrumb` são ignoradas na trilha, então nós puramente estruturais (`''`, wrappers de `:id`) não a poluem.

Para uma folha dinâmica — o nome do registro carregado:

```ts
private readonly breadcrumb = inject(BreadcrumbService);

async ngOnInit(): Promise<void> {
  const user = await this.api.get<User>(`/users/${this.id()}`);
  this.breadcrumb.append({ label: user.name });
}
```

O titulo do documento fica `Usuarios · Inventario de Ativos de TI` (separador em `app.settings.ts`).

---

## 5. Anatomia de uma página

Toda página roteada começa igual:

```html
<app-page-header title="users.title" description="users.subtitle" icon="pi pi-users">
  <p-button [label]="'users.new' | translate" icon="pi pi-plus" (onClick)="novo()" />
</app-page-header>

<app-card title="users.list" [flush]="true">
  @if (users.isLoading()) { <app-loading /> } @else if (users.hasError()) {
  <app-error-state [error]="users.error()" (retry)="load()" /> } @else if (users.isEmpty()) {
  <app-empty-state actionLabel="users.new" (action)="novo()" /> } @else {
  <p-table [value]="users.data() ?? []"> … </p-table> }
</app-card>
```

Essa uniformidade é o que faz módulos escritos por squads diferentes parecerem o mesmo produto.

---

## 6. Acessibilidade

O layout entrega, sem esforço adicional da página:

| Recurso                             | Onde                      | Critério WCAG |
| ----------------------------------- | ------------------------- | ------------- |
| Skip link como primeiro focável     | `shell.component.html`    | 2.4.1         |
| `<main tabindex="-1">`              | `shell.component.html`    | 2.4.1         |
| `<nav aria-label>` na sidebar       | `sidebar.component.html`  | 1.3.1         |
| `aria-current="page"` no item ativo | `sidebar`, `breadcrumb`   | 2.4.8         |
| Breadcrumb em `<nav><ol>`           | `breadcrumb.component.ts` | 2.4.8         |
| `aria-haspopup`/`aria-expanded`     | `user-menu.component.ts`  | 4.1.2         |
| `inert` no drawer fechado           | `shell.component.html`    | 2.4.3         |
| Anel de foco consistente            | `_a11y.scss`              | 2.4.7         |
| Barra de progresso com `role`       | `shell.component.html`    | 4.1.3         |

---

## 7. Customizações comuns

**Sidebar à direita** — inverta a ordem no flex de `.app-shell__body` e troque `border-right` por `border-left`.

**Sem sidebar** — remova `<app-sidebar>` do shell e desative `collapsibleSidebar` em `app.settings.ts`.

**Navbar mais alta** — ajuste `--app-navbar-height` em `_base.scss`; shell e sidebar já derivam a altura dele.

**Conteúdo em largura total** — mude `--app-content-max-width` para `none`.

**Busca funcional** — implemente `NavbarComponent.onSearch()`; o campo já está pronto e acessível.

---

## Leitura relacionada

- [`theme.md`](./theme.md) — tokens de layout e tema
- [`creating-page.md`](./creating-page.md) — passo a passo para uma nova página
