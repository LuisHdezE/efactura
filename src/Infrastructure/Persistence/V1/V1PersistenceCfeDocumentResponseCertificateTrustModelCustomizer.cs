using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

/// <summary>
/// Extends the accepted ACKCFE signature model with append-only PKI Uruguay certificate-trust
/// evidence. All source relationships are Restrict-only and no earlier evidence is rewritten.
/// </summary>
public sealed class V1PersistenceCfeDocumentResponseCertificateTrustModelCustomizer : ModelCustomizer
{
    private readonly V1PersistenceCfeDocumentResponseSignatureModelCustomizer _baseline;

    public V1PersistenceCfeDocumentResponseCertificateTrustModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
        _baseline = new V1PersistenceCfeDocumentResponseSignatureModelCustomizer(dependencies);
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        _baseline.Customize(modelBuilder, context);

        modelBuilder.Entity<V1FiscalCfeDocumentResponseCertificateTrustValidationRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_cfe_document_response_certificate_trust_validations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.SignatureVerificationId).IsRequired();
            entity.Property(x => x.ConsultationId).IsRequired();
            entity.Property(x => x.AckObservationId).IsRequired();
            entity.Property(x => x.SubmissionId).IsRequired();
            entity.Property(x => x.EnvelopeId).IsRequired();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OperationId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ResponseSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ValidationProfileId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CertificateSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TrustedRootSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ChainCertificateSha256Json).IsRequired();
            entity.Property(x => x.RevocationMode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.PkiUruguayTrustValidated).IsRequired();
            entity.Property(x => x.DgiIdentityValidated).IsRequired();
            entity.Property(x => x.ValidatedAtUtc).HasPrecision(0);

            entity.HasOne<V1FiscalCfeDocumentResponseSignatureVerificationRecord>()
                .WithMany()
                .HasForeignKey(x => x.SignatureVerificationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrctv_signature_verification");
            entity.HasOne<V1FiscalCfeDocumentResponseConsultationRecord>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrctv_consultation");
            entity.HasOne<V1FiscalCfeEnvelopeAckObservationRecord>()
                .WithMany()
                .HasForeignKey(x => x.AckObservationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrctv_ack_observation");
            entity.HasOne<V1FiscalCfeEnvelopeSubmissionRecord>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrctv_submission");
            entity.HasOne<V1FiscalCfeEnvelopeRecord>()
                .WithMany()
                .HasForeignKey(x => x.EnvelopeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrctv_envelope");

            entity.HasIndex(x => new { x.OrganizationId, x.OperationId })
                .IsUnique()
                .HasDatabaseName("UX_v1_fcdrctv_operation");
            entity.HasIndex(x => x.SignatureVerificationId)
                .HasDatabaseName("IX_v1_fcdrctv_signature_verification");
            entity.HasIndex(x => x.ConsultationId)
                .HasDatabaseName("IX_v1_fcdrctv_consultation");
            entity.HasIndex(x => x.AckObservationId)
                .HasDatabaseName("IX_v1_fcdrctv_ack_observation");
            entity.HasIndex(x => x.SubmissionId)
                .HasDatabaseName("IX_v1_fcdrctv_submission");
            entity.HasIndex(x => x.EnvelopeId)
                .HasDatabaseName("IX_v1_fcdrctv_envelope");
        });
    }
}
