using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

public sealed class V1PersistenceSecurityUserModelCustomizer : ModelCustomizer
{
    private readonly V1PersistenceSecurityRoleModelCustomizer _baseline;

    public V1PersistenceSecurityUserModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
        _baseline = new V1PersistenceSecurityRoleModelCustomizer(dependencies);
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        _baseline.Customize(modelBuilder, context);

        modelBuilder.Entity<V1SecurityUserRecord>(entity =>
        {
            entity.ToTable("v1_security_users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(200).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IdentityProvider).HasMaxLength(120).IsRequired();
            entity.Property(x => x.NormalizedIdentityProvider).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ExternalSubject).HasMaxLength(300).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.NormalizedIdentityProvider, x.ExternalSubject })
                .IsUnique()
                .HasDatabaseName("UX_v1_security_user_org_identity");
            entity.HasIndex(x => new { x.OrganizationId, x.Active })
                .HasDatabaseName("IX_v1_security_user_org_active");
        });

        modelBuilder.Entity<V1SecurityUserLocationScopeRecord>(entity =>
        {
            entity.ToTable("v1_security_user_location_scopes");
            entity.HasKey(x => new { x.UserId, x.LocationId });
            entity.Property(x => x.UserId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LocationId).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.LocationScopes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_v1_security_user_location_user");
            entity.HasIndex(x => x.LocationId)
                .HasDatabaseName("IX_v1_security_user_location_scope_location");
        });

        modelBuilder.Entity<V1SecurityUserTerminalScopeRecord>(entity =>
        {
            entity.ToTable("v1_security_user_terminal_scopes");
            entity.HasKey(x => new { x.UserId, x.TerminalId });
            entity.Property(x => x.UserId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TerminalId).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.TerminalScopes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_v1_security_user_terminal_user");
            entity.HasIndex(x => x.TerminalId)
                .HasDatabaseName("IX_v1_security_user_terminal_scope_terminal");
        });

        modelBuilder.Entity<V1SecurityUserRoleRecord>(entity =>
        {
            entity.ToTable("v1_security_user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.Property(x => x.UserId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RoleId).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_v1_security_user_role_user");
            entity.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_security_user_role_role");
            entity.HasIndex(x => x.RoleId)
                .HasDatabaseName("IX_v1_security_user_role_role");
        });
    }
}
