# Specs De Desenvolvimento

Esta pasta organiza o desenvolvimento do Inventario de Ativos de TI em tarefas rastreaveis para agentes de IA.

## Como Usar

Antes de iniciar qualquer trabalho:

1. Leia `../README.md`.
2. Leia `../.agent/agent-instructions.md`.
3. Leia `00-project-state.md`.
4. Leia `01-master-task-list.md`.
5. Escolha a proxima tarefa com status `PENDING`.
6. Marque como `IN_PROGRESS` antes de alterar arquivos.
7. Ao concluir, marque como `DONE`, informe arquivos alterados e atualize `00-project-state.md`.

## Status Permitidos

- `PENDING`: ainda nao iniciado.
- `IN_PROGRESS`: em execucao por um agente.
- `BLOCKED`: bloqueado por dependencia, decisao ou erro.
- `DONE`: concluido e validado.
- `SKIPPED`: nao sera feito agora, com justificativa.

## Regra De Continuidade

Se um modelo perder contexto ou acabar tokens, o proximo agente deve conseguir continuar lendo:

- `00-project-state.md`
- `01-master-task-list.md`
- A spec especifica da area em andamento.

Todo agente deve deixar rastros claros do que fez, do que falta e de como validar.
