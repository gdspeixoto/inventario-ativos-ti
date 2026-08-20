# Arquitetura

Este documento descreve **como o template é organizado e por quê**. Ele é a leitura inicial obrigatória para qualquer pessoa que vá construir um sistema a partir desta base.

---

## 1. Princípio central: dependências apontam para dentro

O projeto aplica Clean Architecture adaptada ao frontend. A regra fundamental é:

```
pages  ──▶  shared  ──▶  core
  │                        ▲
  └────────────────────────┘
config ──▶ (preenche tokens de core, via app.config.ts)
```

| Camada         | Pode importar de                  | Nunca importa de               |
| -------------- | --------------------------------- | ------------------------------ |
| `core/`        | Angular, bibliotecas              | `pages/`, `config/`, `shared/` |
| `core/layout/` | o acima **+ `shared/components`** | `pages/`, `config/`            |
| `shared/`      | Angular, `core/`                  | `pages/`                       |
| `pages/`       | Angular, `core/`, `shared/`       | outra `pages/`                 |
| `config/`      | tipos de `core/`, `@env`          | `pages/`, `shared/`            |

> **A exceção de `core/layout`.** Ele é a moldura visual da aplicação — navbar, sidebar, shell — e por definição compõe componentes reutilizáveis (`ThemeSwitcher`, `UserMenu`, `BrandLogo`). É a única parte de `core/` autorizada a importar de `shared/components`. O restante de `core/` (autenticação, HTTP, tema, serviços) permanece livre de qualquer dependência de UI, que é o que o torna reutilizável.

A consequência prática é que **`core/` é reutilizável entre projetos sem edição**. Ele não conhece o nome do sistema, a cor da marca, o provedor OAuth nem os itens do menu — recebe tudo por injeção de dependência.

### Como a inversão acontece

`core/` declara _tokens_; `config/` declara _valores_; `app.config.ts` liga os dois.

```ts
// core/theme/theme.tokens.ts — a biblioteca declara o que precisa
export const THEME_CONFIG = new InjectionToken<ThemeConfig>('THEME_CONFIG');

// config/theme.config.ts — o projeto declara o valor
export const themeConfig: ThemeConfig = { defaultMode: 'system', ... };

// app.config.ts — a raiz de composição conecta
{ provide: THEME_CONFIG, useValue: themeConfig }
```

Isso vale para `APP_SETTINGS`, `AUTH_CONFIG`, `THEME_CONFIG`, `I18N_CONFIG` e `MENU_SECTIONS`.

---

## 2. Mapa de diretórios

```
src/
├── app/
│   ├── app.ts                    # raiz: router-outlet + hosts de toast/confirm
│   ├── app.config.ts             # raiz de composição (providers)
│   ├── app.routes.ts             # rotas, todas lazy
│   │
│   ├── config/                   # ÚNICO lugar que um novo projeto edita
│   │   ├── app.settings.ts       # nome, versão, logo, feature flags
│   │   ├── auth.config.ts        # provedores OIDC, rotas de auth
│   │   ├── theme.config.ts       # modo padrão, densidade, PrimeNG
│   │   ├── i18n.config.ts        # idiomas disponíveis
│   │   └── menu.config.ts        # navegação lateral
│   │
│   ├── core/                     # infraestrutura — singleton, sem UI de negócio
│   │   ├── authentication/       # biblioteca OIDC (ver authentication.md)
│   │   ├── errors/               # AppHttpError + ErrorHandler global
│   │   ├── guards/               # auth, guest, role, permission
│   │   ├── http/                 # HttpContext tokens
│   │   ├── i18n/                 # TranslationService + pipe
│   │   ├── interceptors/         # apiUrl, auth, error, loading
│   │   ├── layout/               # shell, navbar, sidebar, footer, menu, breadcrumb
│   │   ├── services/             # Api, Storage, Platform, Logger, Toast, ...
│   │   ├── theme/                # tokens, store, service, preset PrimeNG
│   │   └── app-settings.ts       # token + contrato de identidade da aplicação
│   │
│   ├── shared/                   # reutilizável e sem estado
│   │   ├── components/           # Avatar, AppCard, PageHeader, EmptyState, ...
│   │   ├── directives/           # *appHasRole, *appHasPermission, appAutofocus
│   │   ├── pipes/                # initials, timeAgo
│   │   ├── models/               # Page, AsyncState
│   │   └── utils/
│   │
│   └── pages/                    # telas roteadas
│       ├── login/  auth-callback/  dashboard/  example/  settings/  errors/
│
├── environments/                 # environment.ts | environment.production.ts
├── styles/                       # tokens/ + reset, typography, a11y, primeng
├── styles.scss                   # entrada SCSS global
├── tailwind.css                  # entrada Tailwind (carregada antes)
└── index.html
```

---

## 3. Estado: signals, não NgRx

O template usa **stores baseados em signals**. Um store é uma classe injetável que:

1. mantém `signal`s privados e expõe versões `readonly`;
2. deriva estado com `computed`;
3. muda estado apenas por métodos com nome de intenção.

```ts
@Injectable({ providedIn: 'root' })
export class ThemeStore {
  private readonly _mode = signal<ThemeMode>('system');
  readonly mode = this._mode.asReadonly();
  readonly isDark = computed(() => this.resolved() === 'dark');

  setMode(mode: ThemeMode): void {
    this._mode.set(mode);
  }
}
```

Stores existentes: `AuthStore`, `ThemeStore`, `LayoutStore` (+ `LoadingService`, que é um contador).

**Por que não NgRx.** Actions, reducers, effects e selectors resolvem um problema real — time-travel debugging e fluxos assíncronos complexos compartilhados. A maioria dos sistemas internos não tem esse problema, e paga o custo em boilerplate. Signals dão granularidade fina, `computed` memoizado e zero cerimônia. Se um módulo específico vier a precisar de NgRx, ele pode ser adotado localmente sem reescrever o template.

**Separação store/service.** O store é síncrono e trivialmente testável; efeitos colaterais (DOM, storage, HTTP, timers) ficam no service correspondente. `ThemeStore` decide qual é o tema; `ThemeService` escreve no `<html>` e persiste.

---

## 4. RxJS: onde ainda faz sentido

A regra é: **`Promise` por padrão, `Observable` quando há fluxo**.

Use RxJS quando existir cancelamento, múltiplas emissões ou composição temporal:

- eventos do `Router` (`BreadcrumbService`);
- `BreakpointObserver` do CDK (`BreakpointService`);
- interceptors (a API do Angular é baseada em `Observable`);
- typeahead, polling, upload com progresso (`ApiService.upload`).

Use `Promise` para requisições pontuais — que é a esmagadora maioria:

```ts
const users = await this.api.get<User[]>('/users');
```

`ApiService` oferece as duas formas: `get()` retorna `Promise`, `get$()` retorna `Observable`.

---

## 5. Zoneless e OnPush

O projeto roda **sem `zone.js`** (padrão do Angular 21). A detecção de mudanças é disparada por signals, e não por _monkey-patching_ de APIs do browser. Consequências:

- todo componente usa `ChangeDetectionStrategy.OnPush` (garantido por regra de ESLint e pelo schematic em `angular.json`);
- estado que a view lê deve ser um `signal`/`computed`, não um campo simples mutado por callback;
- `setTimeout`, `addEventListener` e afins **não** disparam renderização sozinhos.

---

## 6. Roteamento e lazy loading

Todas as rotas usam `loadComponent`. A árvore tem dois níveis:

- **rotas públicas** (`/login`, `/auth/callback`, `/401`…`/500`) renderizam sozinhas;
- **rotas privadas** ficam sob `ShellComponent`, com `authGuard` aplicado **uma única vez** no nó pai.

```ts
{
  path: '',
  canActivate: [authGuard],
  loadComponent: () => import('@core/layout/shell/shell.component')...,
  children: [ /* nenhuma página aqui pode ser esquecida sem proteção */ ],
}
```

`data.breadcrumb` alimenta o `BreadcrumbService`; `title` alimenta o `AppTitleStrategy` (tratado como chave i18n).

---

## 7. Sequência de inicialização

`provideAppInitializer` roda **antes da primeira rota ser ativada**:

1. `ThemeService.initialize()` — aplica o tema (o `<script>` inline no `index.html` já evitou o flash);
2. `TranslationService.initialize()` — carrega o bundle do idioma;
3. `AuthService.restoreSession()` — restaura ou renova a sessão.

Por isso os guards são **síncronos**: quando eles rodam, o status da sessão nunca é `unknown`. Não há piscar de tela de login para um usuário já autenticado.

---

## 8. Camada HTTP

Ordem dos interceptors (`app.config.ts`), com justificativa:

```
requisição →  apiUrl  →  loading  →  error  →  auth  →  rede
resposta   ←  apiUrl  ←  loading  ←  error  ←  auth  ←  rede
```

`authInterceptor` fica **mais próximo da rede** de propósito: ele precisa ver o `HttpErrorResponse` cru de um 401 para tentar o refresh, antes que o `errorInterceptor` normalize o erro em `AppHttpError` e exiba o toast.

Comportamentos pontuais são controlados por `HttpContext`, não por regex de URL:

```ts
this.api.get('/users', { silent: true, background: true, retries: 2 });
```

---

## 9. Erros

| Origem                | Quem trata         | Efeito                               |
| --------------------- | ------------------ | ------------------------------------ |
| HTTP 4xx/5xx          | `errorInterceptor` | toast + `AppHttpError` propagado     |
| HTTP 401              | `authInterceptor`  | refresh silencioso; logout se falhar |
| HTTP 403              | `errorInterceptor` | redireciona para `/403`              |
| Exceção não capturada | `AppErrorHandler`  | log + toast genérico (com throttle)  |
| Falha de chunk        | `AppErrorHandler`  | mensagem de indisponibilidade        |

---

## 10. Decisões registradas

| Decisão                                | Alternativa descartada | Motivo                                                                                |
| -------------------------------------- | ---------------------- | ------------------------------------------------------------------------------------- |
| Biblioteca OIDC própria                | `angular-oauth2-oidc`  | Controle total do fluxo, zero dependência crítica, troca de provedor por configuração |
| Signals stores                         | NgRx                   | Boilerplate desproporcional ao problema                                               |
| i18n em runtime                        | `@angular/localize`    | Troca de idioma sem rebuild e sem um bundle por locale                                |
| Design tokens em CSS variables         | Variáveis SCSS         | Troca de tema em runtime sem recompilar                                               |
| Tailwind **e** PrimeNG                 | Só um dos dois         | PrimeNG dá os componentes complexos; Tailwind dá o layout sem CSS morto               |
| Componentes com sufixo `.component.ts` | Convenção sem sufixo   | Evita colisão de nomes com PrimeNG (`Avatar`, `Menu`, `Card`)                         |
| `Promise` como padrão na API           | `Observable` em tudo   | Código linear e legível para requisições pontuais                                     |

---

## Leitura relacionada

- [`folder-structure.md`](./folder-structure.md) — o que colocar em cada pasta
- [`authentication.md`](./authentication.md) — a biblioteca de autenticação em detalhe
- [`coding-standards.md`](./coding-standards.md) — convenções de código
