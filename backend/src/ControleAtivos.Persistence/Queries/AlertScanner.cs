using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Features.Alerts;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

/// <summary>
/// Varre a base procurando pendencias e cria alertas.
///
/// A deduplicacao acontece por <c>DeduplicationKey</c>: enquanto o alerta de um
/// problema continuar aberto, execucoes seguintes nao criam duplicatas. Isso
/// permite rodar a varredura com frequencia sem poluir a caixa de pendencias.
/// </summary>
public sealed class AlertScanner(
    ControleAtivosDbContext context,
    ITenantContext tenantContext) : IAlertScanner
{
    public async Task<AlertScanResultDto> ScanAsync(
        AlertScanSettings settings,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var existingKeys = (await context.Alerts
            .AsNoTracking()
            .Where(a => a.Status == AlertStatus.Aberto || a.Status == AlertStatus.EmAnalise)
            .Select(a => a.DeduplicationKey)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var created = new List<Alert>();
        var alreadyOpen = 0;

        void TryCreate(
            AlertType type,
            Guid entityId,
            AlertPriority priority,
            TimelineEntityType entityType,
            Guid unitId,
            string title,
            string description,
            string action,
            DateOnly? dueDate)
        {
            var key = Alert.BuildDeduplicationKey(type, entityId);

            if (!existingKeys.Add(key))
            {
                alreadyOpen++;
                return;
            }

            created.Add(new Alert(
                Guid.NewGuid(),
                tenantContext.TenantId,
                type,
                priority,
                entityType,
                entityId,
                unitId,
                title,
                description,
                action,
                dueDate));
        }

        // --- Contratos vencendo ---
        var contracts = await ScopedContracts(scope)
            .Where(c => c.Status == ContractStatus.Ativo)
            .Where(c => c.EndDate >= today && c.EndDate <= today.AddDays(settings.ContractExpirationDays))
            .Select(c => new
            {
                c.Id,
                c.Number,
                c.Name,
                c.EndDate,
                c.OrganizationalUnitId,
                SupplierName = c.Supplier.Name,
                Amount = c.MonthlyAmount.Amount,
            })
            .ToListAsync(cancellationToken);

        foreach (var contract in contracts)
        {
            var days = contract.EndDate.DayNumber - today.DayNumber;

            TryCreate(
                AlertType.ContratoVencendo,
                contract.Id,
                days <= 15 ? AlertPriority.Critica : days <= 30 ? AlertPriority.Alta : AlertPriority.Media,
                TimelineEntityType.Contract,
                contract.OrganizationalUnitId,
                $"Contrato {contract.Number} vence em {days} dia(s)",
                $"O contrato '{contract.Name}' com {contract.SupplierName} encerra em " +
                $"{contract.EndDate:dd/MM/yyyy}. Valor mensal atual: R$ {contract.Amount:N2}.",
                "Avaliar renovacao, renegociacao ou encerramento junto ao fornecedor.",
                contract.EndDate);
        }

        // --- Licencas proximas da renovacao ---
        var renewals = await ScopedAssets(scope)
            .OfType<LicenseAsset>()
            .Where(l => l.Status == AssetStatus.Ativo)
            .Where(l => l.RenewalDate != null
                && l.RenewalDate >= today
                && l.RenewalDate <= today.AddDays(settings.LicenseRenewalDays))
            .Select(l => new { l.Id, l.Name, l.RenewalDate, l.OrganizationalUnitId })
            .ToListAsync(cancellationToken);

        foreach (var license in renewals)
        {
            var days = license.RenewalDate!.Value.DayNumber - today.DayNumber;

            TryCreate(
                AlertType.LicencaProximaRenovacao,
                license.Id,
                days <= 15 ? AlertPriority.Alta : AlertPriority.Media,
                TimelineEntityType.License,
                license.OrganizationalUnitId,
                $"Licenca {license.Name} renova em {days} dia(s)",
                $"A licenca '{license.Name}' tem renovacao prevista para " +
                $"{license.RenewalDate:dd/MM/yyyy}.",
                "Revisar a quantidade contratada antes da renovacao automatica.",
                license.RenewalDate);
        }

        // --- Licencas ociosas e proximas do limite ---
        var utilization = await ScopedAssets(scope)
            .OfType<LicenseAsset>()
            .Where(l => l.Status == AssetStatus.Ativo && l.ContractedQuantity > 0)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.ContractedQuantity,
                l.UsedQuantity,
                l.OrganizationalUnitId,
                Amount = l.MonthlyAmount.Amount,
                Rate = (decimal)l.UsedQuantity / l.ContractedQuantity * 100m,
            })
            .ToListAsync(cancellationToken);

        foreach (var license in utilization)
        {
            if (license.Rate < settings.IdleUtilizationBelow)
            {
                var idle = license.ContractedQuantity - license.UsedQuantity;
                var unitPrice = license.Amount / license.ContractedQuantity;

                TryCreate(
                    AlertType.LicencaBaixaUtilizacao,
                    license.Id,
                    AlertPriority.Media,
                    TimelineEntityType.License,
                    license.OrganizationalUnitId,
                    $"Licenca {license.Name} com {license.Rate:N1}% de utilizacao",
                    $"{license.UsedQuantity} de {license.ContractedQuantity} licencas em uso. " +
                    $"Ha {idle} licenca(s) ociosa(s), equivalentes a R$ {idle * unitPrice:N2} por mes.",
                    "Avaliar reducao da quantidade contratada na proxima renovacao.",
                    null);
            }
            else if (license.Rate > settings.HighUtilizationAbove)
            {
                TryCreate(
                    AlertType.LicencaProximaDoLimite,
                    license.Id,
                    AlertPriority.Alta,
                    TimelineEntityType.License,
                    license.OrganizationalUnitId,
                    $"Licenca {license.Name} com {license.Rate:N1}% de utilizacao",
                    $"{license.UsedQuantity} de {license.ContractedQuantity} licencas em uso. " +
                    "O limite contratado esta proximo de ser atingido.",
                    "Planejar a ampliacao da quantidade contratada.",
                    null);
            }
        }

        // --- Servidores sem responsavel ou sem backup recente ---
        var backupLimit = DateTimeOffset.UtcNow.AddDays(-settings.BackupMaxAgeDays);

        var servers = await ScopedAssets(scope)
            .OfType<ServerAsset>()
            .Where(s => s.Status == AssetStatus.Ativo)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Hostname,
                s.Environment,
                s.LastBackupAt,
                s.TechnicalResponsibleUserId,
                s.OrganizationalUnitId,
            })
            .ToListAsync(cancellationToken);

        foreach (var server in servers)
        {
            if (server.TechnicalResponsibleUserId is null)
            {
                TryCreate(
                    AlertType.ServidorSemResponsavel,
                    server.Id,
                    AlertPriority.Media,
                    TimelineEntityType.Server,
                    server.OrganizationalUnitId,
                    $"Servidor {server.Name} sem responsavel tecnico",
                    $"O servidor '{server.Hostname}' ({server.Environment}) nao possui responsavel " +
                    "tecnico definido.",
                    "Atribuir um responsavel tecnico ao servidor.",
                    null);
            }

            if (server.LastBackupAt is null || server.LastBackupAt < backupLimit)
            {
                // Falha de backup em producao e mais grave que nos demais ambientes.
                var priority = server.Environment == ServerEnvironment.Producao
                    ? AlertPriority.Critica
                    : AlertPriority.Alta;

                TryCreate(
                    AlertType.ServidorSemBackupRecente,
                    server.Id,
                    priority,
                    TimelineEntityType.Server,
                    server.OrganizationalUnitId,
                    $"Servidor {server.Name} sem backup recente",
                    server.LastBackupAt is null
                        ? $"O servidor '{server.Hostname}' nunca registrou backup."
                        : $"O ultimo backup do servidor '{server.Hostname}' foi em " +
                          $"{server.LastBackupAt:dd/MM/yyyy HH:mm}.",
                    "Verificar a rotina de backup e registrar a execucao.",
                    null);
            }
        }

        // --- Ativos sem centro de custo ---
        var withoutCostCenter = await ScopedAssets(scope)
            .Where(a => a.Status == AssetStatus.Ativo && a.CostCenterId == null)
            .Select(a => new { a.Id, a.Name, a.Kind, a.OrganizationalUnitId })
            .ToListAsync(cancellationToken);

        foreach (var asset in withoutCostCenter)
        {
            TryCreate(
                AlertType.ItemSemCentroDeCusto,
                asset.Id,
                AlertPriority.Baixa,
                asset.Kind == AssetKind.License ? TimelineEntityType.License : TimelineEntityType.Server,
                asset.OrganizationalUnitId,
                $"{asset.Name} sem centro de custo",
                $"O ativo '{asset.Name}' nao possui centro de custo definido, o que impede o rateio " +
                "correto da despesa.",
                "Vincular o ativo ao centro de custo responsavel.",
                null);
        }

        foreach (var alert in created)
        {
            context.Alerts.Add(alert);
        }

        var dtos = created
            .Select(a => new AlertDto(
                a.Id,
                a.Type,
                a.Priority,
                a.Status,
                a.EntityType,
                a.EntityId,
                a.Title,
                a.Description,
                a.RecommendedAction,
                a.DueDate,
                null,
                null,
                "-",
                a.CreatedAt,
                null,
                null))
            .ToList();

        return new AlertScanResultDto(created.Count, alreadyOpen, 0, dtos);
    }

    private IQueryable<Contract> ScopedContracts(IOrganizationalScope scope)
    {
        var source = context.Contracts.AsNoTracking();

        if (scope.HasGlobalScope)
        {
            return source;
        }

        if (scope.IsEmpty)
        {
            return source.Where(_ => false);
        }

        var unitIds = scope.UnitIds.ToList();
        return source.Where(c => unitIds.Contains(c.OrganizationalUnitId));
    }

    private IQueryable<Asset> ScopedAssets(IOrganizationalScope scope)
    {
        var source = context.Assets.AsNoTracking();

        if (scope.HasGlobalScope)
        {
            return source;
        }

        if (scope.IsEmpty)
        {
            return source.Where(_ => false);
        }

        var unitIds = scope.UnitIds.ToList();
        return source.Where(a => unitIds.Contains(a.OrganizationalUnitId));
    }
}
