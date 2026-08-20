# Especificacao Do Produto

## Objetivo

Construir um sistema corporativo para controlar ativos do setor, incluindo licencas Microsoft, outros softwares, servidores, contratos, fornecedores, custos, reajustes, timeline, documentos e relatorios.

## Modulos

- Dashboard executivo e operacional.
- Licencas Microsoft e softwares.
- Servidores fisicos, virtuais e cloud.
- Contratos.
- Fornecedores.
- Custos e reajustes.
- Timeline geral.
- Alertas.
- Relatorios.
- Configuracoes.
- Organizacao e multitenancy.
- Usuarios, papeis e permissoes.

## Funcionalidades Obrigatorias

- Cadastrar, editar, consultar e inativar licencas.
- Controlar quantidade contratada, quantidade em uso e quantidade disponivel.
- Controlar valor unitario, mensal, anual, moeda, vigencia e reajuste.
- Cadastrar servidores com especificacoes tecnicas, custos e responsaveis.
- Vincular servidores a contratos, fornecedores, aplicacoes e unidades organizacionais.
- Cadastrar contratos com vigencia, vencimento, regras de reajuste e anexos.
- Cadastrar fornecedores e acompanhar custos consolidados por fornecedor.
- Registrar reajustes com valor anterior, novo valor, percentual, motivo, data efetiva, aprovador e documento.
- Criar timeline automatica para toda mudanca relevante.
- Gerar alertas de vencimento, reajuste, baixa utilizacao, custo anormal e pendencias cadastrais.
- Permitir relatorios por categoria, fornecedor, centro de custo, unidade, gerencia, area tecnologica e periodo.

## Regras Financeiras

- Valor anual deve ser calculado como valor mensal vezes 12 quando o item for mensal.
- Reajuste deve manter historico imutavel.
- Alteracao de quantidade deve recalcular totais e registrar timeline.
- Alteracao de valor deve exigir motivo e data efetiva.
- Reajustes acima do limite configurado devem gerar alerta.
- Valores devem armazenar moeda e casas decimais adequadas.
- O sistema deve permitir simulacao de reajuste futuro.

## Timeline

Todo item deve possuir timeline propria.

Eventos minimos:

- Criacao.
- Edicao cadastral.
- Alteracao financeira.
- Reajuste.
- Renovacao.
- Cancelamento.
- Alteracao de quantidade.
- Mudanca de fornecedor.
- Mudanca de responsavel.
- Upload de documento.
- Alerta criado.
- Alerta resolvido.

Cada evento deve conter:

- Tenant.
- Escopo organizacional.
- Item relacionado.
- Tipo do item.
- Tipo do evento.
- Data/hora UTC.
- Usuario responsavel.
- Dados anteriores quando aplicavel.
- Dados novos quando aplicavel.
- Impacto financeiro quando aplicavel.
- Observacao.
- Correlation ID.

## Perfis

- Administrador global do sistema.
- Administrador do tenant/unidade.
- Gestor de TI.
- Gerente de negocio/area/nucleo.
- Analista de TI.
- Financeiro.
- Auditor somente leitura.

## Nao Funcionais

- Multitenant desde o inicio.
- PostgreSQL.
- API versionada.
- Auditoria forte.
- Observabilidade.
- Testes automatizados.
- Seguranca por padrao.
- LGPD: minimizar dados pessoais e registrar acesso a dados sensiveis quando necessario.
