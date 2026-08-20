using ControleAtivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleAtivos.Persistence.Mappings;

public sealed class TimelineEventMap : IEntityTypeConfiguration<TimelineEvent>
{
    public void Configure(EntityTypeBuilder<TimelineEvent> builder)
    {
        builder.ToTable("timeline_events");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.EntityType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(t => t.EventType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000).IsRequired();
        builder.Property(t => t.UserDisplayName).HasMaxLength(200);
        builder.Property(t => t.PreviousAmount).HasPrecision(18, 2);
        builder.Property(t => t.NewAmount).HasPrecision(18, 2);
        builder.Property(t => t.OccurredAt).IsRequired();

        builder.Ignore(t => t.FinancialImpact);
        builder.Ignore(t => t.HasFinancialImpact);

        // Consulta principal: historico de um item em ordem cronologica inversa.
        builder
            .HasIndex(t => new { t.TenantId, t.EntityType, t.EntityId, t.OccurredAt })
            .HasDatabaseName("ix_timeline_events_entity_occurred");

        // Timeline geral e "ultimos acontecimentos" do dashboard.
        builder.HasIndex(t => new { t.TenantId, t.OccurredAt });
        builder.HasIndex(t => new { t.TenantId, t.EventType, t.OccurredAt });
        builder.HasIndex(t => new { t.TenantId, t.OrganizationalUnitId, t.OccurredAt });
        builder.HasIndex(t => t.CorrelationId);
    }
}

public sealed class AuditLogMap : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(120).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(120).IsRequired();
        builder.Property(a => a.UserDisplayName).HasMaxLength(200);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.MetadataJson).HasColumnType("jsonb");
        builder.Property(a => a.OccurredAt).IsRequired();

        builder.HasIndex(a => new { a.TenantId, a.OccurredAt });
        builder.HasIndex(a => new { a.TenantId, a.UserId, a.OccurredAt });
        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId });
        builder.HasIndex(a => a.CorrelationId);
    }
}

public sealed class AlertMap : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.EntityType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.RecommendedAction).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.ResolutionNotes).HasMaxLength(1000);
        builder.Property(a => a.DeduplicationKey).HasMaxLength(120).IsRequired();

        builder.Ignore(a => a.IsOpen);

        /*
         * Impede que a rotina de deteccao crie um segundo alerta para um
         * problema ja aberto. O indice cobre apenas alertas nao encerrados.
         */
        builder
            .HasIndex(a => new { a.TenantId, a.DeduplicationKey })
            .IsUnique()
            .HasFilter("status IN ('Aberto', 'EmAnalise')")
            .HasDatabaseName("ix_alerts_open_deduplication");

        builder.HasIndex(a => new { a.TenantId, a.Status, a.Priority });
        builder.HasIndex(a => new { a.TenantId, a.OrganizationalUnitId, a.Status });
        builder.HasIndex(a => new { a.TenantId, a.DueDate });
    }
}

public sealed class DocumentMap : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.EntityType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.SizeInBytes).IsRequired();

        builder.HasIndex(d => new { d.TenantId, d.EntityType, d.EntityId });
        builder.HasIndex(d => new { d.TenantId, d.Sha256 });
    }
}
