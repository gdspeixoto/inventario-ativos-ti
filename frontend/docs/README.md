# Documentação

Índice da documentação do template.

## Fundamentos

| Documento                                      | Conteúdo                                                         |
| ---------------------------------------------- | ---------------------------------------------------------------- |
| [`architecture.md`](./architecture.md)         | Camadas, inversão de dependência, estado, HTTP, decisões tomadas |
| [`folder-structure.md`](./folder-structure.md) | Onde colocar cada arquivo, convenções de nome, aliases           |
| [`coding-standards.md`](./coding-standards.md) | TypeScript, componentes, signals, estilos, acessibilidade, i18n  |

## Subsistemas

| Documento                                  | Conteúdo                                               |
| ------------------------------------------ | ------------------------------------------------------ |
| [`authentication.md`](./authentication.md) | OIDC + PKCE, provedores, guards, sessão, tokens, erros |
| [`theme.md`](./theme.md)                   | Design tokens, tema claro/escuro, PrimeNG, rebranding  |
| [`layout.md`](./layout.md)                 | Shell, navbar, sidebar, breadcrumb, responsividade     |

## Guias práticos

| Documento                                          | Conteúdo                                               |
| -------------------------------------------------- | ------------------------------------------------------ |
| [`development.md`](./development.md)               | Ambiente, scripts, Keycloak local, commits, depuração  |
| [`creating-page.md`](./creating-page.md)           | Passo a passo de uma nova tela                         |
| [`creating-component.md`](./creating-component.md) | Passo a passo de um novo componente                    |
| [`adding-provider.md`](./adding-provider.md)       | Novo provedor de autenticação                          |
| [`deployment.md`](./deployment.md)                 | Build, Nginx, Docker, configuração em runtime, CSP, CI |

## Por onde começar

**Vou criar um sistema novo a partir do template**
[`development.md`](./development.md) → [`architecture.md`](./architecture.md) → [`authentication.md`](./authentication.md)

**Entrei em um projeto que usa o template**
[`architecture.md`](./architecture.md) → [`folder-structure.md`](./folder-structure.md) → [`coding-standards.md`](./coding-standards.md)

**Preciso entregar uma tela**
[`creating-page.md`](./creating-page.md) → [`creating-component.md`](./creating-component.md)

**Vou publicar em produção**
[`deployment.md`](./deployment.md)
