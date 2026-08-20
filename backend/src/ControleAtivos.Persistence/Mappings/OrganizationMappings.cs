using ControleAtivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleAtivos.Persistence.Mappings;

public sealed class TenantMap : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(80).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}

public sealed class OrganizationalUnitMap : IEntityTypeConfiguration<OrganizationalUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationalUnit> builder)
    {
        builder.ToTable("organizational_units");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Code).HasMaxLength(40).IsRequired();
        builder.Property(u => u.Description).HasMaxLength(1000);
        builder.Property(u => u.Path).HasMaxLength(1000).IsRequired();
        builder.Property(u => u.Level).IsRequired();
        builder.Property(u => u.IsActive).IsRequired();

        builder
            .HasOne(u => u.Parent)
            .WithMany(u => u.Children)
            .HasForeignKey(u => u.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => new { u.TenantId, u.Code }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.ParentId });

        // Suporta as consultas de sub-arvore (Path LIKE 'prefixo%') do escopo organizacional.
        builder
            .HasIndex(u => new { u.TenantId, u.Path })
            .HasDatabaseName("ix_organizational_units_tenant_path");
    }
}

public sealed class AppUserMap : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.ExternalSubject).HasMaxLength(200).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.Property(u => u.JobTitle).HasMaxLength(200);
        builder.Property(u => u.IsActive).IsRequired();

        builder
            .HasOne(u => u.PrimaryOrganizationalUnit)
            .WithMany()
            .HasForeignKey(u => u.PrimaryOrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => new { u.TenantId, u.ExternalSubject }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.Email });
    }
}

public sealed class ManagementAssignmentMap : IEntityTypeConfiguration<ManagementAssignment>
{
    public void Configure(EntityTypeBuilder<ManagementAssignment> builder)
    {
        builder.ToTable("management_assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Role).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(a => a.StartDate).IsRequired();
        builder.Property(a => a.IsPrimary).IsRequired();
        builder.Property(a => a.IncludesDescendants).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder
            .HasOne(a => a.User)
            .WithMany(u => u.ManagementAssignments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(a => a.OrganizationalUnit)
            .WithMany(u => u.ManagementAssignments)
            .HasForeignKey(a => a.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        /*
         * Um usuario nao pode ter o mesmo papel duas vezes na mesma unidade no
         * mesmo periodo. O indice filtrado cobre apenas vinculos vigentes,
         * permitindo reatribuir o papel depois de encerrado.
         */
        builder
            .HasIndex(a => new { a.TenantId, a.UserId, a.OrganizationalUnitId, a.Role })
            .HasFilter("end_date IS NULL")
            .IsUnique()
            .HasDatabaseName("ix_management_assignments_active_unique");

        builder.HasIndex(a => new { a.TenantId, a.UserId, a.EndDate });
        builder.HasIndex(a => new { a.TenantId, a.OrganizationalUnitId });
    }
}

public sealed class CostCenterMap : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.ToTable("cost_centers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(40).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();

        builder
            .HasOne(c => c.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(c => c.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
    }
}
