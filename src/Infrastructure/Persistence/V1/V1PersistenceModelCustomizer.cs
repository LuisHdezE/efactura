using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

public sealed class V1PersistenceModelCustomizer : ModelCustomizer
{
    public V1PersistenceModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        ConfigureOrganization(modelBuilder);
        ConfigureFinance(modelBuilder);
        ConfigureSaleLocalEffects(modelBuilder);
        ConfigureSaleConfirmation(modelBuilder);
        ConfigureFiscalDocumentIdentity(modelBuilder);
        ConfigureFiscalContentSnapshot(modelBuilder);
        ConfigureFiscalSigningEvidence(modelBuilder);
        ConfigureFiscalSignedArtifact(modelBuilder);
        ConfigureFiscalDailyReportDurability(modelBuilder);
    }

    private static void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1CompanyFiscalProfileRecord>(entity =>
        {
            entity.ToTable("v1_company_fiscal_profiles");
            entity.HasKey(x => x.OrganizationId);
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Ruc).HasMaxLength(12).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CommercialName).HasMaxLength(30);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => x.Ruc)
                .HasDatabaseName("IX_v1_company_ruc");
        });

        modelBuilder.Entity<V1FiscalLocationRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_locations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(200).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DgiBranchCode).HasMaxLength(4).IsRequired();
            entity.Property(x => x.FiscalAddress).HasMaxLength(70).IsRequired();
            entity.Property(x => x.City).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Department).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.DgiBranchCode })
                .IsUnique()
                .HasDatabaseName("UX_v1_location_org_branch");
            entity.HasIndex(x => new { x.OrganizationId, x.Active })
                .HasDatabaseName("IX_v1_location_org_active");
        });
    }

    private static void ConfigureFinance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1PaymentMethodRecord>(entity =>
        {
            entity.ToTable("v1_payment_methods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.Enabled })
                .HasDatabaseName("IX_v1_pm_org_enabled");
        });

        modelBuilder.Entity<V1PaymentRecord>(entity =>
        {
            entity.ToTable("v1_payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 6);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.ExternalReference).HasMaxLength(200);
            entity.Property(x => x.ConfirmationFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SettlementFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RecordedAtUtc).HasPrecision(6);
            entity.HasOne<V1SaleRecord>()
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_pay_sale");
            entity.HasOne<V1PaymentMethodRecord>()
                .WithMany()
                .HasForeignKey(x => x.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_pay_method");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId })
                .HasDatabaseName("IX_v1_pay_org_sale");
            entity.HasIndex(x => x.PaymentMethodId)
                .HasDatabaseName("IX_v1_pay_method");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId, x.SettlementFingerprint, x.Sequence })
                .IsUnique()
                .HasDatabaseName("UX_v1_pay_sale_plan_seq");
        });

        modelBuilder.Entity<V1ReceivableRecord>(entity =>
        {
            entity.ToTable("v1_receivables");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OriginalAmount).HasPrecision(18, 6);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.DueDate).HasColumnType("date");
            entity.Property(x => x.ConfirmationFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SettlementFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.HasOne<V1SaleRecord>()
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_ar_sale");
            entity.HasOne<V1PartyRecord>()
                .WithMany()
                .HasForeignKey(x => x.CustomerPartyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_ar_customer");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId })
                .IsUnique()
                .HasDatabaseName("UX_v1_ar_org_sale");
            entity.HasIndex(x => new { x.OrganizationId, x.CustomerPartyId, x.DueDate })
                .HasDatabaseName("IX_v1_ar_org_customer_due");
        });
    }

    private static void ConfigureSaleLocalEffects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1StockMovementRecord>(entity =>
        {
            entity.Property(x => x.ConfirmationFingerprint).HasMaxLength(64);
            entity.Property(x => x.SettlementFingerprint).HasMaxLength(64);
            entity.HasOne<V1SaleRecord>()
                .WithMany()
                .HasForeignKey(x => x.SourceSaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_stock_sale");
            entity.HasIndex(x => new { x.OrganizationId, x.SourceSaleId, x.PositionId })
                .IsUnique()
                .HasDatabaseName("UX_v1_stock_sale_position");
        });

        modelBuilder.Entity<V1FiscalizationRequestRecord>(entity =>
        {
            entity.ToTable("v1_fiscalization_requests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LocationId).HasMaxLength(200);
            entity.Property(x => x.TerminalId).HasMaxLength(200);
            entity.Property(x => x.FormatVersion).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ConfirmationFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SettlementFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.NetAmount).HasPrecision(18, 6);
            entity.Property(x => x.VatAmount).HasPrecision(18, 6);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 6);
            entity.Property(x => x.ConfirmationEvidenceFingerprint).HasMaxLength(64);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.RequestedAtUtc).HasPrecision(6);
            entity.Property(x => x.IdentityCreatedAtUtc).HasPrecision(6);
            entity.HasOne<V1SaleRecord>()
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fiscal_req_sale");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId })
                .IsUnique()
                .HasDatabaseName("UX_v1_fiscal_req_org_sale");
            entity.HasIndex(x => new { x.OrganizationId, x.Status, x.RequestedAtUtc })
                .HasDatabaseName("IX_v1_fiscal_req_work");
        });
    }

    private static void ConfigureSaleConfirmation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1SaleRecord>(entity =>
        {
            entity.Property(x => x.ConfirmationFingerprint).HasMaxLength(64);
            entity.Property(x => x.SettlementFingerprint).HasMaxLength(64);
            entity.Property(x => x.ConfirmedAtUtc).HasPrecision(6);
        });
    }

    private static void ConfigureFiscalDocumentIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1FiscalDocumentRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_documents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Series).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CaeAuthorizationNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CaeValidFrom).HasColumnType("date");
            entity.Property(x => x.CaeValidTo).HasColumnType("date");
            entity.Property(x => x.FiscalDate).HasColumnType("date");
            entity.Property(x => x.LocationId).HasMaxLength(200);
            entity.Property(x => x.TerminalId).HasMaxLength(200);
            entity.Property(x => x.FormatVersion).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ConfirmationFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SettlementFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.NetAmount).HasPrecision(18, 6);
            entity.Property(x => x.VatAmount).HasPrecision(18, 6);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 6);
            entity.Property(x => x.IdentityCreatedAtUtc).HasPrecision(6);

            entity.HasOne<V1FiscalizationRequestRecord>()
                .WithMany()
                .HasForeignKey(x => x.FiscalizationRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fd_req");
            entity.HasOne<V1SaleRecord>()
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fd_sale");
            entity.HasOne<V1FiscalNumberReservationRecord>()
                .WithMany()
                .HasForeignKey(x => x.FiscalNumberReservationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fd_res");
            entity.HasOne<V1CaeAuthorizationRecord>()
                .WithMany()
                .HasForeignKey(x => x.CaeAuthorizationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fd_cae");

            entity.HasIndex(x => new { x.OrganizationId, x.FiscalizationRequestId })
                .IsUnique()
                .HasDatabaseName("UX_v1_fd_org_req");
            entity.HasIndex(x => new { x.OrganizationId, x.CfeType, x.Series, x.Number })
                .IsUnique()
                .HasDatabaseName("UX_v1_fd_identity");
            entity.HasIndex(x => x.FiscalNumberReservationId)
                .IsUnique()
                .HasDatabaseName("UX_v1_fd_res");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId })
                .HasDatabaseName("IX_v1_fd_org_sale");
            entity.HasIndex(x => new { x.OrganizationId, x.Status, x.IdentityCreatedAtUtc })
                .HasDatabaseName("IX_v1_fd_org_status");
        });
    }

    private static void ConfigureFiscalContentSnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1FiscalContentSnapshotRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_content_snapshots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ContentFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SnapshotJson).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);

            entity.HasOne<V1FiscalDocumentRecord>()
                .WithMany()
                .HasForeignKey(x => x.FiscalDocumentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcs_document");
            entity.HasOne<V1FiscalizationRequestRecord>()
                .WithMany()
                .HasForeignKey(x => x.FiscalizationRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcs_request");
            entity.HasOne<V1SaleRecord>()
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fcs_sale");

            entity.HasIndex(x => x.FiscalDocumentId)
                .IsUnique()
                .HasDatabaseName("UX_v1_fcs_document");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId })
                .HasDatabaseName("IX_v1_fcs_org_sale");
        });
    }

    private static void ConfigureFiscalSigningEvidence(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1FiscalSigningEvidenceRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_signing_evidence");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.FiscalContentFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UnsignedContentHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SigningTimestamp).HasPrecision(0);

            entity.HasOne<V1FiscalDocumentRecord>()
                .WithMany()
                .HasForeignKey(x => x.FiscalDocumentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fse_document");

            entity.HasIndex(x => new { x.OrganizationId, x.FiscalDocumentId })
                .IsUnique()
                .HasDatabaseName("UX_v1_fse_org_document");
        });
    }

    private static void ConfigureFiscalSignedArtifact(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1FiscalSignedArtifactRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_signed_artifacts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.FiscalContentFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UnsignedContentHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SigningPayloadHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SignedContentHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SigningTimestamp).HasPrecision(0);
            entity.Property(x => x.SignatureProfileId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CertificateThumbprint).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CertificateSerialNumber).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SignedXml).IsRequired();

            entity.HasOne<V1FiscalDocumentRecord>()
                .WithMany()
                .HasForeignKey(x => x.FiscalDocumentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fsa_document");
            entity.HasOne<V1FiscalSigningEvidenceRecord>()
                .WithMany()
                .HasForeignKey(x => x.SigningEvidenceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fsa_evidence");

            entity.HasIndex(x => new { x.OrganizationId, x.FiscalDocumentId })
                .IsUnique()
                .HasDatabaseName("UX_v1_fsa_org_document");
            entity.HasIndex(x => x.SigningEvidenceId)
                .IsUnique()
                .HasDatabaseName("UX_v1_fsa_evidence");
        });
    }

private static void ConfigureFiscalDailyReportDurability(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<V1FiscalDailyReportSigningEvidenceRecord>(entity =>
    {
        entity.ToTable("v1_fiscal_daily_report_signing_evidence");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
        entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
        entity.Property(x => x.SummaryDate).HasColumnType("date");
        entity.Property(x => x.FunctionalFormatVersion).HasMaxLength(40).IsRequired();
        entity.Property(x => x.ProjectionFingerprint).HasMaxLength(64).IsRequired();
        entity.Property(x => x.UnsignedContentHash).HasMaxLength(64).IsRequired();
        entity.Property(x => x.SigningTimestamp).HasPrecision(0);
        entity.HasIndex(x => new { x.OrganizationId, x.IssuerRuc, x.SummaryDate, x.Sequence })
            .IsUnique()
            .HasDatabaseName("UX_v1_fdr_evidence_identity");
    });

    modelBuilder.Entity<V1FiscalDailyReportSignedArtifactRecord>(entity =>
    {
        entity.ToTable("v1_fiscal_daily_report_signed_artifacts");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
        entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
        entity.Property(x => x.SummaryDate).HasColumnType("date");
        entity.Property(x => x.FunctionalFormatVersion).HasMaxLength(40).IsRequired();
        entity.Property(x => x.ProjectionFingerprint).HasMaxLength(64).IsRequired();
        entity.Property(x => x.UnsignedContentHash).HasMaxLength(64).IsRequired();
        entity.Property(x => x.SignedContentHash).HasMaxLength(64).IsRequired();
        entity.Property(x => x.SigningTimestamp).HasPrecision(0);
        entity.Property(x => x.SignatureProfileId).HasMaxLength(120).IsRequired();
        entity.Property(x => x.CertificateThumbprint).HasMaxLength(160).IsRequired();
        entity.Property(x => x.CertificateSerialNumber).HasMaxLength(160).IsRequired();
        entity.Property(x => x.SchemaSetId).HasMaxLength(120).IsRequired();
        entity.Property(x => x.SchemaFunctionalFormatVersion).HasMaxLength(40).IsRequired();
        entity.Property(x => x.SchemaArchiveVersion).HasMaxLength(40).IsRequired();
        entity.Property(x => x.SchemaSetFingerprint).HasMaxLength(64).IsRequired();
        entity.Property(x => x.SignedXml).IsRequired();
        entity.HasOne<V1FiscalDailyReportSigningEvidenceRecord>()
            .WithMany()
            .HasForeignKey(x => x.SigningEvidenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_v1_fdr_artifact_evidence");
        entity.HasIndex(x => new { x.OrganizationId, x.IssuerRuc, x.SummaryDate, x.Sequence })
            .IsUnique()
            .HasDatabaseName("UX_v1_fdr_artifact_identity");
        entity.HasIndex(x => x.SigningEvidenceId)
            .IsUnique()
            .HasDatabaseName("UX_v1_fdr_artifact_evidence");
    });
}

}
