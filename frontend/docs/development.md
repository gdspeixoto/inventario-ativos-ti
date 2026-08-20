# Desenvolvimento

Como preparar o ambiente, rodar o projeto e trabalhar no dia a dia.

---

## 1. Pré-requisitos

| Ferramenta | Versão mínima | Verificação     |
| ---------- | ------------- | --------------- |
| Node.js    | 20.19 (LTS)   | `node -v`       |
| npm        | 10            | `npm -v`        |
| Git        | 2.30          | `git --version` |

Um provedor de identidade acessível (Keycloak local ou corporativo) é necessário para o login OIDC. Enquanto ele não existir, use `credentials.enabled: true` com um backend simulado, ou desabilite temporariamente os guards.

---

## 2. Primeiro uso

```bash
git clone <url-do-repositorio> meu-sistema
cd meu-sistema
npm install          # o script `prepare` instala os hooks do Husky
npm start            # http://localhost:4200
```

### Personalizando o template

1. `package.json` → `name`, `version`, `description`
2. `src/app/config/app.settings.ts` → nome, descrição, versão, e-mail de suporte
3. `src/app/config/auth.config.ts` → issuer, clientId, redirectUri
4. `src/environments/*.ts` → URL da API e do provedor de identidade
5. `public/logo.svg` → logo da aplicação
6. `src/styles/tokens/_palette.scss` → rampa `--app-palette-brand-*`
7. `src/app/config/menu.config.ts` → itens de navegação
8. `public/i18n/*.json` → textos

---

## 3. Scripts

| Comando                 | O que faz                                                       |
| ----------------------- | --------------------------------------------------------------- |
| `npm start`             | Servidor de desenvolvimento com HMR, abre o browser             |
| `npm run start:prod`    | Serve com a configuração de produção (valida o build otimizado) |
| `npm run build`         | Build de produção em `dist/`                                    |
| `npm run build:dev`     | Build sem otimização, útil para depurar                         |
| `npm run watch`         | Build incremental em modo desenvolvimento                       |
| `npm test`              | Testes unitários (Vitest)                                       |
| `npm run test:watch`    | Testes em modo observação                                       |
| `npm run test:coverage` | Testes com relatório de cobertura                               |
| `npm run lint`          | ESLint em TypeScript e templates                                |
| `npm run lint:fix`      | ESLint com correção automática                                  |
| `npm run format`        | Prettier em `src/` e `docs/`                                    |
| `npm run format:check`  | Verifica formatação sem escrever (use na CI)                    |
| `npm run clean`         | Remove `dist`, `coverage`, `.angular`, `out-tsc`                |
| `npm run analyze`       | Build com source maps + visualizador de bundle                  |

---

## 4. Keycloak local

```bash
docker run -d --name keycloak -p 8081:8080 \
  -e KC_BOOTSTRAP_ADMIN_USERNAME=admin \
  -e KC_BOOTSTRAP_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest start-dev
```

Em `http://localhost:8081`:

1. crie o realm `corporate`;
2. crie o client `controle-ativos-frontend`:
   - Client authentication: **Off**
   - Standard flow: **On** · Implicit flow: **Off**
   - Valid redirect URIs: `http://localhost:4200/auth/callback`
   - Valid post logout redirect URIs: `http://localhost:4200/login`
   - Web origins: `http://localhost:4200`
   - Advanced → PKCE method: **S256**
3. crie um usuário com senha e atribua roles.

Os valores padrão de `environment.ts` já apontam para esse setup.

> **HTTPS é obrigatório fora de `localhost`.** A PKCE usa `crypto.subtle`, disponível apenas em contexto seguro. Acessar a aplicação por IP de rede (`http://192.168.x.x:4200`) resulta em `insecure_context`.

---

## 5. Backend em outra origem

Ajuste `apiBaseUrl` em `src/environments/environment.ts`. Se houver CORS, prefira o proxy do dev server a relaxar o backend:

```jsonc
// proxy.conf.json
{
  "/api": { "target": "http://localhost:8080", "secure": false, "changeOrigin": true },
}
```

```jsonc
// angular.json → architect.serve.options
"proxyConfig": "proxy.conf.json"
```

E use `apiBaseUrl: '/api'`.

---

## 6. Fluxo de trabalho

### Branches

```
main         produção
develop      integração
feature/…    novas funcionalidades
fix/…        correções
```

### Commits — Conventional Commits

O `commitlint` valida cada mensagem no hook `commit-msg`:

```
<tipo>(<escopo>): <assunto>

feat(auth): adiciona provedor Azure AD B2C
fix(layout): corrige drawer preso ao girar o dispositivo
docs(theme): documenta os tokens de elevação
refactor(core): extrai a normalização de claims
```

Tipos: `feat` `fix` `docs` `style` `refactor` `perf` `test` `build` `ci` `chore` `revert`
Escopos sugeridos: `auth` `core` `shared` `layout` `theme` `i18n` `pages` `config` `docs` `deps` `ci`

Assunto no imperativo, em minúsculas, sem ponto final, até 100 caracteres no cabeçalho.

### Hooks do Git

| Hook         | Ação                                      |
| ------------ | ----------------------------------------- |
| `pre-commit` | `lint-staged` → ESLint `--fix` + Prettier |
| `commit-msg` | `commitlint`                              |

Em emergência: `git commit --no-verify`. Deve ser exceção justificada.

---

## 7. Depuração

**Autenticação** — suba o log:

```ts
// environment.ts
logLevel: 'debug',
```

Inspecione `sessionStorage` (`app:auth.tokens`, `app:auth.request`) e a aba Network no `.well-known/openid-configuration` e no `POST /token`.

**Tema** — verifique o atributo `data-theme` no `<html>` e a chave `app:theme.preference` no `localStorage`.

**i18n** — chave não traduzida aparece literalmente na tela (`dashboard.welcome`). Confira o carregamento de `public/i18n/<locale>.json` na aba Network.

**Bundle grande** — `npm run analyze`.

---

## 8. Testes

Runner: **Vitest** com ambiente `jsdom`, via `@angular/build:unit-test`.

```bash
npm test
npm run test:watch
npm run test:coverage
```

Padrões adotados:

- lógica pura (`internal/jwt.ts`, `internal/claims.ts`) — teste direto, sem TestBed;
- stores — `TestBed.inject`, verificando os `computed` derivados;
- componentes — smoke test de renderização com os tokens de configuração providos (veja `login.component.spec.ts`);
- HTTP — `provideHttpClientTesting()`.

---

## 9. Problemas comuns

| Sintoma                                  | Solução                                                                |
| ---------------------------------------- | ---------------------------------------------------------------------- |
| `insecure_context` no login              | Use `localhost` ou HTTPS                                               |
| Estilos do PrimeNG sobrepondo o Tailwind | Confira a ordem de `@layer` em `tailwind.css` e o `cssLayer` do preset |
| `Cannot find module '@core/...'`         | Reinicie o servidor de TS do editor após mexer no `tsconfig.json`      |
| Alterações no SCSS não aparecem          | `npm run clean` e reinicie o dev server                                |
| Hooks do Git não rodam                   | `npm install` de novo (o script `prepare` instala o Husky)             |
| Componente não atualiza a tela           | O projeto é zoneless: o estado lido pela view precisa ser `signal`     |

---

## Leitura relacionada

- [`coding-standards.md`](./coding-standards.md)
- [`deployment.md`](./deployment.md)
