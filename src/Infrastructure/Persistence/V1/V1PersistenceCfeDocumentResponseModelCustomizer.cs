using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

/// <summary>
/// Extends the accepted V1PersistenceEnvelopeModelCustomizer with append-only ACKCFE token-query
/// evidence. The source Sobre, submission and ACKSobre relationships are Restrict-only so a later
/// consultation can never rewrite or cascade-delete accepted fiscal evidence.
/// </summary>
public sealed class V1PersistenceCfeDocumentResponseModelCustomizer : ModelCustomizer
{
    private readonly V1PersistenceEnvelopeModelCustomizer _baseline;

    public V1PersistenceCfeDocumentResponseModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
        _baseline = new V1PersistenceEnvelopeModelCustomizer(dependencies);
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        _baseline.Customize(modelBuilder, context);

        modelBuilder.Entity<V1FiscalCfeDocumentResponseConsultationRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_cfe_document_response_consultations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.AckObservationId).IsRequired();
            entity.Property(x => x.SubmissionId).IsRequired();
            entity.Property(x => x.EnvelopeId).IsRequired();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OperationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SourceAckResponseSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ConsultationTokenSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
            entity.Property(x => x.ReceiverRut).HasMaxLength(12).IsRequired();
            entity.Property(x => x.DetailsJson).IsRequired();
            entity.Property(x => x.ResponseXml).IsRequired();
            entity.Property(x => x.ResponseSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ConsultedAtUtc).HasPrecision(0);
            entity.HasOne<V1FiscalCfeEnvelopeAckObservationRecord>()
                .WithMany()
                .HasForeignKey(x => x.AckObservationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrc_ack_observation");
            entity.HasOne<V1FiscalCfeEnvelopeSubmissionRecord>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrc_submission");
            entity.HasOne<V1FiscalCfeEnvelopeRecord>()
                .WithMany()
                .HasForeignKey(x => x.EnvelopeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrc_envelope");
            entity.HasIndex(x => new { x.OrganizationId, x.OperationId })
                .IsUnique()
                .HasDatabaseName("UX_v1_fcdrc_operation");
            entity.HasIndex(x => x.AckObservationId)
                .HasDatabaseName("IX_v1_fcdrc_ack_observation");
        });
    }
}
