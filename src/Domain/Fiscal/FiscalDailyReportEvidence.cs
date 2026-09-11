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
/// Monetary partitions are always expressed in UYU. For a foreign-currency source CFE the original
/// currency, conversion-evidence fingerprint and reliquidation marker remain frozen alongside the
/// converted amounts so provenance is not lost during reconciliation.
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
    string OriginalCurrencyCode,
    string ReportingCurrencyCode,
    string? CurrencyConversionEvidenceFingerprint,
    bool CurrencyConversionRequiresReliquidation,
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
    public const string DailyReportCurrencyCode = "UYU";

    /// <summary>
    /// Existing UYU-only boundary. Foreign-currency callers must use the explicit typed FX composer.
    /// </summary>
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
        ValidateDocumentSnapshot(document, snapshot);

        if (signedArtifactId == Guid.Empty)
            throw Rule("fiscal.daily_report.signed_artifact_id_required", "Daily-report evidence requires a signed artifact id.");
        if (signingEvidenceId == Guid.Empty)
            throw Rule("fiscal.daily_report.signing_evidence_id_required", "Daily-report evidence requires signing evidence.");

        var artifactFingerprint = Fingerprint(
            artifactFiscalContentFingerprint,
            "fiscal.daily_report.artifact_snapshot_fingerprint_invalid");
        if (!string.Equals(artifactFingerprint, snapshot.ContentFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.artifact_snapshot_fingerprint_mismatch",
                "Signed artifact fiscal-content fingerprint does not match the immutable snapshot.");
        }

        var sourceCurrency = Currency(document.CurrencyCode);
        if (!string.Equals(sourceCurrency, DailyReportCurrencyCode, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.foreign_currency_conversion_evidence_required",
                "Daily-report monetary evidence must be in UYU; foreign-currency CFE requires separately frozen fiscal exchange-rate evidence.");
        }

        var amounts = AmountsFromSnapshot(snapshot);
        return Build(
            document,
            snapshot,
            signedArtifactId,
            signingEvidenceId,
            signingTimestamp,
            artifactFingerprint,
            signedContentHash,
            reportingStatusEvidenceFingerprint,
            paymentOnBehalfOfThirdParty,
            thirdPartyPaymentEvidenceFingerprint,
            sourceCurrency,
            currencyConversionEvidenceFingerprint: null,
            currencyConversionRequiresReliquidation: false,
            amounts);
    }

    /// <summary>
    /// Typed foreign-currency path used only after signed-CFE identity and fiscal FX evidence have
    /// already been frozen. The conversion rate is never accepted as an unbound primitive here.
    /// </summary>
    internal static FiscalDailyReportDocumentEvidence CaptureConverted(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        FiscalDailyReportCfeIdentityEvidence identity,
        FiscalDailyReportCurrencyConversionEvidence conversion,
        string reportingStatusEvidenceFingerprint,
        bool paymentOnBehalfOfThirdParty,
        string thirdPartyPaymentEvidenceFingerprint)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(conversion);
        identity.EnsureIntegrity();
        conversion.EnsureIntegrity();
        ValidateDocumentSnapshot(document, snapshot);

        if (identity.FiscalDocumentId != document.Id
            || !string.Equals(identity.OrganizationId, document.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(identity.IssuerRuc, snapshot.Issuer.Ruc, StringComparison.Ordinal)
            || identity.CfeType != document.CfeType
            || !string.Equals(identity.Series, document.Series, StringComparison.Ordinal)
            || identity.Number != document.Number
            || identity.FiscalDate != document.FiscalDate
            || !string.Equals(identity.CurrencyCode, Currency(document.CurrencyCode), StringComparison.Ordinal)
            || !string.Equals(identity.FiscalContentFingerprint, snapshot.ContentFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.fx_identity_input_mismatch",
                "Typed Daily Report FX identity does not match the supplied fiscal document and immutable snapshot.");
        }

        if (string.Equals(identity.CurrencyCode, DailyReportCurrencyCode, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.fx_not_required_for_uyu",
                "UYU CFE must use the ordinary Daily Report evidence path without currency conversion.");
        }

        if (!string.Equals(conversion.CfeIdentityFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal)
            || conversion.CfeType != identity.CfeType
            || conversion.FiscalDate != identity.FiscalDate
            || !string.Equals(conversion.CurrencyCode, identity.CurrencyCode, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.fx_evidence_identity_mismatch",
                "Currency-conversion evidence belongs to a different signed CFE identity.");
        }

        var sourceAmounts = AmountsFromSnapshot(snapshot);
        var convertedAmounts = new MonetaryAmounts(
            conversion.ConvertToUyu(sourceAmounts.NetAmount),
            conversion.ConvertToUyu(sourceAmounts.NonTaxedAmount),
            conversion.ConvertToUyu(sourceAmounts.MinimumTaxableAmount),
            conversion.ConvertToUyu(sourceAmounts.BasicTaxableAmount),
            conversion.ConvertToUyu(sourceAmounts.ExportAmount),
            conversion.ConvertToUyu(sourceAmounts.MinimumVatAmount),
            conversion.ConvertToUyu(sourceAmounts.BasicVatAmount),
            conversion.ConvertToUyu(sourceAmounts.VatAmount),
            conversion.ConvertToUyu(sourceAmounts.TotalAmount));

        return Build(
            document,
            snapshot,
            identity.SignedArtifactId,
            identity.SigningEvidenceId,
            identity.SigningTimestamp,
            identity.FiscalContentFingerprint,
            identity.SignedContentHash,
            reportingStatusEvidenceFingerprint,
            paymentOnBehalfOfThirdParty,
            thirdPartyPaymentEvidenceFingerprint,
            identity.CurrencyCode,
            conversion.EvidenceFingerprint,
            conversion.RequiresReliquidation,
            convertedAmounts);
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

        var originalCurrency = Currency(OriginalCurrencyCode);
        var reportingCurrency = Currency(ReportingCurrencyCode);
        if (!string.Equals(reportingCurrency, DailyReportCurrencyCode, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.reporting_currency_invalid",
                "Daily Report monetary evidence must always be expressed in UYU.");
        }

        if (string.Equals(originalCurrency, DailyReportCurrencyCode, StringComparison.Ordinal))
        {
            if (CurrencyConversionEvidenceFingerprint is not null)
                throw Rule("fiscal.daily_report.fx_evidence_forbidden_for_uyu", "UYU Daily Report evidence must not carry a currency-conversion fingerprint.");
            if (CurrencyConversionRequiresReliquidation)
                throw Rule("fiscal.daily_report.fx_reliquidation_forbidden_for_uyu", "UYU Daily Report evidence cannot require FX reliquidation.");
        }
        else
        {
            if (CurrencyConversionEvidenceFingerprint is null)
                throw Rule("fiscal.daily_report.fx_evidence_required", "Foreign-currency Daily Report evidence requires a frozen conversion-evidence fingerprint.");
            Fingerprint(CurrencyConversionEvidenceFingerprint, "fiscal.daily_report.fx_evidence_invalid");
        }

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
            OriginalCurrencyCode,
            ReportingCurrencyCode,
            CurrencyConversionEvidenceFingerprint ?? "-",
            CurrencyConversionRequiresReliquidation ? "1" : "0",
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

    private static FiscalDailyReportDocumentEvidence Build(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        Guid signedArtifactId,
        Guid signingEvidenceId,
        DateTimeOffset signingTimestamp,
        string artifactFiscalContentFingerprint,
        string signedContentHash,
        string reportingStatusEvidenceFingerprint,
        bool paymentOnBehalfOfThirdParty,
        string thirdPartyPaymentEvidenceFingerprint,
        string originalCurrencyCode,
        string? currencyConversionEvidenceFingerprint,
        bool currencyConversionRequiresReliquidation,
        MonetaryAmounts amounts)
    {
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
            Fingerprint(thirdPartyPaymentEvidenceFingerprint, "fiscal.daily_report.third_party_payment_evidence_invalid"),
            NormalizeToSecond(signingTimestamp),
            Fingerprint(artifactFiscalContentFingerprint, "fiscal.daily_report.content_fingerprint_invalid"),
            Fingerprint(signedContentHash, "fiscal.daily_report.signed_content_hash_invalid"),
            Fingerprint(reportingStatusEvidenceFingerprint, "fiscal.daily_report.reporting_status_evidence_invalid"),
            Currency(originalCurrencyCode),
            DailyReportCurrencyCode,
            currencyConversionEvidenceFingerprint is null
                ? null
                : Fingerprint(currencyConversionEvidenceFingerprint, "fiscal.daily_report.fx_evidence_invalid"),
            currencyConversionRequiresReliquidation,
            amounts.NetAmount,
            amounts.NonTaxedAmount,
            amounts.MinimumTaxableAmount,
            amounts.BasicTaxableAmount,
            amounts.ExportAmount,
            amounts.MinimumVatAmount,
            amounts.BasicVatAmount,
            amounts.VatAmount,
            amounts.TotalAmount,
            new string('0', 64));

        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    private static MonetaryAmounts AmountsFromSnapshot(FiscalContentSnapshot snapshot)
    {
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

        return new MonetaryAmounts(
            totals.NetAmount,
            nonTaxed,
            totals.MinimumTaxableAmount,
            totals.BasicTaxableAmount,
            totals.ExportAmount,
            totals.MinimumVatAmount,
            totals.BasicVatAmount,
            totals.VatAmount,
            totals.TotalAmount);
    }

    private static void ValidateDocumentSnapshot(FiscalDocument document, FiscalContentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(snapshot);
        snapshot.EnsureIntegrity();
        EnsureSupportedFamily(document.CfeType);

        if (!string.Equals(document.OrganizationId, snapshot.OrganizationId, StringComparison.Ordinal)
            || document.SaleId != snapshot.SaleId
            || document.CfeType != snapshot.CfeFamily
            || !string.Equals(document.FormatVersion, snapshot.FormatVersion, StringComparison.Ordinal)
            || !string.Equals(document.ConfirmationFingerprint, snapshot.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(document.SettlementFingerprint, snapshot.SettlementFingerprint, StringComparison.Ordinal)
            || !string.Equals(Currency(document.CurrencyCode), Currency(snapshot.FiscalEvidence.CurrencyCode), StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.identity_snapshot_mismatch",
                "Fiscal document identity and immutable content snapshot do not match.");
        }
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

    internal static string Currency(string value)
    {
        var normalized = Required(value, 3, "fiscal.daily_report.currency_required").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("fiscal.daily_report.currency_invalid", "Daily-report currency must use three uppercase alphabetic characters.");
        return normalized;
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

    private sealed record MonetaryAmounts(
        decimal NetAmount,
        decimal NonTaxedAmount,
        decimal MinimumTaxableAmount,
        decimal BasicTaxableAmount,
        decimal ExportAmount,
        decimal MinimumVatAmount,
        decimal BasicVatAmount,
        decimal VatAmount,
        decimal TotalAmount);
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