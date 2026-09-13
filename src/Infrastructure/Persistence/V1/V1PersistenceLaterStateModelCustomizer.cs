using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

/// <summary>
/// Extends the canonical v1 EF Core model with append-only Reporte Diario later-state evidence
/// while preserving every mapping owned by <see cref="V1PersistenceModelCustomizer"/>.
/// </summary>
public sealed class V1PersistenceLaterStateModelCustomizer : ModelCustomizer
{
    private readonly V1PersistenceModelCustomizer _baseline;

    public V1PersistenceLaterStateModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
        _baseline = new V1PersistenceModelCustomizer(dependencies);
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        _baseline.Customize(modelBuilder, context);

        modelBuilder.Entity<V1FiscalDailyReportLaterStateObservationRecord>(entity =>
        {
            entity.ToTable("v1_fdr_later_state_observations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
            entity.Property(x => x.SummaryDate).HasColumnType("date");
            entity.Property(x => x.OperationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DgiEmitterId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DgiReceiverId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DgiStateCode).HasMaxLength(8).IsRequired();
            entity.Property(x => x.DgiReceptionTimestampText).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EvidenceXml).IsRequired();
            entity.Property(x => x.EvidenceXmlHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ObservedAtUtc).HasPrecision(0);

            entity.HasOne<V1FiscalDailyReportSubmissionRecord>()
                .WithMany()
                .HasForeignKey(x => x.RootSubmissionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fdr_later_root");

            entity.HasOne<V1FiscalDailyReportBrCorrectionRevisionRecord>()
                .WithMany()
                .HasForeignKey(x => x.BrCorrectionRevisionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fdr_later_br");

            entity.HasIndex(x => new { x.OrganizationId, x.OperationId })
                .IsUnique()
                .HasDatabaseName("UX_v1_fdr_later_operation");
            entity.HasIndex(x => new { x.OrganizationId, x.DgiReceiverId, x.ObservedAtUtc })
                .HasDatabaseName("IX_v1_fdr_later_receiver");
            entity.HasIndex(x => x.RootSubmissionId)
                .HasDatabaseName("IX_v1_fdr_later_root");
            entity.HasIndex(x => x.BrCorrectionRevisionId)
                .HasDatabaseName("IX_v1_fdr_later_br");
        });
    }
}
