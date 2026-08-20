# Controle de acesso

## O problema

Os usuários vêm federados do AD para o Keycloak e **não podem ser editados lá**.
Isso inviabiliza as duas coisas que o sistema esperava do provedor de identidade:

| O que faltava | Sintoma |
| --- | --- |
| Claim de tenant | 403 `"Tenant nao identificado"` em toda requisição |
| Papel `controle-ativos-admin` | 403 sem mensagem, mesmo com token válido |

## A separação adotada

**O provedor corporativo diz quem você é. O sistema diz o que você pode.**

Autenticação continua no Keycloak/AD — com MFA e gestão centralizada de
identidade, que não faz sentido reimplementar. A autorização passou a ser
resolvida localmente, porque é a parte que a instituição precisa administrar e
o AD não oferece.

Os papéis do token e os do banco **se somam**, e nenhum revoga o outro: quem
administra o Keycloak e quem administra o produto são equipes diferentes, e o
silêncio de uma não deve apagar a decisão da outra
(`TenantResolutionMiddleware.ApplyLocalRolesAsync`).

---

## Configuração

### Tenant padrão

```json
"Access": {
  "DefaultTenantSlug": "contoso"
}
```

Aplicado apenas quando o token não traz `tenant_id` nem `tenant`. A precedência
segue sendo claim → header → padrão; o corpo da requisição nunca é consultado.

> **Só use em instalação de organização única.** Com vários tenants, um padrão
> silencioso coloca o usuário na organização errada e mistura dados. Nesse
> cenário, deixe vazio e exija a claim.

### Primeiro administrador

```json
"Access": {
  "BootstrapAdmins": ["admin@example.com"]
}
```

Resolve o problema do ovo e da galinha: sem isso, ninguém teria permissão para
conceder permissão a ninguém. No primeiro acesso, o usuário listado recebe
`controle-ativos-admin` **gravado no banco** — não apenas como claim da
requisição —, então a concessão aparece nas telas de administração e pode ser
revogada normalmente depois.

Esvazie a lista assim que a equipe estiver configurada. É um atalho de
inicialização, não um mecanismo de administração.

### Acesso total

`controle-ativos-admin` concede as 16 permissões de `Permissions.All`
(`Permissions.cs:71`). Não existe escopo por unidade organizacional nas
consultas — o isolamento é por tenant, via filtro global do EF Core
(`ControleAtivosDbContext.cs:138`). Com esse papel, você enxerga todo o acervo
do tenant.

---

## Login local de emergência (break-glass)

### Para que serve — e para que não serve

Existe para **um** cenário: o SSO indisponível e ainda ser preciso entrar. Não é
um caminho alternativo para o dia a dia; para isso existe o login corporativo,
que traz MFA e gestão central de identidade que este caminho não tem.

### Travas de segurança

| Trava | Implementação |
| --- | --- |
| Hash de senha | PBKDF2-HMAC-SHA256, 210.000 iterações, salt por senha, comparação em tempo fixo |
| Bloqueio | 5 tentativas → 15 min. **Temporário**, não permanente: bloqueio eterno transformaria força bruta em negação de serviço contra o próprio administrador |
| Expiração | A credencial pode expirar sozinha — conta de emergência esquecida é porta aberta que ninguém está olhando |
| Enumeração de contas | Usuário inexistente e senha errada devolvem a **mesma** resposta, e o hash é derivado mesmo quando o usuário não existe, para igualar o tempo |
| Endpoint desligado | Responde **404**, não 403: caminho desativado não precisa anunciar que existe |
| Produção | Exige `Enabled` **e** `AllowedInProduction`, para que uma configuração copiada do ambiente de dev não abra o caminho sem querer |
| Chave fraca | O login não sobe se a chave de assinatura tiver menos de 32 bytes |
| Rastreabilidade | O token carrega `amr=pwd` e emissor próprio (`controle-ativos-local`), separável do corporativo numa auditoria. Todo login local é registrado em nível `Warning` |

### Configuração

```json
"Access": {
  "LocalLogin": {
    "Enabled": true,
    "AllowedInProduction": false,
    "TokenLifetimeMinutes": 60,
    "SigningKey": "<32+ bytes, via variavel de ambiente ou cofre>",
    "SeedCredentials": [
      {
        "Username": "admin.local",
        "Password": "<senha inicial>",
        "DisplayName": "Administrador Local",
        "Email": "admin.local@example.com",
        "ExpiresInDays": 0
      }
    ]
  }
}
```

> **`SigningKey` não deve ser versionada.** Use variável de ambiente
> (`Access__LocalLogin__SigningKey`) ou cofre de segredos. O mesmo vale para
> `Password` — o seeder é idempotente e **nunca** sobrescreve a senha de uma
> credencial existente, então você pode remover a entrada depois do primeiro
> provisionamento.

### Dois emissores, sem afrouxar validação

Com o login local ativo há dois emissores válidos. Um *policy scheme* lê o `iss`
do token e encaminha para o validador certo, em vez de relaxar a validação de um
para aceitar o outro. O token local só aceita HMAC-SHA256 — aceitar outro
algoritmo abriria caminho para confusão de algoritmo.

Ler o `iss` antes de validar é seguro: serve apenas para escolher o validador, e
um token que mente sobre o emissor simplesmente falha na validação seguinte.

---

## Diagnóstico de 403

O `TenantResolutionMiddleware` roda **antes** da autorização (`Program.cs:80` vs
`:81`), então o tipo de 403 se distingue pelo corpo da resposta:

| Corpo | Causa | Correção |
| --- | --- | --- |
| `{"title": "Tenant nao identificado", ...}` | Sem claim de tenant | `Access:DefaultTenantSlug` ou mapper no Keycloak |
| Vazio / ProblemDetails genérico | Sem o papel necessário | Conceder o papel (banco ou `BootstrapAdmins`) |

Adicionar papéis não resolve o primeiro caso: o middleware retorna antes de
avaliar autorização.

---

## Operação

Verificar identidade e permissões efetivas do chamador:

```
GET /api/v1/auth/me
```

Devolve papéis e permissões já combinando token e concessões locais. O frontend
usa isso para decidir o que exibir — a decisão de acesso continua no servidor.

### Rotina recomendada

1. Manter `BootstrapAdmins` vazio depois da configuração inicial.
2. Trocar a senha da credencial de emergência após o primeiro uso.
3. Definir `ExpiresInDays` para credenciais temporárias.
4. Revisar `local_credentials` e `user_role_assignments` periodicamente — a
   revogação preserva o histórico, então "quem teve acesso e quando" continua
   legível numa auditoria.
