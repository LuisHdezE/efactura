using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1TerminalRecordConfiguration : IEntityTypeConfiguration<V1TerminalRecord>
{
    public void Configure(EntityTypeBuilder<V1TerminalRecord> entity)
    {
        entity.ToTable("v1_terminals");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasMaxLength(200).ValueGeneratedNever();
        entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
        entity.Property(x => x.NormalizedCode).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
        entity.Property(x => x.LocationId).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
        entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);

        entity.HasOne(x => x.Location)
            .WithMany(x => x.Terminals)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_v1_terminal_location");

        entity.HasIndex(x => new { x.OrganizationId, x.NormalizedCode })
            .IsUnique()
            .HasDatabaseName("UX_v1_terminal_org_code");

        entity.HasIndex(x => new { x.OrganizationId, x.Active })
            .HasDatabaseName("IX_v1_terminal_org_active");

        entity.HasIndex(x => new { x.OrganizationId, x.LocationId })
            .HasDatabaseName("IX_v1_terminal_org_location");
    }
}
