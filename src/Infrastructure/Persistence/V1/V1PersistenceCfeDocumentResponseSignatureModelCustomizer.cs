using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

/// <summary>
/// Extends the ACKCFE consultation persistence model with append-only XMLDSig signature-math
/// evidence. Every source relationship is Restrict-only so verification can never rewrite or
/// cascade-delete the accepted consultation, ACKSobre, submission or Sobre evidence.
/// </summary>
public sealed class V1PersistenceCfeDocumentResponseSignatureModelCustomizer : ModelCustomizer
{
    private readonly V1PersistenceCfeDocumentResponseModelCustomizer _baseline;

    public V1PersistenceCfeDocumentResponseSignatureModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
        _baseline = new V1PersistenceCfeDocumentResponseModelCustomizer(dependencies);
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        _baseline.Customize(modelBuilder, context);

        modelBuilder.Entity<V1FiscalCfeDocumentResponseSignatureVerificationRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_cfe_document_response_signature_verifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.ConsultationId).IsRequired();
            entity.Property(x => x.AckObservationId).IsRequired();
            entity.Property(x => x.SubmissionId).IsRequired();
            entity.Property(x => x.EnvelopeId).IsRequired();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ResponseSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.VerificationProfileId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CertificateSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CertificateThumbprint).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CertificateSerialNumber).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CertificateSubject).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.CertificateIssuer).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.CanonicalizationMethod).HasMaxLength(300).IsRequired();
            entity.Property(x => x.SignatureMethod).HasMaxLength(300).IsRequired();
            entity.Property(x => x.DigestMethod).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ReferenceUri).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ReferenceTransformsJson).IsRequired();
            entity.Property(x => x.CertificateTrustValidated).IsRequired();
            entity.Property(x => x.VerifiedAtUtc).HasPrecision(0);

            entity.HasOne<V1FiscalCfeDocumentResponseConsultationRecord>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrsv_consultation");
            entity.HasOne<V1FiscalCfeEnvelopeAckObservationRecord>()
                .WithMany()
                .HasForeignKey(x => x.AckObservationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrsv_ack_observation");
            entity.HasOne<V1FiscalCfeEnvelopeSubmissionRecord>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrsv_submission");
            entity.HasOne<V1FiscalCfeEnvelopeRecord>()
                .WithMany()
                .HasForeignKey(x => x.EnvelopeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcdrsv_envelope");

            entity.HasIndex(x => x.ConsultationId)
                .IsUnique()
                .HasDatabaseName("UX_v1_fcdrsv_consultation");
            entity.HasIndex(x => x.AckObservationId)
                .HasDatabaseName("IX_v1_fcdrsv_ack_observation");
            entity.HasIndex(x => x.SubmissionId)
                .HasDatabaseName("IX_v1_fcdrsv_submission");
            entity.HasIndex(x => x.EnvelopeId)
                .HasDatabaseName("IX_v1_fcdrsv_envelope");
        });
    }
}
