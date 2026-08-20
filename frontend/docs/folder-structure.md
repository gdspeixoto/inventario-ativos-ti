# Estrutura de pastas

Guia prático de **onde colocar cada coisa**. Em caso de dúvida, a pergunta certa é: _"quem precisa disso?"_

| Quem precisa                    | Onde vai          |
| ------------------------------- | ----------------- |
| Uma única página                | `pages/<pagina>/` |
| Duas ou mais páginas            | `shared/`         |
| A infraestrutura da aplicação   | `core/`           |
| Só muda de projeto para projeto | `config/`         |
| Muda de ambiente (dev/prod)     | `environments/`   |

---

## Árvore comentada

```
src/
├── app/
│   ├── app.ts                  # raiz: router-outlet + <p-toast> + <p-confirmdialog>
│   ├── app.config.ts           # raiz de composição — providers e initializers
│   ├── app.routes.ts           # rotas (todas lazy)
│   │
│   ├── config/                 ← COMECE AQUI em um projeto novo
│   │   ├── app.settings.ts     # nome, versão, logo, suporte, feature flags
│   │   ├── auth.config.ts      # provedores OIDC e rotas de autenticação
│   │   ├── theme.config.ts     # modo padrão, densidade, opções PrimeNG
│   │   ├── i18n.config.ts      # idiomas disponíveis
│   │   └── menu.config.ts      # navegação lateral
│   │
│   ├── core/                   ← infraestrutura; singletons; sem UI de negócio
│   │   ├── app-settings.ts     # token + contrato de AppSettings
│   │   ├── authentication/
│   │   │   ├── index.ts              # API pública
│   │   │   ├── auth.service.ts       # fachada
│   │   │   ├── auth.store.ts         # estado da sessão
│   │   │   ├── auth.errors.ts
│   │   │   ├── auth.tokens.ts
│   │   │   ├── models/               # AuthConfig, AuthTokens, User
│   │   │   ├── providers/            # interface + Keycloak/Microsoft/OIDC + registry
│   │   │   └── internal/             # pkce, jwt, claims, storage, oidc-client
│   │   ├── errors/             # AppHttpError, AppErrorHandler
│   │   ├── guards/             # auth, guest, role, permission
│   │   ├── http/               # HttpContext tokens
│   │   ├── i18n/               # TranslationService, TranslatePipe
│   │   ├── interceptors/       # apiUrl, auth, error, loading
│   │   ├── layout/             # shell, navbar, sidebar, footer, stores do chrome
│   │   ├── services/           # Api, Storage, Platform, Logger, Toast, Confirm, Loading, Breakpoint
│   │   └── theme/              # tokens DI, store, service, preset PrimeNG
│   │
│   ├── shared/                 ← reutilizável, sem estado global, sem regra de negócio
│   │   ├── components/         # Avatar, AppCard, PageHeader, Loading, EmptyState, ErrorState,
│   │   │                       # Breadcrumb, ThemeSwitcher, LanguageSwitcher, UserMenu, BrandLogo
│   │   ├── directives/         # *appHasRole, *appHasPermission, appAutofocus
│   │   ├── pipes/              # initials, timeAgo
│   │   ├── models/             # Page/PageRequest, AsyncState
│   │   └── utils/              # funções puras
│   │
│   └── pages/                  ← telas roteadas; uma pasta por página
│       ├── login/
│       ├── auth-callback/
│       ├── dashboard/
│       ├── example/            # copie esta pasta ao criar uma listagem
│       ├── settings/
│       └── errors/
│
├── environments/
│   ├── environment.ts             # desenvolvimento
│   └── environment.production.ts  # produção (substituído no build)
│
├── styles/
│   ├── tokens/                 # _palette _base _light _dark _index
│   ├── _reset.scss  _typography.scss  _a11y.scss  _scrollbar.scss  _primeng.scss
├── styles.scss                 # entrada SCSS global
├── tailwind.css                # entrada Tailwind (carregada ANTES do styles.scss)
└── index.html

public/
├── logo.svg
└── i18n/
    ├── pt-BR.json
    └── en-US.json

docs/                           # esta documentação
```

---

## Convenções de nome

| Tipo         | Arquivo                  | Classe / símbolo     |
| ------------ | ------------------------ | -------------------- |
| Componente   | `user-list.component.ts` | `UserListComponent`  |
| Serviço      | `user.service.ts`        | `UserService`        |
| Store        | `user.store.ts`          | `UserStore`          |
| Guard        | `auth.guard.ts`          | `authGuard` (função) |
| Interceptor  | `auth.interceptor.ts`    | `authInterceptor`    |
| Pipe         | `initials.pipe.ts`       | `InitialsPipe`       |
| Diretiva     | `has-role.directive.ts`  | `HasRoleDirective`   |
| Modelo       | `user.model.ts`          | `interface User`     |
| Token DI     | `auth.tokens.ts`         | `AUTH_CONFIG`        |
| Configuração | `auth.config.ts`         | `authConfig`         |
| Teste        | `*.spec.ts`              | —                    |

Arquivos em **kebab-case**, classes em **PascalCase**, funções e variáveis em **camelCase**, tokens de DI em **SCREAMING_SNAKE_CASE**.

> **Sobre o sufixo `.component.ts`.** O guia de estilo mais recente do Angular sugere omiti-lo. Este template o mantém porque o projeto usa PrimeNG intensamente, e classes como `Avatar`, `Menu`, `Card` e `Toast` colidiriam. O sufixo elimina qualquer ambiguidade no import.

---

## Aliases de import

Configurados em `tsconfig.json`:

```ts
import { AuthService } from '@core/authentication';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { authConfig } from '@config/auth.config';
import { environment } from '@env/environment';
```

| Alias       | Aponta para          |
| ----------- | -------------------- |
| `@app/*`    | `src/app/*`          |
| `@core/*`   | `src/app/core/*`     |
| `@shared/*` | `src/app/shared/*`   |
| `@config/*` | `src/app/config/*`   |
| `@pages/*`  | `src/app/pages/*`    |
| `@env/*`    | `src/environments/*` |

**Nunca** use caminhos relativos que sobem de pasta (`../../core/...`). Dentro da mesma feature, relativo é bem-vindo (`./user.service`).

---

## Uma feature maior que uma página

Quando um módulo cresce (várias telas, serviços e modelos próprios), organize-o internamente:

```
pages/users/
├── users.routes.ts            # rotas filhas da feature
├── list/
│   ├── user-list.component.ts|html|scss
│   └── user-list.component.spec.ts
├── detail/
│   └── user-detail.component.ts|html|scss
├── components/                # componentes SÓ desta feature
│   └── user-status-badge/
├── services/
│   └── user.service.ts
└── models/
    └── user.model.ts
```

E no roteamento principal:

```ts
{
  path: 'users',
  loadChildren: () => import('@pages/users/users.routes').then((m) => m.USERS_ROUTES),
}
```

Se um componente de `pages/users/components/` passar a ser usado por outra feature, **promova-o para `shared/`** — não importe entre features.

---

## Regras de dependência

```
pages  ──▶  shared  ──▶  core
config ──▶  core (apenas tipos)
```

| Proibido                               | Por quê                                         |
| -------------------------------------- | ----------------------------------------------- |
| `core/` importar de `pages/`           | Quebra a reutilização da infraestrutura         |
| `core/` importar de `config/`          | Acopla a biblioteca ao projeto                  |
| `core/` importar de `shared/`\*        | Infraestrutura não deve depender de UI          |
| `shared/` importar de `pages/`         | Inverte a dependência                           |
| Uma `pages/a` importar de `pages/b`    | Acoplamento entre features; promova a `shared/` |
| Importar de `authentication/internal/` | É detalhe de implementação; use `index.ts`      |

\* **Única exceção:** `core/layout/` pode importar de `shared/components/`, porque a moldura da aplicação é, por natureza, composição de componentes de UI. Ver [`architecture.md`](./architecture.md).

---

## Leitura relacionada

- [`architecture.md`](./architecture.md) — a razão de ser dessa divisão
- [`creating-page.md`](./creating-page.md) · [`creating-component.md`](./creating-component.md)
