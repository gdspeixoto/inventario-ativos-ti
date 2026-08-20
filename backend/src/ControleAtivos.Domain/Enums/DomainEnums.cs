namespace ControleAtivos.Domain.Enums;

/// <summary>Papel exercido por um usuario sobre um no organizacional.</summary>
public enum ManagementRole
{
    Gerente = 0,
    Coordenador = 1,
    ResponsavelTecnico = 2,
    ResponsavelFinanceiro = 3,
}

/// <summary>Natureza do ativo controlado.</summary>
public enum AssetKind
{
    License = 0,
    Server = 1,
    Service = 2,
    Equipment = 3,
}

public enum AssetStatus
{
    Ativo = 0,
    EmRenovacao = 1,
    Suspenso = 2,
    Cancelado = 3,
    Vencido = 4,
    EmManutencao = 5,
    Descontinuado = 6,
    PendenteRevisao = 7,
}

/// <summary>Periodicidade de cobranca de um ativo ou contrato.</summary>
public enum BillingType
{
    Mensal = 0,
    Anual = 1,
    Unico = 2,
    SobDemanda = 3,
}

public enum ServerType
{
    Fisico = 0,
    Virtual = 1,
    Cloud = 2,
    BancoDados = 3,
    Aplicacao = 4,
    Backup = 5,
    Container = 6,
}

public enum ServerEnvironment
{
    Producao = 0,
    Homologacao = 1,
    Desenvolvimento = 2,
    Testes = 3,
    Backup = 4,
    Contingencia = 5,
}

public enum ContractCategory
{
    Licenciamento = 0,
    Infraestrutura = 1,
    Cloud = 2,
    Suporte = 3,
    Seguranca = 4,
    Backup = 5,
    Telecom = 6,
    Consultoria = 7,
    Outros = 8,
}

public enum ContractStatus
{
    Ativo = 0,
    EmRenovacao = 1,
    Encerrado = 2,
    Cancelado = 3,
    Suspenso = 4,
}

public enum SupplierStatus
{
    Ativo = 0,
    Inativo = 1,
    Bloqueado = 2,
}

/// <summary>Indice usado como base para um reajuste.</summary>
public enum AdjustmentIndex
{
    NegociacaoManual = 0,
    Ipca = 1,
    Igpm = 2,
    Dolar = 3,
    Euro = 4,
    PercentualFixo = 5,
    ReajusteContratual = 6,
}

public enum AdjustmentPeriodicity
{
    Nenhuma = 0,
    Mensal = 1,
    Trimestral = 2,
    Semestral = 3,
    Anual = 4,
}

/// <summary>Tipo de item alvo de um reajuste de preco.</summary>
public enum PriceAdjustmentTargetType
{
    Asset = 0,
    Contract = 1,
    SupplierService = 2,
}

public enum CostType
{
    Mensal = 0,
    Anual = 1,
    Pontual = 2,
    SobDemanda = 3,
}

public enum CostCategory
{
    Licenciamento = 0,
    Infraestrutura = 1,
    Cloud = 2,
    Suporte = 3,
    Backup = 4,
    Seguranca = 5,
    Telecom = 6,
    Consultoria = 7,
    Outros = 8,
}

/// <summary>Entidade a qual um evento de timeline ou alerta se refere.</summary>
public enum TimelineEntityType
{
    Asset = 0,
    License = 1,
    Server = 2,
    Contract = 3,
    Supplier = 4,
    OrganizationalUnit = 5,
}

public enum TimelineEventType
{
    Criacao = 0,
    EdicaoCadastral = 1,
    AlteracaoValor = 2,
    ReajusteAplicado = 3,
    Renovacao = 4,
    Cancelamento = 5,
    AlteracaoQuantidade = 6,
    MudancaFornecedor = 7,
    MudancaResponsavel = 8,
    ServidorCriado = 9,
    ServidorAlterado = 10,
    ServidorDesativado = 11,
    DocumentoAnexado = 12,
    ObservacaoAdicionada = 13,
    AlertaGerado = 14,
    AlertaResolvido = 15,
    CustoRegistrado = 16,
}

public enum AlertType
{
    ContratoVencendo = 0,
    LicencaProximaRenovacao = 1,
    ServidorSemResponsavel = 2,
    ServidorSemBackupRecente = 3,
    ReajustePrevisto = 4,
    ReajusteAcimaDoLimite = 5,
    LicencaBaixaUtilizacao = 6,
    LicencaProximaDoLimite = 7,
    DocumentoObrigatorioAusente = 8,
    ItemSemCentroDeCusto = 9,
    CustoAnormal = 10,
}

public enum AlertPriority
{
    Baixa = 0,
    Media = 1,
    Alta = 2,
    Critica = 3,
}

public enum AlertStatus
{
    Aberto = 0,
    EmAnalise = 1,
    Resolvido = 2,
    Ignorado = 3,
}
