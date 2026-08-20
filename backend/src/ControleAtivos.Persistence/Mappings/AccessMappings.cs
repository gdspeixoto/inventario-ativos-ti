using ControleAtivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleAtivos.Persistence.Mappings;

public sealed class UserRoleAssignmentMap : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("user_role_assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Role).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.IsActive).IsRequired();

        builder
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        /*
         * O indice unico cobre apenas as concessoes ativas: o mesmo papel pode
         * ter sido concedido e revogado varias vezes, e esse historico precisa
         * sobreviver para a auditoria.
         */
        builder
            .HasIndex(a => new { a.TenantId, a.UserId, a.Role })
            .IsUnique()
            .HasFilter("is_active")
            .HasDatabaseName("ix_user_role_assignments_active_unique");

        builder.HasIndex(a => new { a.TenantId, a.UserId });
    }
}

public sealed class LocalCredentialMap : IEntityTypeConfiguration<LocalCredential>
{
    public void Configure(EntityTypeBuilder<LocalCredential> builder)
    {
        builder.ToTable("local_credentials");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Username).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.FailedAttempts).IsRequired();

        builder.Ignore(c => c.IsExpired);
        builder.Ignore(c => c.IsLockedOut);
        builder.Ignore(c => c.CanAuthenticate);

        builder
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        /*
         * O login acontece antes de o tenant estar resolvido — nao ha token
         * ainda — entao o nome de usuario precisa ser unico no banco inteiro,
         * e nao apenas dentro do tenant.
         */
        builder
            .HasIndex(c => c.Username)
            .IsUnique()
            .HasDatabaseName("ix_local_credentials_username");

        builder.HasIndex(c => new { c.TenantId, c.UserId });
    }
}
