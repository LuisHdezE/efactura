using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EFactura.Domain.Common;

namespace EFactura.Domain.Fiscal;

/// <summary>
/// Why a CFE number is reported as annulled rather than as an emitted/non-rejected CFE.
/// This is reporting evidence only. A credit/debit note does not annul its referenced CFE.
/// </summary>
public enum FiscalDailyReportAnnulmentKind
{
    CompanyAnnulment = 1,
    DgiRejection = 2
}

/// <summary>
/// Frozen evidence for one CFE that is eligible to contribute monetary totals to a Daily Report.
/// The caller must provide separate evidence that the document belongs in the "emitted and not
/// rejected by DGI" population. A signed artifact alone is intentionally insufficient.
/// </summary>
public sealed record FiscalDailyReportDocumentEvidence(
    Guid FiscalDocumentId,
    Guid SignedArtifactId,
    Guid SigningEvidenceId,
    string OrganizationId,
    string IssuerRuc,
    CfeFamily CfeType,
    string Series,
    long Number,
    DateOnly FiscalDate,
    string DgiBranchCode,
    bool PaymentOnBehalfOfThirdParty,
    string ThirdPartyPaymentEvidenceFingerprint,
    DateTimeOffset SigningTimestamp,
    string FiscalContentFingerprint,
    string SignedContentHash,
    string ReportingStatusEvidenceFingerprint,
    decimal NetAmount,
    decimal NonTaxedAmount,
    decimal MinimumTaxableAmount,
    decimal BasicTaxableAmount,
    decimal ExportAmount,
    decimal MinimumVatAmount,
    decimal BasicVatAmount,
    decimal VatAmount,
    decimal TotalAmount,
    string EvidenceFingerprint)
{
    public static FiscalDailyReportDocumentEvidence Capture(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        Guid signedArtifactId,
        Guid signingEvidenceId,
        DateTimeOffset signingTimestamp,
        string artifactFiscalContentFingerprint,
        string signedContentHash,
        string reportingStatusEvidenceFingerprint,
        bool paymentOnBehalfOfThirdParty,
        string thirdPartyPaymentEvidenceFingerprint)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(snapshot);

        snapshot.EnsureIntegrity();
        EnsureSupportedFamily(document.CfeType);

        if (signedArtifactId == Guid.Empty)
            throw Rule("fiscal.daily_report.signed_artifact_id_required", "Daily-report evidence requires a signed artifact id.");
        if (signingEvidenceId == Guid.Empty)
            throw Rule("fiscal.daily_report.signing_evidence_id_required", "Daily-report evidence requires signing evidence.");

        if (!string.Equals(document.OrganizationId, snapshot.OrganizationId, StringComparison.Ordinal)
            || document.SaleId != snapshot.SaleId
            || document.CfeType != snapshot.CfeFamily
            || !string.Equals(document.FormatVersion, snapshot.FormatVersion, StringComparison.Ordinal)
            || !string.Equals(document.ConfirmationFingerprint, snapshot.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(document.SettlementFingerprint, snapshot.SettlementFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.identity_snapshot_mismatch",
                "Fiscal document identity and immutable content snapshot do not match.");
        }

        var artifactFingerprint = Fingerprint(
            artifactFiscalContentFingerprint,
            "fiscal.daily_report.artifact_snapshot_fingerprint_invalid");
        if (!string.Equals(artifactFingerprint, snapshot.ContentFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.artifact_snapshot_fingerprint_mismatch",
                "Signed artifact fiscal-content fingerprint does not match the immutable snapshot.");
        }

        if (!string.Equals(document.CurrencyCode, "UYU", StringComparison.Ordinal)
            || !string.Equals(snapshot.FiscalEvidence.CurrencyCode, "UYU", StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.foreign_currency_conversion_evidence_required",
                "Daily-report monetary evidence must be in UYU; foreign-currency CFE requires separately frozen fiscal exchange-rate evidence.");
        }

        var totals = snapshot.FiscalEvidence.Totals;
        var nonTaxed = totals.NetAmount
            - totals.MinimumTaxableAmount
            - totals.BasicTaxableAmount
            - totals.ExportAmount;
        if (nonTaxed < 0m)
        {
            throw Rule(
                "fiscal.daily_report.amount_partition_invalid",
                "Frozen fiscal totals cannot be partitioned safely for Daily Report reconciliation.");
        }

        var provisional = new FiscalDailyReportDocumentEvidence(
            document.Id,
            signedArtifactId,
            signingEvidenceId,
            document.OrganizationId,
            snapshot.Issuer.Ruc,
            document.CfeType,
            document.Series,
            document.Number,
            document.FiscalDate,
            snapshot.Issuer.DgiBranchCode,
            paymentOnBehalfOfThirdParty,
            Fingerprint(
                thirdPartyPaymentEvidenceFingerprint,
                "fiscal.daily_report.third_party_payment_evidence_invalid"),
            NormalizeToSecond(signingTimestamp),
            artifactFingerprint,
            Fingerprint(signedContentHash, "fiscal.daily_report.signed_content_hash_invalid"),
            Fingerprint(reportingStatusEvidenceFingerprint, "fiscal.daily_report.reporting_status_evidence_invalid"),
            totals.NetAmount,
            nonTaxed,
            totals.MinimumTaxableAmount,
            totals.BasicTaxableAmount,
            totals.ExportAmount,
            totals.MinimumVatAmount,
            totals.BasicVatAmount,
            totals.VatAmount,
            totals.TotalAmount,
            new string('0', 64));

        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        if (FiscalDocumentId == Guid.Empty || SignedArtifactId == Guid.Empty || SigningEvidenceId == Guid.Empty)
            throw Rule("fiscal.daily_report.document_identity_invalid", "Daily-report document evidence contains an empty durable identity.");

        Required(OrganizationId, 200, "fiscal.daily_report.organization_required");
        Ruc(IssuerRuc);
        EnsureSupportedFamily(CfeType);
        Required(Series, 20, "fiscal.daily_report.series_required");
        if (Number is < 1 or > 9_999_999)
            throw Rule("fiscal.daily_report.number_invalid", "Daily-report CFE number must be between 1 and 9999999.");
        Branch(DgiBranchCode);

        Fingerprint(ThirdPartyPaymentEvidenceFingerprint, "fiscal.daily_report.third_party_payment_evidence_invalid");
        Fingerprint(FiscalContentFingerprint, "fiscal.daily_report.content_fingerprint_invalid");
        Fingerprint(SignedContentHash, "fiscal.daily_report.signed_content_hash_invalid");
        Fingerprint(ReportingStatusEvidenceFingerprint, "fiscal.daily_report.reporting_status_evidence_invalid");

        var values = new[]
        {
            NetAmount, NonTaxedAmount, MinimumTaxableAmount, BasicTaxableAmount, ExportAmount,
            MinimumVatAmount, BasicVatAmount, VatAmount, TotalAmount
        };
        if (values.Any(value => value < 0m))
            throw Rule("fiscal.daily_report.amount_invalid", "Daily-report monetary evidence cannot contain negative amounts.");
        if (NetAmount != NonTaxedAmount + MinimumTaxableAmount + BasicTaxableAmount + ExportAmount)
            throw Rule("fiscal.daily_report.net_partition_mismatch", "Daily-report net amount does not match its frozen tax partitions.");
        if (VatAmount != MinimumVatAmount + BasicVatAmount)
            throw Rule("fiscal.daily_report.vat_partition_mismatch", "Daily-report VAT amount does not match its frozen VAT partitions.");
        if (TotalAmount != NetAmount + VatAmount)
            throw Rule("fiscal.daily_report.total_mismatch", "Daily-report total amount does not equal net plus VAT.");

        Fingerprint(EvidenceFingerprint, "fiscal.daily_report.evidence_fingerprint_invalid");
        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.evidence_fingerprint_mismatch", "Daily-report document evidence fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint()
    {
        var material = string.Join(
            "|",
            FiscalDocumentId.ToString("N"),
            SignedArtifactId.ToString("N"),
            SigningEvidenceId.ToString("N"),
            OrganizationId,
            IssuerRuc,
            ((int)CfeType).ToString(CultureInfo.InvariantCulture),
            Series,
            Number.ToString(CultureInfo.InvariantCulture),
            FiscalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DgiBranchCode,
            PaymentOnBehalfOfThirdParty ? "1" : "0",
            ThirdPartyPaymentEvidenceFingerprint,
            SigningTimestamp.ToString("O", CultureInfo.InvariantCulture),
            FiscalContentFingerprint,
            SignedContentHash,
            ReportingStatusEvidenceFingerprint,
            Decimal(NetAmount),
            Decimal(NonTaxedAmount),
            Decimal(MinimumTaxableAmount),
            Decimal(BasicTaxableAmount),
            Decimal(ExportAmount),
            Decimal(MinimumVatAmount),
            Decimal(BasicVatAmount),
            Decimal(VatAmount),
            Decimal(TotalAmount));
        return Hash(material);
    }

    internal static void EnsureSupportedFamily(CfeFamily family)
    {
        if (family is not CfeFamily.ETicket
            and not CfeFamily.ETicketCreditNote
            and not CfeFamily.ETicketDebitNote
            and not CfeFamily.EFactura
            and not CfeFamily.EFacturaCreditNote
            and not CfeFamily.EFacturaDebitNote)
        {
            throw Rule(
                "fiscal.daily_report.cfe_family_not_supported",
                "Daily Report foundation only supports the accepted domestic 101/102/103/111/112/113 CFE families.");
        }
    }

    internal static string Fingerprint(string value, string code)
    {
        var normalized = Required(value, 64, code).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Rule(code, "Daily-report evidence requires a SHA-256 hexadecimal fingerprint.");
        return normalized;
    }

    internal static string Ruc(string value)
    {
        var normalized = Required(value, 12, "fiscal.daily_report.issuer_ruc_required");
        if (normalized.Length != 12 || normalized.Any(ch => ch is < '0' or > '9'))
            throw Rule("fiscal.daily_report.issuer_ruc_invalid", "Daily-report issuer RUC must contain 12 decimal digits.");
        return normalized;
    }

    internal static string Branch(string value)
    {
        var normalized = Required(value, 4, "fiscal.daily_report.branch_required");
        if (normalized.Length != 4 || normalized.Any(ch => ch is < '0' or > '9'))
            throw Rule("fiscal.daily_report.branch_invalid", "Daily-report branch code must contain four decimal digits.");
        return normalized;
    }

    internal static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required Daily Report evidence is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Daily Report evidence cannot exceed {max} characters.");
        return normalized;
    }

    internal static DateTimeOffset NormalizeToSecond(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

    internal static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);

    internal static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    internal static DomainRuleException Rule(string code, string message) => new(code, message);
}

/// <summary>
/// Explicit evidence that one fiscal number belongs to the Reporte Diario annulled population.
/// This evidence is never synthesized from numbering gaps.
/// </summary>
public sealed record FiscalDailyReportAnnulmentEvidence(
    string OrganizationId,
    string IssuerRuc,
    CfeFamily CfeType,
    string Series,
    long Number,
    DateOnly SummaryDate,
    FiscalDailyReportAnnulmentKind Kind,
    Guid? FiscalDocumentId,
    Guid? SignedArtifactId,
    string SourceEvidenceFingerprint,
    string EvidenceFingerprint)
{
    public static FiscalDailyReportAnnulmentEvidence Capture(
        string organizationId,
        string issuerRuc,
        CfeFamily cfeType,
        string series,
        long number,
        DateOnly summaryDate,
        FiscalDailyReportAnnulmentKind kind,
        string sourceEvidenceFingerprint,
        Guid? fiscalDocumentId = null,
        Guid? signedArtifactId = null)
    {
        var provisional = new FiscalDailyReportAnnulmentEvidence(
            FiscalDailyReportDocumentEvidence.Required(organizationId, 200, "fiscal.daily_report.organization_required"),
            FiscalDailyReportDocumentEvidence.Ruc(issuerRuc),
            cfeType,
            FiscalDailyReportDocumentEvidence.Required(series, 20, "fiscal.daily_report.series_required").ToUpperInvariant(),
            number,
            summaryDate,
            kind,
            fiscalDocumentId,
            signedArtifactId,
            FiscalDailyReportDocumentEvidence.Fingerprint(
                sourceEvidenceFingerprint,
                "fiscal.daily_report.annulment_source_evidence_invalid"),
            new string('0', 64));

        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        FiscalDailyReportDocumentEvidence.Required(OrganizationId, 200, "fiscal.daily_report.organization_required");
        FiscalDailyReportDocumentEvidence.Ruc(IssuerRuc);
        FiscalDailyReportDocumentEvidence.EnsureSupportedFamily(CfeType);
        FiscalDailyReportDocumentEvidence.Required(Series, 20, "fiscal.daily_report.series_required");
        if (Number is < 1 or > 9_999_999)
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.number_invalid", "Daily-report CFE number must be between 1 and 9999999.");
        if (!Enum.IsDefined(Kind))
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.annulment_kind_invalid", "Daily-report annulment kind is invalid.");
        if (Kind == FiscalDailyReportAnnulmentKind.DgiRejection && (!FiscalDocumentId.HasValue || !SignedArtifactId.HasValue))
        {
            throw FiscalDailyReportDocumentEvidence.Rule(
                "fiscal.daily_report.dgi_rejection_artifact_required",
                "A DGI-rejection annulment must identify the rejected fiscal document and signed artifact.");
        }
        if (FiscalDocumentId == Guid.Empty || SignedArtifactId == Guid.Empty)
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.annulment_identity_invalid", "Optional annulment identities cannot be empty GUIDs.");

        FiscalDailyReportDocumentEvidence.Fingerprint(
            SourceEvidenceFingerprint,
            "fiscal.daily_report.annulment_source_evidence_invalid");
        FiscalDailyReportDocumentEvidence.Fingerprint(
            EvidenceFingerprint,
            "fiscal.daily_report.annulment_evidence_fingerprint_invalid");
        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
        {
            throw FiscalDailyReportDocumentEvidence.Rule(
                "fiscal.daily_report.annulment_evidence_fingerprint_mismatch",
                "Daily-report annulment evidence fingerprint does not match its immutable content.");
        }
    }

    public string ComputeFingerprint()
    {
        var material = string.Join(
            "|",
            OrganizationId,
            IssuerRuc,
            ((int)CfeType).ToString(CultureInfo.InvariantCulture),
            Series,
            Number.ToString(CultureInfo.InvariantCulture),
            SummaryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ((int)Kind).ToString(CultureInfo.InvariantCulture),
            FiscalDocumentId?.ToString("N") ?? "-",
            SignedArtifactId?.ToString("N") ?? "-",
            SourceEvidenceFingerprint);
        return FiscalDailyReportDocumentEvidence.Hash(material);
    }
}

public sealed record FiscalDailyReportSummaryBucket(
    CfeFamily CfeType,
    DateOnly FiscalDate,
    string DgiBranchCode,
    bool PaymentOnBehalfOfThirdParty,
    int DocumentCount,
    decimal NetAmount,
    decimal NonTaxedAmount,
    decimal MinimumTaxableAmount,
    decimal BasicTaxableAmount,
    decimal ExportAmount,
    decimal MinimumVatAmount,
    decimal BasicVatAmount,
    decimal VatAmount,
    decimal TotalAmount);

public sealed record FiscalDailyReportNumberRange(
    string Series,
    long From,
    long To)
{
    public long Count => checked(To - From + 1);
}

public sealed record FiscalDailyReportTypeConsumption(
    CfeFamily CfeType,
    int UsedCount,
    int EmittedCount,
    int AnnulledCount,
    IReadOnlyCollection<FiscalDailyReportNumberRange> UsedRanges,
    IReadOnlyCollection<FiscalDailyReportNumberRange> AnnulledRanges);
