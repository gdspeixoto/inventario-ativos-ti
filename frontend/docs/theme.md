# Tema e Design Tokens

O sistema visual é construído sobre **CSS custom properties**. Nenhum componente contém um valor hexadecimal: cores, espaçamentos, raios, sombras e tipografia vêm sempre de um token.

---

## 1. As três camadas

```
┌─────────────────────────────────────────────────────────┐
│ 1. PRIMITIVOS   --app-palette-brand-600: #2563eb        │  ← único lugar com hex
│                 --app-palette-neutral-900: #0f172a      │
├─────────────────────────────────────────────────────────┤
│ 2. SEMÂNTICOS   --app-color-primary: var(--…brand-600)  │  ← muda por tema
│                 --app-color-surface: var(--…neutral-0)  │
├─────────────────────────────────────────────────────────┤
│ 3. CONSUMO      Tailwind · PrimeNG preset · SCSS        │  ← o que você escreve
└─────────────────────────────────────────────────────────┘
```

Um componente **nunca** referencia a camada 1. Ele usa `var(--app-color-surface)`, que resolve para claro ou escuro automaticamente.

### Arquivos

```
src/styles/
├── tokens/
│   ├── _palette.scss   # camada 1 — as rampas de cor (hex)
│   ├── _base.scss      # espaçamento, raio, sombra, tipografia, motion, z-index
│   ├── _light.scss     # camada 2 — tema claro
│   ├── _dark.scss      # camada 2 — tema escuro
│   └── _index.scss     # emit() + funções auxiliares
├── _reset.scss  _typography.scss  _a11y.scss  _scrollbar.scss  _primeng.scss
└── ../styles.scss      # entrada global
```

---

## 2. Catálogo de tokens

### Cor — superfícies

| Token                        | Uso                                   |
| ---------------------------- | ------------------------------------- |
| `--app-color-background`     | Fundo da aplicação (atrás dos cards)  |
| `--app-color-surface`        | Superfície elevada padrão             |
| `--app-color-surface-hover`  | Hover sobre superfície                |
| `--app-color-surface-sunken` | Cabeçalho de tabela, blocos de código |
| `--app-color-card`           | Fundo de card                         |
| `--app-color-overlay`        | Véu de modal                          |

### Cor — texto

| Token                         | Contraste alvo | Uso                         |
| ----------------------------- | -------------- | --------------------------- |
| `--app-color-text`            | ≥ 7:1 (AAA)    | Corpo do texto              |
| `--app-color-text-secondary`  | ≥ 4.5:1 (AA)   | Descrições, legendas        |
| `--app-color-text-muted`      | ≥ 4.5:1 (AA)   | Metadados, placeholders     |
| `--app-color-text-inverse`    | —              | Texto sobre fundo invertido |
| `--app-color-text-on-primary` | ≥ 4.5:1        | Texto sobre a cor primária  |

### Cor — marca e estado

`--app-color-primary`, `-hover`, `-active`, `-contrast`, `-subtle`, `-border`
`--app-color-success` · `--app-color-warning` · `--app-color-danger` · `--app-color-info` (cada um com variante `-subtle`)

### Bordas

`--app-color-border` · `--app-color-border-strong` · `--app-color-border-subtle` · `--app-color-focus-ring`

### Espaçamento

| Token               | Valor |
| ------------------- | ----- |
| `--app-spacing-2xs` | 4px   |
| `--app-spacing-xs`  | 8px   |
| `--app-spacing-sm`  | 12px  |
| `--app-spacing-md`  | 16px  |
| `--app-spacing-lg`  | 24px  |
| `--app-spacing-xl`  | 32px  |
| `--app-spacing-2xl` | 48px  |
| `--app-spacing-3xl` | 64px  |

### Raio

`--app-radius-none` (0) · `-sm` (2px) · `-md` (4px) · `-lg` (8px) · `-pill` (999px)

> A escala é deliberadamente contida. Consoles corporativos (Azure Portal, GitLab, Backstage) usam pouco arredondamento — passa densidade e seriedade.

### Elevação

`--app-shadow-xs` · `-sm` · `-md` · `-lg`. No tema escuro as sombras são recalibradas: sombra pura não é percebida sobre fundo escuro.

### Tipografia

Base de **14px** (`--app-font-size-md`), adequada a interfaces densas. Escala: `2xs` (11px) → `3xl` (30px). Pesos: 400/500/600/700.

### Layout

`--app-navbar-height` (56px) · `--app-sidebar-width` (240px) · `--app-sidebar-width-collapsed` (56px) · `--app-content-max-width` (1440px)

---

## 3. Como consumir

### Em SCSS de componente

```scss
@use 'tokens' as tokens; // resolve via stylePreprocessorOptions.includePaths

.meu-card {
  padding: tokens.spacing(lg);
  border: 1px solid tokens.color(border);
  border-radius: tokens.radius(md);
  box-shadow: tokens.shadow(sm);

  &:focus-visible {
    @include tokens.focus-ring;
  }
}
```

Auxiliares disponíveis: `color()`, `spacing()`, `radius()`, `shadow()`, `font-size()`, `focus-ring()`, `visually-hidden()`, `truncate()`.

### Em Tailwind

`src/tailwind.css` mapeia a escala do Tailwind sobre os tokens:

```html
<div class="rounded-md border-border bg-surface p-md text-content">…</div>
<span class="text-xs text-content-secondary">…</span>
```

Utilitários disponíveis a partir dos tokens: `bg-*`/`text-*`/`border-*` para `primary`, `background`, `surface`, `card`, `content`, `content-secondary`, `border`, `success`, `warning`, `danger`, `info`; `rounded-{sm,md,lg}`; `shadow-{sm,md,lg}`; `p-{xs,sm,md,lg,xl}`.

### Em PrimeNG

`core/theme/primeng-preset.ts` aponta as variáveis `--p-*` do PrimeNG para os nossos tokens. Você **não** configura cor de componente: mude o token e o PrimeNG acompanha.

Ajustes que um preset não expressa (densidade, bordas, layout interno) ficam em `src/styles/_primeng.scss`.

> Regra prática: **é cor? mexe no preset. É caixa? mexe no `_primeng.scss`.**

---

## 4. Claro e escuro

O tema é selecionado por atributo no `<html>`:

```html
<html data-theme="light">
  <html data-theme="dark"></html>
</html>
```

Esse mesmo seletor serve a três consumidores:

- os tokens semânticos (`:root[data-theme='dark']`);
- o `darkModeSelector` do PrimeNG (`app.config.ts`);
- a variante `dark:` do Tailwind (`@custom-variant` em `tailwind.css`).

### Sem flash na carga

Um script inline mínimo no `index.html` lê a preferência salva e escreve `data-theme` **antes** do Angular iniciar. Sem ele, um usuário de tema escuro veria um flash branco a cada carregamento.

---

## 5. `ThemeService` e `ThemeStore`

`ThemeStore` guarda o estado; `ThemeService` executa os efeitos.

```ts
const theme = inject(ThemeService);

theme.mode(); // 'light' | 'dark' | 'system' (o que o usuário escolheu)
theme.resolved(); // 'light' | 'dark' (o que está pintado)
theme.isDark(); // boolean
theme.isFollowingSystem(); // boolean

theme.toggle(); // alterna e desliga o modo 'system'
theme.setMode('dark');
theme.followSystem();
theme.setDensity('compact');
```

Responsabilidades do service:

1. restaurar a preferência salva no boot;
2. observar `prefers-color-scheme` para manter `system` vivo;
3. espelhar o tema resolvido em `data-theme` e `color-scheme`;
4. persistir cada mudança em `localStorage`.

### Componente pronto

```html
<app-theme-switcher />
<!-- ícone, para a navbar -->
<app-theme-switcher variant="segmented" />
<!-- claro/escuro/sistema, para settings -->
```

---

## 6. Rebranding

Trocar a identidade visual do template:

1. **Cor da marca** — substitua a rampa `--app-palette-brand-*` em `_palette.scss` (11 tons, 50→950). Gere-a com qualquer ferramenta de escala; verifique o contraste dos passos 400 (usado no escuro) e 600 (usado no claro).
2. **Logo** — substitua `public/logo.svg`. Ajuste tamanhos em `brand-logo.component.ts` se a proporção mudar.
3. **Nome e versão** — `config/app.settings.ts`.
4. **Tipografia** — `--app-font-sans` em `_base.scss` e o `<link>` de fonte no `index.html`.
5. **Raio** — se a marca pede um visual mais arredondado, mude `--app-radius-*`. Toda a UI acompanha.

Nenhum componente precisa ser tocado.

---

## 7. Adicionando um token

1. declare-o em `_base.scss` (se for neutro ao tema) **ou** em `_light.scss` **e** `_dark.scss` (se for cor);
2. se componentes Tailwind forem consumi-lo, adicione o mapeamento em `tailwind.css`;
3. documente-o na tabela deste arquivo.

> Um token de cor adicionado só em `_light.scss` herda silenciosamente o valor claro no tema escuro. Os dois arquivos precisam andar juntos.

---

## 8. Acessibilidade do tema

- Todos os pares texto/fundo dos tokens semânticos foram escolhidos para atender WCAG AA; texto de corpo mira AAA.
- No tema escuro a marca usa o passo 400 (mais claro), porque o 600 não atinge contraste sobre superfície escura.
- `:focus-visible` tem anel consistente de 2px em toda a aplicação (`_a11y.scss`).
- `prefers-reduced-motion` zera as durações de transição.
- `forced-colors` (alto contraste do Windows) recebe tratamento específico.

---

## Leitura relacionada

- [`layout.md`](./layout.md) — como o shell usa os tokens de layout
- [`creating-component.md`](./creating-component.md) — checklist ao criar componentes
