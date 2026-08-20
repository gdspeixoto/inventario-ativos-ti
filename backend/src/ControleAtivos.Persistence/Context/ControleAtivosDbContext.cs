using System.Reflection;
using ControleAtivos.Application.Abstractions;
using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Context;

/// <summary>
/// Contexto principal. Concentra tres responsabilidades transversais que, se
/// ficassem a cargo de cada caso de uso, acabariam esquecidas em algum ponto:
///
/// 1. isolamento por tenant, via filtro global;
/// 2. carimbo de auditoria (quem criou, quem alterou, quando);
/// 3. conversao de eventos de dominio em linhas de timeline, na mesma transacao.
/// </summary>
public sealed class ControleAtivosDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public ControleAtivosDbContext(
        DbContextOptions<ControleAtivosDbContext> options,
        ITenantContext tenantContext,
        ICurrentUser currentUser)
        : base(options)
    {
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<ManagementAssignment> ManagementAssignments => Set<ManagementAssignment>();

    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    public DbSet<LocalCredential> LocalCredentials => Set<LocalCredential>();

    public DbSet<CostCenter> CostCenters => Set<CostCenter>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<LicenseAsset> Licenses => Set<LicenseAsset>();

    public DbSet<ServerAsset> Servers => Set<ServerAsset>();

    public DbSet<CostEntry> CostEntries => Set<CostEntry>();

    public DbSet<PriceAdjustment> PriceAdjustments => Set<PriceAdjustment>();

    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("controle_ativos");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyTenantFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // Aplicado por ultimo para alcancar tambem os nomes gerados por convencao.
        modelBuilder.ApplySnakeCaseNames();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Toda string sem tamanho declarado vira varchar(200) em vez de text.
        configurationBuilder.Properties<string>().HaveMaxLength(200);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);

        base.ConfigureConventions(configurationBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        MaterializeDomainEvents();

        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        MaterializeDomainEvents();

        return base.SaveChanges();
    }

    /// <summary>
    /// Registra o filtro global de tenant para toda entidade derivada de
    /// <see cref="TenantEntity"/>. O filtro le o tenant no momento da consulta,
    /// e nao no momento da configuracao do modelo — por isso a expressao acessa
    /// <c>_tenantContext</c> diretamente.
    /// </summary>
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            /*
             * Em uma hierarquia TPH o filtro pertence apenas ao tipo raiz —
             * LicenseAsset e ServerAsset herdam o filtro de Asset. Aplica-lo nas
             * derivadas e rejeitado pelo EF Core.
             */
            if (entityType.BaseType is not null)
            {
                continue;
            }

            var method = typeof(ControleAtivosDbContext)
                .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [modelBuilder]);
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : TenantEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity =>
            _tenantContext.IsTenantFilterBypassed || entity.TenantId == _tenantContext.TenantId);

    private void ApplyAuditInformation()
    {
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreationAudit(userId);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdateAudit(userId);
                    break;
            }
        }
    }

    /// <summary>
    /// Converte os eventos acumulados nas entidades em <see cref="TimelineEvent"/>.
    /// Roda antes do <c>SaveChanges</c> para que historico e mudanca compartilhem
    /// a mesma transacao: ou os dois persistem, ou nenhum.
    /// </summary>
    private void MaterializeDomainEvents()
    {
        var entities = ChangeTracker
            .Entries<Entity>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        if (entities.Count == 0)
        {
            return;
        }

        var correlationId = Guid.NewGuid();
        var tenantId = _tenantContext.TenantId;

        foreach (var entity in entities)
        {
            var effectiveTenantId = entity is TenantEntity tenantEntity ? tenantEntity.TenantId : tenantId;

            foreach (var domainEvent in entity.DomainEvents.OfType<TimelineEntryRequested>())
            {
                TimelineEvents.Add(new TimelineEvent(
                    Guid.NewGuid(),
                    effectiveTenantId,
                    domainEvent.EntityType,
                    domainEvent.EntityId,
                    domainEvent.EventType,
                    domainEvent.Title,
                    domainEvent.Description,
                    domainEvent.OccurredAt,
                    domainEvent.OrganizationalUnitId,
                    _currentUser.UserId,
                    _currentUser.DisplayName,
                    domainEvent.PreviousAmount,
                    domainEvent.NewAmount,
                    correlationId));
            }

            entity.ClearDomainEvents();
        }
    }
}
