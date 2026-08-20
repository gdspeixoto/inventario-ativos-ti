# Publicação

Como levar a aplicação a produção, com foco em **uma imagem promovida entre ambientes** em vez de um build por ambiente.

---

## 1. Build

```bash
npm ci
npm run build          # saida em dist/controle-ativos-frontend/browser
```

O build de produção aplica AOT, minificação, tree-shaking, `outputHashing: all` e substitui `environment.ts` por `environment.production.ts`.

### Orçamento de bundle

Definido em `angular.json`:

| Tipo                 | Aviso  | Erro   |
| -------------------- | ------ | ------ |
| Inicial              | 900 kB | 1.5 MB |
| Estilo de componente | 8 kB   | 16 kB  |

Se um build quebrar o orçamento, rode `npm run analyze` antes de simplesmente aumentar o limite.

---

## 2. Requisitos de servidor

A aplicação é uma SPA com roteamento por path. Duas exigências:

1. **Fallback para `index.html`** em qualquer rota desconhecida — sem isso, recarregar `/dashboard` devolve 404.
2. **`index.html` sem cache**; os demais arquivos têm hash no nome e podem ter cache longo.

### Nginx

```nginx
server {
  listen 80;
  root /usr/share/nginx/html;
  index index.html;

  # Arquivos com hash: cache agressivo.
  location ~* \.(js|css|woff2?|svg|png|jpg|ico)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
    try_files $uri =404;
  }

  # Bundles de tradução mudam sem hash: revalide sempre.
  location /i18n/ {
    add_header Cache-Control "no-cache";
    try_files $uri =404;
  }

  # index.html nunca em cache — é ele que aponta para os hashes atuais.
  location = /index.html {
    add_header Cache-Control "no-store, must-revalidate";
  }

  location / {
    try_files $uri $uri/ /index.html;
  }

  # Cabeçalhos de segurança.
  add_header X-Content-Type-Options "nosniff" always;
  add_header X-Frame-Options "DENY" always;
  add_header Referrer-Policy "strict-origin-when-cross-origin" always;
  add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
}
```

### Apache

```apache
<Directory "/var/www/html">
  RewriteEngine On
  RewriteCond %{REQUEST_FILENAME} !-f
  RewriteCond %{REQUEST_FILENAME} !-d
  RewriteRule ^ index.html [L]
</Directory>
```

---

## 3. Docker

```dockerfile
# ---------- build ----------
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# ---------- runtime ----------
FROM nginx:1.27-alpine
COPY --from=build /app/dist/controle-ativos-frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY docker-entrypoint.sh /docker-entrypoint.d/40-app-config.sh
RUN chmod +x /docker-entrypoint.d/40-app-config.sh
EXPOSE 80
```

```dockerignore
node_modules
dist
.angular
coverage
.git
```

---

## 4. Configuração em tempo de execução

`environment.ts` é compilado no bundle. Para promover **a mesma imagem** entre homologação e produção, sobrescreva os valores na inicialização do contêiner.

`docker-entrypoint.sh`:

```bash
#!/bin/sh
set -e

cat > /usr/share/nginx/html/app-config.js <<EOF
window.__APP_CONFIG__ = {
  apiBaseUrl: "${API_BASE_URL:-/api}",
  keycloakIssuer: "${KEYCLOAK_ISSUER}",
  keycloakClientId: "${KEYCLOAK_CLIENT_ID}",
  appOrigin: "${APP_ORIGIN}"
};
EOF
```

`index.html`, antes do bundle:

```html
<script src="app-config.js"></script>
```

E no `environment.production.ts`:

```ts
declare global {
  interface Window {
    __APP_CONFIG__?: Record<string, string>;
  }
}
const runtime = globalThis.window?.__APP_CONFIG__ ?? {};

export const environment = {
  production: true,
  apiBaseUrl: runtime['apiBaseUrl'] ?? '/api',
  keycloakIssuer: runtime['keycloakIssuer'] ?? '',
  keycloakClientId: runtime['keycloakClientId'] ?? '',
  appOrigin: runtime['appOrigin'] ?? '',
  logLevel: 'warn' as const,
  features: { darkMode: true, i18n: true, localCredentialsLogin: false },
};
```

> Nada aqui é segredo: tudo chega ao browser. `clientId` e `issuer` são públicos por definição no OIDC. **Nunca** injete client secret.

---

## 5. Content Security Policy

Recomendação inicial — ajuste os hosts:

```
default-src 'self';
script-src 'self' 'unsafe-inline';
style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
font-src 'self' https://fonts.gstatic.com;
img-src 'self' data: https:;
connect-src 'self' https://sso.example.com https://inventario.example.com;
frame-ancestors 'none';
base-uri 'self';
form-action 'self';
```

`connect-src` precisa listar o issuer do provedor de identidade (discovery, token e userinfo) e a API.

`'unsafe-inline'` em `script-src` é exigido pelo script anti-flash de tema no `index.html`; para eliminá-lo, mova-o para um arquivo próprio e use hash ou nonce.

Se a fonte for auto-hospedada, remova as entradas do Google Fonts.

---

## 6. Checklist de pré-produção

**Configuração**

- [ ] `environment.production.ts` com issuer, clientId e API corretos
- [ ] `redirectUri` e `postLogoutRedirectUri` cadastrados no provedor de identidade
- [ ] `credentials.enabled` conforme a política da empresa
- [ ] `appSettings.version` alinhado ao `package.json`

**Segurança**

- [ ] HTTPS obrigatório (PKCE exige contexto seguro)
- [ ] Cabeçalhos de segurança e CSP aplicados
- [ ] `logLevel: 'warn'` ou `'error'`
- [ ] Nenhum segredo no bundle (`grep -ri "secret\|password" dist/`)

**Servidor**

- [ ] Fallback SPA para `index.html`
- [ ] `index.html` sem cache; assets com hash em cache longo
- [ ] Gzip/Brotli habilitado

**Qualidade**

- [ ] `npm run lint` sem erros
- [ ] `npm test` verde
- [ ] `npm run build` dentro do orçamento

---

## 7. Pipeline de CI

```yaml
name: CI
on:
  push: { branches: [main, develop] }
  pull_request:

jobs:
  quality:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '22', cache: 'npm' }
      - run: npm ci
      - run: npm run format:check
      - run: npm run lint
      - run: npm test
      - run: npm run build
      - uses: actions/upload-artifact@v4
        with:
          name: dist
          path: dist/controle-ativos-frontend/browser
```

---

## 8. Subpath (`/meu-sistema/`)

Se a aplicação não ficar na raiz do domínio:

```bash
npm run build -- --base-href=/meu-sistema/ --deploy-url=/meu-sistema/
```

E ajuste `redirectUri` para `https://empresa.com.br/meu-sistema/auth/callback`, tanto no `auth.config.ts` quanto no cadastro do provedor.

---

## Leitura relacionada

- [`development.md`](./development.md) · [`authentication.md`](./authentication.md)
