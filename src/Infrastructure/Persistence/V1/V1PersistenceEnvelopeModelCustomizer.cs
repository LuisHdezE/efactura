using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

/// <summary>
/// Extends the accepted v1 EF Core model with durable CFE Sobre identity, transport and append-only
/// ACKSobre observation evidence while preserving all previously accepted Reporte Diario mappings.
/// </summary>
public sealed class V1PersistenceEnvelopeModelCustomizer : ModelCustomizer
{
    private readonly V1PersistenceLaterStateModelCustomizer _baseline;

    public V1PersistenceEnvelopeModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
        _baseline = new V1PersistenceLaterStateModelCustomizer(dependencies);
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        _baseline.Customize(modelBuilder, context);

        modelBuilder.Entity<V1FiscalCfeEnvelopeRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_cfe_envelopes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ReceiverRut).HasMaxLength(12).IsRequired();
            entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.Property(x => x.CreatedAtOffsetMinutes).IsRequired();
            entity.Property(x => x.FiscalDocumentIdsJson).IsRequired();
            entity.Property(x => x.OperationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CertificateThumbprint).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CertificateSerialNumber).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EnvelopeXml).IsRequired();
            entity.Property(x => x.EnvelopeSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SchemaSetId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SchemaVersion).HasMaxLength(40).IsRequired();
            entity.Property(x => x.SchemaSetFingerprint).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.OperationId }).IsUnique().HasDatabaseName("UX_v1_fce_operation");
            entity.HasIndex(x => new { x.OrganizationId, x.IssuerRuc, x.ReceiverRut, x.SenderEnvelopeId }).IsUnique().HasDatabaseName("UX_v1_fce_identity");
        });

        modelBuilder.Entity<V1FiscalCfeEnvelopeSubmissionRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_cfe_envelope_submissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.EnvelopeId).IsRequired();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
            entity.Property(x => x.ReceiverRut).HasMaxLength(12).IsRequired();
            entity.Property(x => x.OperationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EnvelopeSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PreparedAtUtc).HasPrecision(0);
            entity.Property(x => x.LastAttemptAtUtc).HasPrecision(0);
            entity.Property(x => x.CompletedAtUtc).HasPrecision(0);
            entity.Property(x => x.ResponseXml);
            entity.Property(x => x.ResponseSha256).HasMaxLength(64);
            entity.Property(x => x.FailureCode).HasMaxLength(160);
            entity.HasOne<V1FiscalCfeEnvelopeRecord>()
                .WithMany()
                .HasForeignKey(x => x.EnvelopeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fces_envelope");
            entity.HasIndex(x => new { x.OrganizationId, x.OperationId }).IsUnique().HasDatabaseName("UX_v1_fces_operation");
            entity.HasIndex(x => x.EnvelopeId).IsUnique().HasDatabaseName("UX_v1_fces_envelope");
        });

        modelBuilder.Entity<V1FiscalCfeEnvelopeAckObservationRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_cfe_envelope_ack_observations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.SubmissionId).IsRequired();
            entity.Property(x => x.EnvelopeId).IsRequired();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
            entity.Property(x => x.ReceiverRut).HasMaxLength(12).IsRequired();
            entity.Property(x => x.ResponseSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ReceptionTimestampText).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SigningTimestampText).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ConsultationToken);
            entity.Property(x => x.ConsultationAvailableAtText).HasMaxLength(80);
            entity.Property(x => x.RejectionReasonsJson).IsRequired();
            entity.Property(x => x.ObservedAtUtc).HasPrecision(0);
            entity.HasOne<V1FiscalCfeEnvelopeSubmissionRecord>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fceao_submission");
            entity.HasOne<V1FiscalCfeEnvelopeRecord>()
                .WithMany()
                .HasForeignKey(x => x.EnvelopeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fceao_envelope");
            entity.HasIndex(x => x.SubmissionId).IsUnique().HasDatabaseName("UX_v1_fceao_submission");
        });
    }
}
