using ControleAtivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleAtivos.Persistence.Mappings;

public sealed class SupplierMap : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.DocumentNumber).HasMaxLength(14);
        builder.Property(s => s.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(s => s.MainContactName).HasMaxLength(200);
        builder.Property(s => s.Email).HasMaxLength(320);
        builder.Property(s => s.Phone).HasMaxLength(40);
        builder.Property(s => s.Website).HasMaxLength(300);
        builder.Property(s => s.SlaDescription).HasMaxLength(1000);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder
            .HasIndex(s => new { s.TenantId, s.DocumentNumber })
            .IsUnique()
            .HasFilter("document_number IS NOT NULL");

        builder.HasIndex(s => new { s.TenantId, s.Name });
    }
}

public sealed class ContractMap : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Number).HasMaxLength(60).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.Category).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.AdjustmentIndex).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(c => c.AdjustmentPeriodicity).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.ComplexProperty(c => c.MonthlyAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("monthly_amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        // AnnualAmount e derivado de MonthlyAmount; nao ha coluna correspondente.
        builder.Ignore(c => c.AnnualAmount);

        builder
            .HasOne(c => c.Supplier)
            .WithMany(s => s.Contracts)
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(c => c.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(c => c.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(c => c.InternalResponsible)
            .WithMany()
            .HasForeignKey(c => c.InternalResponsibleUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.TenantId, c.Number }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.SupplierId });
        builder.HasIndex(c => new { c.TenantId, c.EndDate });
        builder.HasIndex(c => new { c.TenantId, c.Status });
    }
}

public sealed class CostEntryMap : IEntityTypeConfiguration<CostEntry>
{
    public void Configure(EntityTypeBuilder<CostEntry> builder)
    {
        builder.ToTable("cost_entries");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.Category).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500).IsRequired();

        builder.ComplexProperty(c => c.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder
            .HasOne(c => c.Contract)
            .WithMany()
            .HasForeignKey(c => c.ContractId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(c => c.Supplier)
            .WithMany()
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(c => c.CostCenter)
            .WithMany()
            .HasForeignKey(c => c.CostCenterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.TenantId, c.CompetenceMonth });
        builder.HasIndex(c => new { c.TenantId, c.AssetId, c.CompetenceMonth });
        builder.HasIndex(c => new { c.TenantId, c.OrganizationalUnitId, c.CompetenceMonth });
    }
}

public sealed class PriceAdjustmentMap : IEntityTypeConfiguration<PriceAdjustment>
{
    public void Configure(EntityTypeBuilder<PriceAdjustment> builder)
    {
        builder.ToTable("price_adjustments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TargetType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.IndexApplied).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(p => p.EffectiveDate).IsRequired();

        builder.ComplexProperty(p => p.PreviousAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("previous_amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("previous_currency").HasMaxLength(3).IsRequired();
        });

        builder.ComplexProperty(p => p.NewAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("new_amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("new_currency").HasMaxLength(3).IsRequired();
        });

        // Diferenca, percentual e impacto anual sao sempre recalculados na leitura.
        builder.Ignore(p => p.AbsoluteDifference);
        builder.Ignore(p => p.PercentageDifference);
        builder.Ignore(p => p.AnnualImpact);
        builder.Ignore(p => p.IsIncrease);

        builder.HasIndex(p => new { p.TenantId, p.EffectiveDate });
        builder.HasIndex(p => new { p.TenantId, p.TargetType, p.TargetId });
        builder.HasIndex(p => new { p.TenantId, p.OrganizationalUnitId });
    }
}
