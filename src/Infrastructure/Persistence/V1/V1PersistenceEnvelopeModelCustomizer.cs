using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

/// <summary>
/// Extends the accepted v1 EF Core model with durable CFE Sobre identity and its independently
/// persisted transport lifecycle while preserving all previously accepted Reporte Diario mappings.
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
            entity.HasIndex(x => new { x.OrganizationId, x.OperationId }).IsUnique().HasDatabaseName("UX_v1_fces_operation");
            entity.HasIndex(x => x.EnvelopeId).IsUnique().HasDatabaseName("UX_v1_fces_envelope");
        });
    }
}
