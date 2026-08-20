using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleAtivos.Persistence.Mappings;

/// <summary>
/// Mapeia a hierarquia de ativos como <em>table-per-hierarchy</em>: uma unica
/// tabela <c>assets</c> com discriminador <c>kind</c>. Licencas e servidores
/// compartilham a maioria dos campos e sao consultados juntos no dashboard e na
/// timeline, o que torna TPH mais eficiente que TPT aqui.
/// </summary>
public sealed class AssetMap : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");

        builder.HasKey(a => a.Id);

        builder
            .HasDiscriminator(a => a.Kind)
            .HasValue<LicenseAsset>(AssetKind.License)
            .HasValue<ServerAsset>(AssetKind.Server);

        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(30).HasColumnName("kind");
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Code).HasMaxLength(60).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(2000);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.BillingType).HasConversion<string>().HasMaxLength(30).IsRequired();

        // Money e um value object: amount e currency viram duas colunas.
        builder.ComplexProperty(a => a.MonthlyAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("monthly_amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder
            .HasOne(a => a.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(a => a.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(a => a.Supplier)
            .WithMany()
            .HasForeignKey(a => a.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(a => a.Contract)
            .WithMany(c => c.Assets)
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(a => a.CostCenter)
            .WithMany()
            .HasForeignKey(a => a.CostCenterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(a => a.TechnicalResponsible)
            .WithMany()
            .HasForeignKey(a => a.TechnicalResponsibleUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(a => a.FinancialResponsible)
            .WithMany()
            .HasForeignKey(a => a.FinancialResponsibleUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.CostEntries).WithOne(c => c.Asset!).HasForeignKey(c => c.AssetId);

        builder.HasIndex(a => new { a.TenantId, a.Code }).IsUnique();
        builder.HasIndex(a => new { a.TenantId, a.OrganizationalUnitId });
        builder.HasIndex(a => new { a.TenantId, a.Status });
        builder.HasIndex(a => new { a.TenantId, a.Kind, a.Status });
        builder.HasIndex(a => new { a.TenantId, a.RenewalDate });
        builder.HasIndex(a => new { a.TenantId, a.ContractId });
    }
}

public sealed class LicenseAssetMap : IEntityTypeConfiguration<LicenseAsset>
{
    public void Configure(EntityTypeBuilder<LicenseAsset> builder)
    {
        builder.Property(l => l.Manufacturer).HasMaxLength(120);
        builder.Property(l => l.Product).HasMaxLength(200);
        builder.Property(l => l.Plan).HasMaxLength(200);
        builder.Property(l => l.ContractedQuantity);
        builder.Property(l => l.UsedQuantity);

        builder.ComplexProperty(l => l.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("unit_price").HasPrecision(18, 4);
            money.Property(m => m.Currency).HasColumnName("unit_price_currency").HasMaxLength(3);
        });
    }
}

public sealed class ServerAssetMap : IEntityTypeConfiguration<ServerAsset>
{
    public void Configure(EntityTypeBuilder<ServerAsset> builder)
    {
        builder.Property(s => s.Hostname).HasMaxLength(253);
        builder.Property(s => s.ServerType).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Environment).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.OperatingSystem).HasMaxLength(120);
        builder.Property(s => s.OperatingSystemVersion).HasMaxLength(60);
        builder.Property(s => s.Provider).HasMaxLength(120);
        builder.Property(s => s.RegionOrDatacenter).HasMaxLength(120);
        builder.Property(s => s.PrimaryIp).HasMaxLength(45);
        builder.Property(s => s.BackupPolicy).HasMaxLength(500);
        builder.Property(s => s.Sla).HasMaxLength(200);
        builder.Property(s => s.MaintenanceWindow).HasMaxLength(200);

        builder.ComplexProperty(s => s.InfrastructureMonthlyCost, money =>
        {
            money.Property(m => m.Amount).HasColumnName("infrastructure_monthly_cost").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("infrastructure_currency").HasMaxLength(3);
        });

        builder.ComplexProperty(s => s.LicenseMonthlyCost, money =>
        {
            money.Property(m => m.Amount).HasColumnName("license_monthly_cost").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("license_cost_currency").HasMaxLength(3);
        });

        builder.ComplexProperty(s => s.SupportMonthlyCost, money =>
        {
            money.Property(m => m.Amount).HasColumnName("support_monthly_cost").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("support_cost_currency").HasMaxLength(3);
        });

        builder.ComplexProperty(s => s.BackupMonthlyCost, money =>
        {
            money.Property(m => m.Amount).HasColumnName("backup_monthly_cost").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("backup_cost_currency").HasMaxLength(3);
        });
    }
}
