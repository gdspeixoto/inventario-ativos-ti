using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Servidor fisico, virtual ou em nuvem.
///
/// O custo mensal e a soma de quatro componentes (infraestrutura, licenca,
/// suporte e backup). Manter os componentes separados permite responder
/// "quanto deste servidor e licenciamento?" sem depender de rateio posterior.
/// </summary>
public sealed class ServerAsset : Asset
{
    private ServerAsset()
    {
    }

    public ServerAsset(
        Guid id,
        Guid tenantId,
        OrganizationalUnit organizationalUnit,
        string name,
        string code,
        string hostname,
        ServerType serverType,
        ServerEnvironment environment,
        Money infrastructureMonthlyCost)
        : base(id, tenantId, AssetKind.Server, organizationalUnit, name, code, infrastructureMonthlyCost)
    {
        Hostname = Guard.MaxLength(Guard.NotEmpty(hostname, nameof(hostname)), 253, nameof(hostname));
        ServerType = serverType;
        Environment = environment;
        InfrastructureMonthlyCost = infrastructureMonthlyCost;
        LicenseMonthlyCost = Money.Zero(infrastructureMonthlyCost.Currency);
        SupportMonthlyCost = Money.Zero(infrastructureMonthlyCost.Currency);
        BackupMonthlyCost = Money.Zero(infrastructureMonthlyCost.Currency);

        RaiseTimeline(
            TimelineEventType.ServidorCriado,
            $"Servidor {name} criado",
            $"{serverType} em ambiente de {environment}, hostname {Hostname}. " +
            $"Custo mensal inicial: {MonthlyAmount}.");
    }

    protected override TimelineEntityType TimelineType => TimelineEntityType.Server;

    public string Hostname { get; private set; } = string.Empty;

    public ServerType ServerType { get; private set; }

    public ServerEnvironment Environment { get; private set; }

    public string? OperatingSystem { get; private set; }

    public string? OperatingSystemVersion { get; private set; }

    public string? Provider { get; private set; }

    public string? RegionOrDatacenter { get; private set; }

    public string? PrimaryIp { get; private set; }

    public int? CpuCores { get; private set; }

    public int? MemoryGb { get; private set; }

    public int? StorageGb { get; private set; }

    public string? BackupPolicy { get; private set; }

    public DateTimeOffset? LastBackupAt { get; private set; }

    public string? Sla { get; private set; }

    public string? MaintenanceWindow { get; private set; }

    public Money InfrastructureMonthlyCost { get; private set; }

    public Money LicenseMonthlyCost { get; private set; }

    public Money SupportMonthlyCost { get; private set; }

    public Money BackupMonthlyCost { get; private set; }

    public bool HasRecentBackup(int maxAgeInDays) =>
        LastBackupAt is not null &&
        LastBackupAt.Value >= DateTimeOffset.UtcNow.AddDays(-maxAgeInDays);

    public bool HasResponsible => TechnicalResponsibleUserId is not null;

    /// <summary>
    /// Corrige hostname, tipo e ambiente.
    ///
    /// Mudar o ambiente e uma correcao de cadastro quando o servidor foi
    /// registrado na coluna errada; se a maquina de fato migrou de ambiente,
    /// isso e um evento operacional e deve ser registrado como tal.
    /// </summary>
    public void CorrectIdentification(
        string hostname,
        ServerType serverType,
        ServerEnvironment environment,
        Guid? userId = null)
    {
        Hostname = Guard.MaxLength(
            Guard.NotEmpty(hostname, nameof(hostname)), 253, nameof(hostname));
        ServerType = serverType;
        Environment = environment;
        Touch(userId);
    }

    public void DefinePlatform(
        string? operatingSystem,
        string? operatingSystemVersion,
        string? provider,
        string? regionOrDatacenter,
        string? primaryIp,
        Guid? userId = null)
    {
        OperatingSystem = Guard.Optional(operatingSystem);
        OperatingSystemVersion = Guard.Optional(operatingSystemVersion);
        Provider = Guard.Optional(provider);
        RegionOrDatacenter = Guard.Optional(regionOrDatacenter);
        PrimaryIp = Guard.Optional(primaryIp);
        Touch(userId);
    }

    /// <summary>
    /// Redimensiona os recursos alocados. Alteracoes de capacidade quase sempre
    /// vem acompanhadas de mudanca de custo, entao o evento fica registrado
    /// mesmo quando o valor ainda nao foi atualizado.
    /// </summary>
    public void Resize(int? cpuCores, int? memoryGb, int? storageGb, string reason, Guid? userId = null)
    {
        var previous = $"{CpuCores ?? 0} vCPU / {MemoryGb ?? 0} GB RAM / {StorageGb ?? 0} GB";

        CpuCores = cpuCores is null ? CpuCores : Guard.NotNegative(cpuCores.Value, nameof(cpuCores));
        MemoryGb = memoryGb is null ? MemoryGb : Guard.NotNegative(memoryGb.Value, nameof(memoryGb));
        StorageGb = storageGb is null ? StorageGb : Guard.NotNegative(storageGb.Value, nameof(storageGb));
        Touch(userId);

        var current = $"{CpuCores ?? 0} vCPU / {MemoryGb ?? 0} GB RAM / {StorageGb ?? 0} GB";

        RaiseTimeline(
            TimelineEventType.ServidorAlterado,
            "Recursos redimensionados",
            $"Configuracao alterada de {previous} para {current}. " +
            $"Motivo: {Guard.NotEmpty(reason, nameof(reason))}");
    }

    public void DefineBackup(string? policy, DateTimeOffset? lastBackupAt, Guid? userId = null)
    {
        BackupPolicy = Guard.Optional(policy);
        LastBackupAt = lastBackupAt;
        Touch(userId);
    }

    public void DefineOperations(string? sla, string? maintenanceWindow, Guid? userId = null)
    {
        Sla = Guard.Optional(sla);
        MaintenanceWindow = Guard.Optional(maintenanceWindow);
        Touch(userId);
    }

    /// <summary>
    /// Atualiza a composicao de custos e recalcula o total mensal do servidor.
    /// </summary>
    public Money UpdateCostBreakdown(
        Money infrastructure,
        Money license,
        Money support,
        Money backup,
        string reason,
        Guid? userId = null)
    {
        var previousTotal = MonthlyAmount;

        InfrastructureMonthlyCost = infrastructure;
        LicenseMonthlyCost = license;
        SupportMonthlyCost = support;
        BackupMonthlyCost = backup;
        MonthlyAmount = infrastructure.Add(license).Add(support).Add(backup);
        Touch(userId);

        var impact = new Money(
            Math.Abs(MonthlyAmount.Amount - previousTotal.Amount),
            MonthlyAmount.Currency);

        RaiseTimeline(
            TimelineEventType.AlteracaoValor,
            "Composicao de custos atualizada",
            $"Custo mensal alterado de {previousTotal} para {MonthlyAmount} " +
            $"(impacto mensal de {impact}, anual de {impact.ToAnnual()}). " +
            $"Infra: {infrastructure} · Licenca: {license} · Suporte: {support} · Backup: {backup}. " +
            $"Motivo: {Guard.NotEmpty(reason, nameof(reason))}",
            previousTotal.Amount,
            MonthlyAmount.Amount);

        return impact;
    }

    public void Decommission(string reason, Guid? userId = null)
    {
        ChangeStatus(AssetStatus.Descontinuado, reason, userId);

        RaiseTimeline(
            TimelineEventType.ServidorDesativado,
            "Servidor desativado",
            $"Servidor {Hostname} desativado. Economia mensal estimada: {MonthlyAmount}. " +
            $"Motivo: {reason}");
    }

    /// <summary>
    /// Um reajuste direto no total do servidor recai sobre a linha de
    /// infraestrutura, que e o componente elastico na maioria dos casos.
    /// </summary>
    protected override void OnPriceAdjusted(Money newMonthlyAmount)
    {
        var others = LicenseMonthlyCost.Add(SupportMonthlyCost).Add(BackupMonthlyCost);
        var infrastructure = newMonthlyAmount.Amount - others.Amount;

        InfrastructureMonthlyCost = new Money(
            infrastructure < 0 ? 0m : infrastructure,
            newMonthlyAmount.Currency);
    }
}
