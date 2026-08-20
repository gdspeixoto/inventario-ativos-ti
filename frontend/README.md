# Inventario de Ativos de TI Frontend

Frontend Angular do Inventario de Ativos de TI, com Keycloak/OIDC, arquitetura limpa, temas, i18n, guards, interceptors e PrimeNG.

Use esta base para desenvolver as telas de licencas, servidores, contratos, fornecedores, custos, reajustes, timelines, alertas e relatorios.

```bash
cd frontend
npm install
npm start
```

---

## Sumário

- [Visão geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Instalação](#instalação)
- [Execução](#execução)
- [Build](#build)
- [Temas](#temas)
- [Autenticação](#autenticação)
- [Como adicionar páginas](#como-adicionar-páginas)
- [Como trocar o provedor OAuth](#como-trocar-o-provedor-oauth)
- [Como adicionar novos provedores](#como-adicionar-novos-provedores)
- [Como publicar](#como-publicar)
- [Qualidade](#qualidade)
- [Documentação](#documentação)

---

## Visão geral

| Recurso            | Como está resolvido                                                                                                                         |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| **Autenticação**   | Biblioteca OIDC própria, Authorization Code + PKCE. Keycloak, Microsoft Entra ID, Azure AD B2C e qualquer OIDC — trocáveis por configuração |
| **Autorização**    | `authGuard`, `guestGuard`, `roleGuard`, `permissionGuard` + diretivas `*appHasRole` e `*appHasPermission`                                   |
| **Temas**          | Design tokens em CSS variables, temas claro e escuro completos, detecção do sistema, persistência                                           |
| **UI**             | PrimeNG 21 com preset ligado aos tokens + TailwindCSS 4 para layout                                                                         |
| **Estado**         | Signals stores (`AuthStore`, `ThemeStore`, `LayoutStore`) — sem NgRx                                                                        |
| **HTTP**           | `ApiService` tipado + 4 interceptors (URL base, token, erros, loading)                                                                      |
| **Erros**          | `ErrorHandler` global, `AppHttpError` normalizado, telas 401/403/404/500                                                                    |
| **i18n**           | Tradução em runtime, pt-BR e en-US, troca sem recarregar                                                                                    |
| **Acessibilidade** | WCAG 2.2 AA: skip link, foco visível, ARIA, contraste, `prefers-reduced-motion`                                                             |
| **Responsividade** | Desktop, notebook, tablet e celular — sidebar vira drawer                                                                                   |
| **Qualidade**      | ESLint, Prettier, Husky, lint-staged, commitlint, EditorConfig, VS Code                                                                     |
| **Testes**         | Vitest configurado, com specs de referência                                                                                                 |

### Stack

Angular 21 (standalone, signals, control flow, zoneless) · TypeScript 5.9 · PrimeNG 21 · TailwindCSS 4 · Angular CDK · SCSS · Vitest

**Sem dependência de biblioteca OAuth de terceiros** — o fluxo PKCE é implementado internamente, em `src/app/core/authentication`.

---

## Arquitetura

```
pages  ──▶  shared  ──▶  core
config ──▶  core (apenas tipos, via tokens de injeção)
```

`core/` não conhece o nome do sistema, a cor da marca, o provedor OAuth nem o menu. Tudo isso chega por injeção de dependência a partir de `config/` — o que torna a camada de infraestrutura reutilizável entre projetos sem edição.

```
src/app/
├── config/     ← comece aqui em um projeto novo
├── core/       ← infraestrutura (auth, tema, layout, http, i18n, guards)
├── shared/     ← componentes, diretivas, pipes e modelos reutilizáveis
└── pages/      ← telas roteadas
```

Detalhes em [`docs/architecture.md`](./docs/architecture.md).

---

## Instalação

**Pré-requisitos:** Node.js 20.19+ e npm 10+.

```bash
npm install     # o script `prepare` já instala os hooks do Husky
```

### Os arquivos que você deve editar

| Arquivo                           | O que ajustar                                    |
| --------------------------------- | ------------------------------------------------ |
| `src/app/config/app.settings.ts`  | Nome, descrição, versão, logo, e-mail de suporte |
| `src/app/config/auth.config.ts`   | Issuer, clientId, redirect URIs, provedores      |
| `src/app/config/menu.config.ts`   | Itens da navegação lateral                       |
| `src/app/config/theme.config.ts`  | Modo padrão, densidade                           |
| `src/app/config/i18n.config.ts`   | Idiomas disponíveis                              |
| `src/environments/*.ts`           | URL da API e do provedor de identidade           |
| `src/styles/tokens/_palette.scss` | Rampa de cor da marca                            |
| `public/logo.svg`                 | Logo                                             |
| `public/i18n/*.json`              | Textos                                           |

---

## Execução

```bash
npm start        # http://localhost:4200
```

### Keycloak local

```bash
docker run -d --name keycloak -p 8081:8080 \
  -e KC_BOOTSTRAP_ADMIN_USERNAME=admin -e KC_BOOTSTRAP_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest start-dev
```

Use o realm institucional configurado em `src/environments/environment.ts` e o client publico `controle-ativos-frontend` com redirect `http://localhost:4200/auth/callback` e PKCE `S256`. Passo a passo em [`docs/development.md`](./docs/development.md).

> **HTTPS ou `localhost`.** A PKCE usa `crypto.subtle`, que só existe em contexto seguro. Acessar por IP de rede resulta em erro `insecure_context`.

---

## Build

```bash
npm run build        # producao -> dist/controle-ativos-frontend/browser
npm run build:dev    # sem otimização, para depurar
npm run analyze      # visualizador de bundle
```

### Scripts

| Comando                 | Descrição                        |
| ----------------------- | -------------------------------- |
| `npm start`             | Servidor de desenvolvimento      |
| `npm run build`         | Build de produção                |
| `npm run lint`          | ESLint em TypeScript e templates |
| `npm run format`        | Prettier                         |
| `npm test`              | Testes unitários                 |
| `npm run test:coverage` | Testes com cobertura             |
| `npm run clean`         | Remove artefatos de build        |
| `npm run analyze`       | Análise do bundle                |

---

## Temas

Três camadas: primitivos (as únicas cores em hex) → tokens semânticos (mudam por tema) → consumo (Tailwind, PrimeNG, SCSS).

```scss
// Errado
.card {
  background: #ffffff;
  padding: 24px;
}

// Certo
.card {
  background: var(--app-color-card);
  padding: var(--app-spacing-lg);
}
```

```ts
const theme = inject(ThemeService);
theme.resolved(); // 'light' | 'dark'
theme.toggle();
theme.followSystem();
```

O tema claro e o escuro são completos e recalibrados: no escuro, a marca usa um passo mais claro da rampa e as sombras são reajustadas, porque sombra pura não se percebe sobre fundo escuro. Um script inline no `index.html` aplica a preferência antes do Angular iniciar, eliminando o flash branco.

**Rebranding:** troque a rampa `--app-palette-brand-*` em `src/styles/tokens/_palette.scss` e o `public/logo.svg`. Nenhum componente precisa ser alterado.

Detalhes em [`docs/theme.md`](./docs/theme.md).

---

## Autenticação

Somente **Authorization Code + PKCE**. O fluxo implícito não está implementado, por decisão de segurança.

```ts
const auth = inject(AuthService);

auth.user(); // Signal<User> — modelo canônico, igual para todo provedor
auth.isAuthenticated();
auth.hasRole('admin');
auth.hasPermission('report:read');
await auth.login('keycloak');
await auth.logout();
```

O que já vem pronto: refresh silencioso agendado antes da expiração, refresh reativo em 401 com repetição da requisição (deduplicado entre chamadas concorrentes), restauração de sessão no boot antes da primeira rota, RP-Initiated Logout e proteção contra CSRF e replay (`state` de uso único, `nonce`, validação de `iss`/`aud`/`exp`).

Proteção de rotas:

```ts
{ path: 'admin',      canActivate: [authGuard, roleGuard('admin')], … }
{ path: 'relatorios', canActivate: [authGuard, permissionGuard('report:read')], … }
```

Nos templates:

```html
<button *appHasRole="'admin'">Excluir</button>
```

Detalhes em [`docs/authentication.md`](./docs/authentication.md).

---

## Como adicionar páginas

1. Copie `src/app/pages/example/` — ela já traz os quatro estados de tela (carregando, erro, vazio, dados), tabela, confirmação e toast.
2. Crie o serviço da feature usando `ApiService`.
3. Registre a rota como lazy, dentro dos filhos do shell:

```ts
{
  path: 'users',
  title: 'users.title',
  data: { breadcrumb: 'nav.users' },
  canActivate: [permissionGuard('user:read')],
  loadComponent: () => import('@pages/users/user-list.component').then((m) => m.UserListComponent),
}
```

4. Adicione o item em `config/menu.config.ts` e as chaves em `public/i18n/*.json`.

Receita completa em [`docs/creating-page.md`](./docs/creating-page.md).

---

## Como trocar o provedor OAuth

Um único arquivo: `src/app/config/auth.config.ts`.

```diff
- defaultProviderId: 'keycloak',
+ defaultProviderId: 'microsoft',

  providers: [
-   { id: 'keycloak',  kind: 'keycloak',  enabled: true,  … },
+   { id: 'keycloak',  kind: 'keycloak',  enabled: false, … },
-   { id: 'microsoft', kind: 'microsoft', enabled: false, … },
+   { id: 'microsoft', kind: 'microsoft', enabled: true,  … },
  ],
```

Nenhuma linha da aplicação muda. A tela de login se reorganiza sozinha: com um único provedor e sem formulário de senha, o separador "ou continue com" desaparece e resta apenas o botão correspondente.

O template já traz configurações prontas para Keycloak, Microsoft Entra ID, Azure AD B2C e Google.

---

## Como adicionar novos provedores

**Provedor OIDC padrão** — nenhuma linha de código, apenas uma entrada em `auth.config.ts`.

**Provedor com particularidades** — estenda `BaseOidcProvider` e sobrescreva o mapeamento de claims:

```ts
export class MeuIdpProvider extends BaseOidcProvider {
  protected get defaultClaimMapping(): ClaimMapping {
    return { id: 'sub', name: 'displayName', roles: ['app_roles'] };
  }
}
```

Registre-o sem tocar em `core/`:

```ts
{
  provide: AUTH_PROVIDER_FACTORY,
  multi: true,
  useValue: (config, deps) => (config.kind === 'meu-idp' ? new MeuIdpProvider(config, deps) : null),
}
```

**Autenticação que não é OIDC** (SAML, BFF com cookie, mTLS) — implemente a interface `AuthProvider` diretamente. Guards, diretivas, interceptors e telas continuam funcionando sem alteração.

Detalhes em [`docs/adding-provider.md`](./docs/adding-provider.md).

---

## Como publicar

```bash
npm ci && npm run build
```

A saida e estatica (`dist/controle-ativos-frontend/browser`). O servidor precisa de duas coisas:

1. fallback para `index.html` em rotas desconhecidas;
2. `index.html` sem cache, demais arquivos com cache longo (têm hash no nome).

Para promover **a mesma imagem** entre ambientes, injete a configuração na inicialização do contêiner em vez de gerar um build por ambiente — o padrão está documentado em [`docs/deployment.md`](./docs/deployment.md), junto com Nginx, Dockerfile, CSP e pipeline de CI.

---

## Qualidade

| Ferramenta  | Quando roda                       |
| ----------- | --------------------------------- |
| ESLint      | `npm run lint`, pre-commit        |
| Prettier    | `npm run format`, pre-commit      |
| lint-staged | pre-commit                        |
| commitlint  | commit-msg (Conventional Commits) |
| Husky       | instala os hooks no `npm install` |

Commits seguem Conventional Commits:

```
feat(auth): adiciona provedor Azure AD B2C
fix(layout): corrige drawer preso ao girar o dispositivo
```

---

## Documentação

| Documento                                               | Assunto                                 |
| ------------------------------------------------------- | --------------------------------------- |
| [`architecture.md`](./docs/architecture.md)             | Camadas, estado, HTTP, decisões         |
| [`authentication.md`](./docs/authentication.md)         | OIDC, PKCE, provedores, sessão          |
| [`theme.md`](./docs/theme.md)                           | Design tokens, claro/escuro, rebranding |
| [`layout.md`](./docs/layout.md)                         | Shell, navbar, sidebar, responsividade  |
| [`folder-structure.md`](./docs/folder-structure.md)     | Onde colocar cada arquivo               |
| [`development.md`](./docs/development.md)               | Ambiente, scripts, depuração            |
| [`deployment.md`](./docs/deployment.md)                 | Build, Docker, CSP, CI                  |
| [`coding-standards.md`](./docs/coding-standards.md)     | Convenções de código                    |
| [`adding-provider.md`](./docs/adding-provider.md)       | Novo provedor de autenticação           |
| [`creating-page.md`](./docs/creating-page.md)           | Nova página                             |
| [`creating-component.md`](./docs/creating-component.md) | Novo componente                         |

---

## Licença

Uso interno da empresa.
