# Requisitos Detalhados

## Licencas

### Cadastro

- Nome.
- Produto.
- Fabricante.
- Plano.
- Tipo de cobranca.
- Quantidade contratada.
- Quantidade em uso.
- Valor unitario.
- Valor mensal.
- Valor anual.
- Fornecedor.
- Contrato.
- Centro de custo.
- Unidade organizacional.
- Responsavel tecnico.
- Responsavel financeiro.
- Data de contratacao.
- Data de renovacao.
- Status.

### Regras

- Quantidade disponivel = contratada - em uso.
- Valor mensal = quantidade contratada * valor unitario para licencas por usuario.
- Alteracao de quantidade gera timeline.
- Alteracao de valor gera `PriceAdjustment`.
- Renovacao gera timeline e pode atualizar proxima data de reajuste.

## Servidores

### Cadastro

- Nome.
- Hostname.
- Tipo.
- Ambiente.
- Sistema operacional.
- Provedor/datacenter.
- IP principal.
- CPU.
- Memoria.
- Armazenamento.
- Politica de backup.
- Ultimo backup.
- Custos por componente.
- Responsavel.
- Status.

### Regras

- Custo total mensal = infraestrutura + licenca + suporte + backup.
- Servidor sem backup recente gera alerta.
- Servidor sem responsavel gera pendencia.
- Desativacao exige justificativa.

## Contratos

### Cadastro

- Numero.
- Nome.
- Fornecedor.
- Categoria.
- Valor mensal.
- Valor anual.
- Inicio.
- Vencimento.
- Indice de reajuste.
- Periodicidade.
- Responsavel.
- Itens vinculados.
- Documentos.

### Regras

- Vencimento proximo gera alerta conforme configuracao.
- Renovacao deve criar evento de timeline.
- Reajuste deve criar historico financeiro.

## Organizacao

### Regras

- Um no organizacional pode ter um pai.
- Um gerente pode ter varias atribuicoes.
- Um ativo pertence a um no organizacional principal.
- Permissoes podem ser herdadas para descendentes do no.
