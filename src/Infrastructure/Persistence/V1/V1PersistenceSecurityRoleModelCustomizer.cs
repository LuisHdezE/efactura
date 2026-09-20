using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

public sealed class V1PersistenceSecurityRoleModelCustomizer : ModelCustomizer
{
    private readonly V1PersistenceCfeDocumentResponseCertificateTrustModelCustomizer _baseline;

    public V1PersistenceSecurityRoleModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
        _baseline = new V1PersistenceCfeDocumentResponseCertificateTrustModelCustomizer(dependencies);
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        _baseline.Customize(modelBuilder, context);

        modelBuilder.Entity<V1SecurityRoleRecord>(entity =>
        {
            entity.ToTable("v1_security_roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(200).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.NormalizedName })
                .IsUnique()
                .HasDatabaseName("UX_v1_security_role_org_name");
            entity.HasIndex(x => new { x.OrganizationId, x.Active })
                .HasDatabaseName("IX_v1_security_role_org_active");
        });

        modelBuilder.Entity<V1SecurityRolePermissionRecord>(entity =>
        {
            entity.ToTable("v1_security_role_permissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionCode });
            entity.Property(x => x.RoleId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PermissionCode).HasMaxLength(160).IsRequired();
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_v1_security_role_permission_role");
            entity.HasIndex(x => x.PermissionCode)
                .HasDatabaseName("IX_v1_security_role_permission_code");
        });
    }
}
