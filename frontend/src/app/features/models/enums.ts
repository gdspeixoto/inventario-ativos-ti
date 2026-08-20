/**
 * Enumerações espelhadas do backend.
 *
 * A API serializa enums como texto (`JsonStringEnumConverter`), por isso são
 * union types de string: o valor que chega no JSON é o próprio nome.
 */

export type AssetKind = 'License' | 'Server' | 'Service' | 'Equipment';

export type AssetStatus =
  | 'Ativo'
  | 'EmRenovacao'
  | 'Suspenso'
  | 'Cancelado'
  | 'Vencido'
  | 'EmManutencao'
  | 'Descontinuado'
  | 'PendenteRevisao';

export type BillingType = 'Mensal' | 'Anual' | 'Unico' | 'SobDemanda';

export type ServerType =
  'Fisico' | 'Virtual' | 'Cloud' | 'BancoDados' | 'Aplicacao' | 'Backup' | 'Container';

export type ServerEnvironment =
  'Producao' | 'Homologacao' | 'Desenvolvimento' | 'Testes' | 'Backup' | 'Contingencia';

export type ContractCategory =
  | 'Licenciamento'
  | 'Infraestrutura'
  | 'Cloud'
  | 'Suporte'
  | 'Seguranca'
  | 'Backup'
  | 'Telecom'
  | 'Consultoria'
  | 'Outros';

export type ContractStatus = 'Ativo' | 'EmRenovacao' | 'Encerrado' | 'Cancelado' | 'Suspenso';

export type SupplierStatus = 'Ativo' | 'Inativo' | 'Bloqueado';

export type AdjustmentIndex =
  'NegociacaoManual' | 'Ipca' | 'Igpm' | 'Dolar' | 'Euro' | 'PercentualFixo' | 'ReajusteContratual';

export type AdjustmentPeriodicity = 'Nenhuma' | 'Mensal' | 'Trimestral' | 'Semestral' | 'Anual';

export type PriceAdjustmentTargetType = 'Asset' | 'Contract' | 'SupplierService';

export type OrganizationalUnitType =
  'Matriz' | 'Filial' | 'Negocio' | 'AreaTecnologica' | 'UnidadeOperacional' | 'NucleoSuporte';

export type ManagementRole =
  'Gerente' | 'Coordenador' | 'ResponsavelTecnico' | 'ResponsavelFinanceiro';

export type TimelineEntityType =
  'Asset' | 'License' | 'Server' | 'Contract' | 'Supplier' | 'OrganizationalUnit';

export type TimelineEventType =
  | 'Criacao'
  | 'EdicaoCadastral'
  | 'AlteracaoValor'
  | 'ReajusteAplicado'
  | 'Renovacao'
  | 'Cancelamento'
  | 'AlteracaoQuantidade'
  | 'MudancaFornecedor'
  | 'MudancaResponsavel'
  | 'ServidorCriado'
  | 'ServidorAlterado'
  | 'ServidorDesativado'
  | 'DocumentoAnexado'
  | 'ObservacaoAdicionada'
  | 'AlertaGerado'
  | 'AlertaResolvido'
  | 'CustoRegistrado';

export type AlertType =
  | 'ContratoVencendo'
  | 'LicencaProximaRenovacao'
  | 'ServidorSemResponsavel'
  | 'ServidorSemBackupRecente'
  | 'ReajustePrevisto'
  | 'ReajusteAcimaDoLimite'
  | 'LicencaBaixaUtilizacao'
  | 'LicencaProximaDoLimite'
  | 'DocumentoObrigatorioAusente'
  | 'ItemSemCentroDeCusto'
  | 'CustoAnormal';

export type AlertPriority = 'Baixa' | 'Media' | 'Alta' | 'Critica';

export type AlertStatus = 'Aberto' | 'EmAnalise' | 'Resolvido' | 'Ignorado';

/** Rótulos em português para exibição, evitando `switch` espalhado nas telas. */
export const STATUS_LABELS: Record<string, string> = {
  Ativo: 'Ativo',
  EmRenovacao: 'Em renovação',
  Suspenso: 'Suspenso',
  Cancelado: 'Cancelado',
  Vencido: 'Vencido',
  EmManutencao: 'Em manutenção',
  Descontinuado: 'Descontinuado',
  PendenteRevisao: 'Pendente de revisão',
  Encerrado: 'Encerrado',
  Inativo: 'Inativo',
  Bloqueado: 'Bloqueado',
  Aberto: 'Aberto',
  EmAnalise: 'Em análise',
  Resolvido: 'Resolvido',
  Ignorado: 'Ignorado',
};

export const ENVIRONMENT_LABELS: Record<ServerEnvironment, string> = {
  Producao: 'Produção',
  Homologacao: 'Homologação',
  Desenvolvimento: 'Desenvolvimento',
  Testes: 'Testes',
  Backup: 'Backup',
  Contingencia: 'Contingência',
};

export const SERVER_TYPE_LABELS: Record<ServerType, string> = {
  Fisico: 'Físico',
  Virtual: 'Virtual',
  Cloud: 'Cloud',
  BancoDados: 'Banco de dados',
  Aplicacao: 'Aplicação',
  Backup: 'Backup',
  Container: 'Contêiner',
};

export const CONTRACT_CATEGORY_LABELS: Record<ContractCategory, string> = {
  Licenciamento: 'Licenciamento',
  Infraestrutura: 'Infraestrutura',
  Cloud: 'Cloud',
  Suporte: 'Suporte',
  Seguranca: 'Segurança',
  Backup: 'Backup',
  Telecom: 'Telecom',
  Consultoria: 'Consultoria',
  Outros: 'Outros',
};

export const ADJUSTMENT_INDEX_LABELS: Record<AdjustmentIndex, string> = {
  NegociacaoManual: 'Negociação manual',
  Ipca: 'IPCA',
  Igpm: 'IGP-M',
  Dolar: 'Dólar',
  Euro: 'Euro',
  PercentualFixo: 'Percentual fixo',
  ReajusteContratual: 'Reajuste contratual',
};

export const UNIT_TYPE_LABELS: Record<OrganizationalUnitType, string> = {
  Matriz: 'Matriz',
  Filial: 'Filial',
  Negocio: 'Negócio',
  AreaTecnologica: 'Área tecnológica',
  UnidadeOperacional: 'Unidade operacional',
  NucleoSuporte: 'Núcleo de suporte',
};

export const MANAGEMENT_ROLE_LABELS: Record<ManagementRole, string> = {
  Gerente: 'Gerente',
  Coordenador: 'Coordenador',
  ResponsavelTecnico: 'Responsável técnico',
  ResponsavelFinanceiro: 'Responsável financeiro',
};

export const EVENT_TYPE_LABELS: Record<TimelineEventType, string> = {
  Criacao: 'Criação',
  EdicaoCadastral: 'Edição cadastral',
  AlteracaoValor: 'Alteração de valor',
  ReajusteAplicado: 'Reajuste aplicado',
  Renovacao: 'Renovação',
  Cancelamento: 'Cancelamento',
  AlteracaoQuantidade: 'Alteração de quantidade',
  MudancaFornecedor: 'Mudança de fornecedor',
  MudancaResponsavel: 'Mudança de responsável',
  ServidorCriado: 'Servidor criado',
  ServidorAlterado: 'Servidor alterado',
  ServidorDesativado: 'Servidor desativado',
  DocumentoAnexado: 'Documento anexado',
  ObservacaoAdicionada: 'Observação adicionada',
  AlertaGerado: 'Alerta gerado',
  AlertaResolvido: 'Alerta resolvido',
  CustoRegistrado: 'Custo registrado',
};

export const ALERT_TYPE_LABELS: Record<AlertType, string> = {
  ContratoVencendo: 'Contrato vencendo',
  LicencaProximaRenovacao: 'Licença próxima da renovação',
  ServidorSemResponsavel: 'Servidor sem responsável',
  ServidorSemBackupRecente: 'Servidor sem backup recente',
  ReajustePrevisto: 'Reajuste previsto',
  ReajusteAcimaDoLimite: 'Reajuste acima do limite',
  LicencaBaixaUtilizacao: 'Licença ociosa',
  LicencaProximaDoLimite: 'Licença próxima do limite',
  DocumentoObrigatorioAusente: 'Documento ausente',
  ItemSemCentroDeCusto: 'Item sem centro de custo',
  CustoAnormal: 'Custo anormal',
};

export const ALERT_PRIORITY_LABELS: Record<AlertPriority, string> = {
  Baixa: 'Baixa',
  Media: 'Média',
  Alta: 'Alta',
  Critica: 'Crítica',
};

/** Severidade do PrimeNG correspondente a cada status/prioridade. */
export type Severity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';

export function statusSeverity(status: string): Severity {
  switch (status) {
    case 'Ativo':
    case 'Resolvido':
      return 'success';
    case 'EmRenovacao':
    case 'EmAnalise':
    case 'EmManutencao':
    case 'PendenteRevisao':
      return 'warn';
    case 'Vencido':
    case 'Cancelado':
    case 'Bloqueado':
    case 'Aberto':
      return 'danger';
    case 'Suspenso':
    case 'Descontinuado':
    case 'Encerrado':
    case 'Inativo':
    case 'Ignorado':
      return 'secondary';
    default:
      return 'info';
  }
}

export function prioritySeverity(priority: AlertPriority): Severity {
  switch (priority) {
    case 'Critica':
      return 'danger';
    case 'Alta':
      return 'warn';
    case 'Media':
      return 'info';
    default:
      return 'secondary';
  }
}

export function environmentSeverity(environment: ServerEnvironment): Severity {
  switch (environment) {
    case 'Producao':
      return 'danger';
    case 'Homologacao':
      return 'warn';
    case 'Contingencia':
    case 'Backup':
      return 'info';
    default:
      return 'secondary';
  }
}

/**
 * Faixa de utilização de licenças. Acima de 90% há risco de estouro; abaixo de
 * 50% há dinheiro parado — os dois extremos merecem destaque visual.
 */
export function utilizationSeverity(rate: number): Severity {
  if (rate >= 90) {
    return 'danger';
  }
  if (rate >= 70) {
    return 'success';
  }
  if (rate >= 50) {
    return 'warn';
  }
  return 'secondary';
}

export type CostType = 'Mensal' | 'Anual' | 'Pontual' | 'SobDemanda';

export const COST_TYPE_LABELS: Record<CostType, string> = {
  Mensal: 'Mensal',
  Anual: 'Anual',
  Pontual: 'Pontual',
  SobDemanda: 'Sob demanda',
};

export type CostCategory =
  | 'Licenciamento'
  | 'Infraestrutura'
  | 'Cloud'
  | 'Suporte'
  | 'Backup'
  | 'Seguranca'
  | 'Telecom'
  | 'Consultoria'
  | 'Outros';

export const COST_CATEGORY_LABELS: Record<CostCategory, string> = {
  Licenciamento: 'Licenciamento',
  Infraestrutura: 'Infraestrutura',
  Cloud: 'Cloud',
  Suporte: 'Suporte',
  Backup: 'Backup',
  Seguranca: 'Segurança',
  Telecom: 'Telecom',
  Consultoria: 'Consultoria',
  Outros: 'Outros',
};
