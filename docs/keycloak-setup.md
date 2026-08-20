# Configuração do Keycloak

> **Nota:** os usuarios do realm vem federados do AD e nao sao editaveis, o que
> impede gravar claim de tenant e papeis no Keycloak. Por isso a autorizacao
> passou a ser resolvida no proprio sistema — ver **`docs/controle-de-acesso.md`**.
> Este documento continua valendo para a parte de autenticacao (clients, PKCE,
> audience).

## Estado atual do realm (verificado em 18/08/2026)

Sondagem feita contra `https://sso.example.com/realms/inventario`:

| Item | Estado | Como foi verificado |
| --- | --- | --- |
| `controle-ativos-frontend` | **Já existe** | `/protocol/openid-connect/auth` responde 302 (segue para o login) |
| Redirect URIs do frontend | **Cadastradas** | `http://localhost:4200` e `.../auth/callback` → 302; URI não cadastrada → 400 |
| `controle-ativos-api` | **Não existe** | O endpoint de autorização responde "Client not found" |
| PKCE S256 | Suportado | `code_challenge_methods_supported: [plain, S256]` |

Falta, portanto, **apenas criar o client `controle-ativos-api`** e os mappers
descritos abaixo. Sem ele, o token sai com `aud: account` e a API devolve 401 em
toda requisição.

O tenant existente no banco é:

```
id   = 11111111-1111-1111-1111-111111111111
slug = contoso
```

Um desses dois valores precisa chegar no token (ver "Claim de tenant"), senão o
`TenantResolutionMiddleware` devolve **403 "Tenant nao identificado"** — mesmo
com o token válido e a audiência correta.

## Um ou dois clients?

**Recomendação: dois clients.** Um para o frontend, outro representando a API.

### Por que dois

O backend já valida a claim `aud` do token. Essa validação só tem sentido se a
audiência identificar **a API** — se o token for emitido para o próprio
frontend e a API aceitar esse mesmo valor, a checagem vira tautologia: qualquer
token válido do realm passa.

Além disso:

| Motivo | Consequência prática |
| --- | --- |
| Papéis pertencem ao recurso protegido | `controle-ativos-gestor-ti` é um papel *sobre a API*, não sobre a tela. Se amanhã existir um app mobile ou um painel novo, eles reaproveitam os mesmos papéis. |
| A migração para BFF muda o client do frontend | O frontend deixa de ser público e vira confidencial. Se os papéis e a audiência morassem nele, a migração mexeria em toda a autorização. |
| Integrações servidor-a-servidor virão | A sincronização de uso de licenças com o tenant Microsoft precisa de `client_credentials`. Esse client aponta para a mesma audiência da API, sem herdar nada do frontend. |

### Quando um client basta

Se o objetivo for apenas subir o MVP e não há previsão de outro consumidor, um
client funciona. O código suporta: basta apontar `Keycloak:Audience` e
`Keycloak:ClientId` para o mesmo id do frontend.

O custo aparece depois — ao adicionar o segundo consumidor, os papéis precisam
ser recriados e os tokens já emitidos deixam de ter a audiência esperada.

---

## Client 1: `controle-ativos-frontend`

Aplicação Angular. Cliente **público**, sem secret.

| Configuração | Valor |
| --- | --- |
| Client ID | `controle-ativos-frontend` |
| Client authentication | `Off` (público) |
| Standard flow | `On` |
| Direct access grants | `Off` |
| Implicit flow | `Off` |
| Service accounts | `Off` |
| PKCE Challenge Method | `S256` |
| Valid redirect URIs | `http://localhost:4200/auth/callback` · `https://inventario.example.com/auth/callback` |
| Valid post logout redirect URIs | `http://localhost:4200/login` · `https://inventario.example.com/login` |
| Web origins | `http://localhost:4200` · `https://inventario.example.com` |

Não use `*` em Web origins: isso libera qualquer origem a ler respostas do
token endpoint.

### Audience mapper (obrigatório)

Sem este passo, o Keycloak emite tokens com `aud: account` e **a API responde
401 em todas as requisições**. É o erro mais comum nesta configuração.

Em *Client scopes* → `controle-ativos-frontend-dedicated` → *Add mapper* → *By
configuration* → **Audience**:

| Campo | Valor |
| --- | --- |
| Name | `controle-ativos-api-audience` |
| Included Client Audience | `controle-ativos-api` |
| Add to access token | `On` |

---

## Client 2: `controle-ativos-api`

Representa a API. Não inicia login: existe para nomear a audiência e abrigar os
papéis.

| Configuração | Valor |
| --- | --- |
| Client ID | `controle-ativos-api` |
| Client authentication | `On` (confidencial) |
| Standard flow | `Off` |
| Direct access grants | `Off` |
| Service accounts | `Off` (ligue apenas se houver job server-to-server) |

Em versões antigas do Keycloak isso equivale ao antigo tipo `bearer-only`.

### Papéis (Roles do client)

Criar em *Roles* do client `controle-ativos-api`:

| Papel | Perfil |
| --- | --- |
| `controle-ativos-admin` | Administrador do tenant, acesso total |
| `controle-ativos-gestor-ti` | Gestor de TI, aprova mudanças financeiras |
| `controle-ativos-gerente` | Gerente de negócio, área ou núcleo |
| `controle-ativos-analista` | Analista de TI, operação do cadastro |
| `controle-ativos-financeiro` | Financeiro, custos e contratos |
| `controle-ativos-auditor` | Auditoria, somente leitura |

As permissões derivadas de cada papel estão em
`backend/src/ControleAtivos.Application/Abstractions/Permissions.cs`. O mapa vive
no backend de propósito: um realm mal configurado não deve conceder acesso
indevido nem bloquear quem deveria ter.

---

## Claim de tenant

O backend resolve o tenant nesta ordem:

1. claim `tenant_id` (UUID) no access token;
2. claim `tenant` (slug), resolvida contra a tabela `tenants`;
3. header `X-Tenant-Id` — **aceito somente quando a requisição vem do BFF**, que
   já validou a sessão. Em deployment sem BFF, o proxy de borda deve remover
   esse header das requisições externas.

O corpo da requisição nunca é considerado: aceitar o tenant enviado pelo cliente
anularia o isolamento entre organizações.

### Como emitir a claim

Em *Client scopes* → `controle-ativos-frontend-dedicated` → *Add mapper* → *By
configuration* → **User Attribute**:

| Campo | Valor |
| --- | --- |
| Name | `tenant` |
| User Attribute | `tenant` |
| Token Claim Name | `tenant` |
| Claim JSON Type | `String` |
| Add to access token | `On` |

Depois basta preencher o atributo `tenant` no usuário com o slug da organização
(ex.: `contoso`).

> **Decisão pendente.** Se a instituição usa grupos do Keycloak para representar
> a organização, um *Group Membership* mapper pode substituir o atributo. Confirme
> o padrão adotado antes de provisionar os usuários.

### Diagnóstico: 403 "Tenant nao identificado"

Este 403 **não é falta de permissão**. Ele vem do
`TenantResolutionMiddleware`, que roda *antes* da autorização
(`Program.cs:80` vs `:81`). O token está válido e a audiência correta — o
middleware apenas não achou a claim de tenant e nem chegou a avaliar papéis.

Distinção rápida pelo corpo da resposta:

| Corpo do 403 | Causa | Correção |
| --- | --- | --- |
| `{"title": "Tenant nao identificado", ...}` | Falta a claim de tenant | Criar o mapper abaixo |
| Vazio ou ProblemDetails genérico | Falta o papel necessário | Atribuir o papel no client da API |

Adicionar papéis não resolve o primeiro caso: o middleware retorna antes.

### Passo a passo do mapper de tenant

No client **`controle-ativos-frontend`** → *Client scopes* →
`controle-ativos-frontend-dedicated` → *Add mapper* → *By configuration* →
**User Attribute**:

| Campo | Valor |
| --- | --- |
| Name | `tenant-id` |
| User Attribute | `tenant_id` |
| Token Claim Name | `tenant_id` |
| Claim JSON Type | `String` |
| Add to access token | **ON** |

Depois, em *Users* → seu usuário → *Attributes*, adicione:

```
tenant_id = 11111111-1111-1111-1111-111111111111
```

Alternativa equivalente: mapear o atributo `tenant` com o valor `contoso` (o
slug). O middleware aceita os dois — tenta `tenant_id` primeiro e, se ausente,
resolve o slug consultando a tabela `tenants`.

**Faça logout e login de novo.** As claims são gravadas no token na emissão;
um token já emitido não ganha o atributo retroativamente.

### Acesso total ("ver tudo")

O papel `controle-ativos-admin` concede as 16 permissões de `Permissions.All`
(ver `Permissions.cs:71`). Não existe escopo por unidade organizacional nas
consultas — o isolamento é por tenant, via filtro global do EF Core
(`ControleAtivosDbContext.cs:138`).

Portanto, com a claim de tenant **mais** o papel `controle-ativos-admin`, você
enxerga todo o acervo do tenant `contoso`. Não é preciso configurar mais nada.

Um detalhe que evita confusão: o papel precisa estar no client
**`controle-ativos-api`**, não no do frontend. O backend lê
`resource_access.controle-ativos-api.roles`, conforme `ClientId` no
appsettings. Papel atribuído no client errado é ignorado — e o sintoma seria um
403 **sem** a mensagem de tenant.

---

## Configuração da aplicação

### Backend

`backend/src/ControleAtivos.Api/appsettings.Development.json`:

```json
{
  "Keycloak": {
    "Authority": "https://sso.example.com/realms/inventario",
    "Audience": "controle-ativos-api",
    "ClientId": "controle-ativos-api",
    "RequireHttpsMetadata": true
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

`ClientId` é usado para ler `resource_access.<clientId>.roles`. Deve apontar
para o client onde os papéis foram criados — o da API.

### Frontend

`frontend/src/environments/environment.ts`:

```ts
keycloakIssuer: 'https://sso.example.com/realms/inventario',
keycloakClientId: 'controle-ativos-frontend',
apiBaseUrl: 'http://localhost:5169/api',
```

---

## Verificação

1. Faça login em `http://localhost:4200`.
2. Copie o access token (DevTools → Network → requisição ao token endpoint).
3. Cole em <https://jwt.io> e confira:
   - `aud` contém `controle-ativos-api`;
   - `resource_access.controle-ativos-api.roles` traz o papel esperado;
   - existe `tenant` ou `tenant_id`.
4. Chame a API com o token:

```bash
curl -H "Authorization: Bearer <token>" http://localhost:5169/api/v1/dashboard/summary
```

| Resposta | Causa provável |
| --- | --- |
| `200` | Configuração correta |
| `401` | `aud` sem `controle-ativos-api` — falta o audience mapper |
| `403` com "Tenant nao identificado" | Falta a claim de tenant no usuário |
| `403` sem corpo | Papel ausente ou não mapeado em `resource_access` |

---

## Evolução para BFF

Ao adotar o BFF (ver `bff-token-strategy.md`), muda **apenas o client do
frontend**:

| Antes | Depois |
| --- | --- |
| Público, PKCE no browser | Confidencial, com secret no servidor |
| Redirect para `/auth/callback` do Angular | Redirect para `/bff/callback` do backend |

O client `controle-ativos-api`, os papéis e o audience mapper permanecem
inalterados — que é justamente o motivo de separá-los desde agora.

---

## Listagem de usuários (pendente de configuração)

O autocomplete de responsáveis — em ativos, contratos e vínculos de gestão —
precisa consultar os usuários do realm. Isso exige a **Admin REST API**, que não
aceita o token do usuário logado: é preciso um *service account*.

### Estado verificado (19/08/2026)

```
GET /admin/realms/inventario/users        → 401 (exige token de admin)
POST /token grant_type=client_credentials
     client_id=controle-ativos-api          → unauthorized_client
```

Ou seja: o client existe, mas **não tem service account habilitado**. Sem isso, a
funcionalidade não pode ser implementada.

### O que configurar

No client `controle-ativos-api`:

| Passo | Onde |
| --- | --- |
| 1. Ligar `Service accounts roles` | Settings → Capability config |
| 2. Copiar o `Client secret` | Credentials |
| 3. Conceder a role `view-users` | Service accounts roles → Assign role → Filter by clients → `realm-management` |

A role `view-users` é a permissão mínima: permite listar e buscar usuários, sem
autorizar criação, alteração ou exclusão. Não conceda `manage-users` — o sistema
não altera identidades, apenas as consulta.

Depois, no backend:

```json
"Keycloak": {
  "ClientSecret": "<via variável de ambiente Keycloak__ClientSecret>"
}
```

### Enquanto isso

O sistema espelha na tabela `users` todo mundo que já entrou pelo menos uma vez
(o `TenantResolutionMiddleware` provisiona no primeiro acesso). Essa tabela é
utilizável como fonte imediata, com uma limitação honesta: **não enxerga quem
nunca fez login**. Para atribuir um responsável que ainda não acessou o sistema,
a integração com a Admin API é necessária.
