using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EFactura.Domain.Common;

namespace EFactura.Domain.Fiscal;

/// <summary>
/// Current authoritative DGI reception outcome for one CFE as represented by Mensaje de Respuesta v19.
/// A later DGI response may supersede an earlier one, so callers must provide the current authoritative
/// outcome at reconciliation time rather than treating an old AE response as permanently final.
/// </summary>
public enum FiscalDailyReportDgiCfeOutcome
{
    Received = 1,
    Rejected = 2
}

/// <summary>
/// Why a non-UYU amount is converted with a particular rate for Reporte Diario purposes.
/// This enum intentionally models only rules evidenced for the currently accepted domestic
/// 101/102/103/111/112/113 boundary.
/// </summary>
public enum FiscalDailyReportFxRateSource
{
    BcuFiscalQuotation = 1,
    CfeRateNoBcuQuotation = 2,
    CorrectionCfeRate = 3,
    FutureDateCfeRate = 4
}

/// <summary>
/// Immutable identity tying Reporte Diario source facts to one signed CFE artifact.
/// </summary>
public sealed record FiscalDailyReportCfeIdentityEvidence(
    Guid FiscalDocumentId,
    Guid SignedArtifactId,
    Guid SigningEvidenceId,
    string OrganizationId,
    string IssuerRuc,
    CfeFamily CfeType,
    string Series,
    long Number,
    DateOnly FiscalDate,
    string CurrencyCode,
    DateTimeOffset SigningTimestamp,
    string FiscalContentFingerprint,
    string SignedContentHash,
    string EvidenceFingerprint)
{
    public static FiscalDailyReportCfeIdentityEvidence Capture(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        Guid signedArtifactId,
        Guid signingEvidenceId,
        DateTimeOffset signingTimestamp,
        string signedContentHash)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(snapshot);

        snapshot.EnsureIntegrity();
        FiscalDailyReportDocumentEvidence.EnsureSupportedFamily(document.CfeType);

        if (signedArtifactId == Guid.Empty)
            throw Rule("fiscal.daily_report.source_identity.signed_artifact_id_required", "Daily-report source identity requires a signed artifact id.");
        if (signingEvidenceId == Guid.Empty)
            throw Rule("fiscal.daily_report.source_identity.signing_evidence_id_required", "Daily-report source identity requires signing evidence.");

        if (!string.Equals(document.OrganizationId, snapshot.OrganizationId, StringComparison.Ordinal)
            || document.SaleId != snapshot.SaleId
            || document.CfeType != snapshot.CfeFamily
            || !string.Equals(document.FormatVersion, snapshot.FormatVersion, StringComparison.Ordinal)
            || !string.Equals(document.ConfirmationFingerprint, snapshot.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(document.SettlementFingerprint, snapshot.SettlementFingerprint, StringComparison.Ordinal)
            || !string.Equals(document.CurrencyCode, snapshot.FiscalEvidence.CurrencyCode, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.source_identity.snapshot_mismatch",
                "Fiscal document identity and immutable content snapshot do not match for Daily Report source evidence.");
        }

        var provisional = new FiscalDailyReportCfeIdentityEvidence(
            document.Id,
            signedArtifactId,
            signingEvidenceId,
            document.OrganizationId,
            snapshot.Issuer.Ruc,
            document.CfeType,
            document.Series,
            document.Number,
            document.FiscalDate,
            Currency(document.CurrencyCode),
            FiscalDailyReportDocumentEvidence.NormalizeToSecond(signingTimestamp),
            Fingerprint(snapshot.ContentFingerprint, "fiscal.daily_report.source_identity.content_fingerprint_invalid"),
            Fingerprint(signedContentHash, "fiscal.daily_report.source_identity.signed_content_hash_invalid"),
            new string('0', 64));

        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        if (FiscalDocumentId == Guid.Empty || SignedArtifactId == Guid.Empty || SigningEvidenceId == Guid.Empty)
            throw Rule("fiscal.daily_report.source_identity.identity_invalid", "Daily-report source identity contains an empty durable identity.");

        FiscalDailyReportDocumentEvidence.Required(OrganizationId, 200, "fiscal.daily_report.source_identity.organization_required");
        FiscalDailyReportDocumentEvidence.Ruc(IssuerRuc);
        FiscalDailyReportDocumentEvidence.EnsureSupportedFamily(CfeType);
        FiscalDailyReportDocumentEvidence.Required(Series, 20, "fiscal.daily_report.source_identity.series_required");
        if (Number is < 1 or > 9_999_999)
            throw Rule("fiscal.daily_report.source_identity.number_invalid", "Daily-report source identity CFE number must be between 1 and 9999999.");
        Currency(CurrencyCode);
        if (SigningTimestamp == default)
            throw Rule("fiscal.daily_report.source_identity.signing_timestamp_required", "Daily-report source identity requires the CFE advanced-signature timestamp.");

        Fingerprint(FiscalContentFingerprint, "fiscal.daily_report.source_identity.content_fingerprint_invalid");
        Fingerprint(SignedContentHash, "fiscal.daily_report.source_identity.signed_content_hash_invalid");
        Fingerprint(EvidenceFingerprint, "fiscal.daily_report.source_identity.evidence_fingerprint_invalid");

        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.source_identity.evidence_fingerprint_mismatch", "Daily-report source identity fingerprint does not match its immutable content.");
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
            CurrencyCode,
            SigningTimestamp.ToString("O", CultureInfo.InvariantCulture),
            FiscalContentFingerprint,
            SignedContentHash);
        return Hash(material);
    }

    internal static string Currency(string value)
    {
        var normalized = FiscalDailyReportDocumentEvidence.Required(
            value,
            3,
            "fiscal.daily_report.source_identity.currency_required").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("fiscal.daily_report.source_identity.currency_invalid", "Daily-report source identity currency must use three uppercase alphabetic characters.");
        return normalized;
    }

    internal static string Fingerprint(string value, string code) =>
        FiscalDailyReportDocumentEvidence.Fingerprint(value, code);

    internal static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    internal static DomainRuleException Rule(string code, string message) => new(code, message);
}

/// <summary>
/// Frozen current DGI CFE reception outcome. Mensaje de Respuesta v19 uses AE for received CFE and
/// BE for rejected CFE. SupersedesEvidenceFingerprint allows an immutable chain when a later DGI
/// response replaces an earlier known outcome.
/// </summary>
public sealed record FiscalDailyReportDgiOutcomeEvidence(
    string CfeIdentityFingerprint,
    DateTimeOffset CfeSigningTimestamp,
    FiscalDailyReportDgiCfeOutcome Outcome,
    string DgiStatusCode,
    DateTimeOffset ObservedAtUtc,
    string ResponseArtifactFingerprint,
    string? SupersedesEvidenceFingerprint,
    string EvidenceFingerprint)
{
    public bool IsEligibleForEmittedPopulation => Outcome == FiscalDailyReportDgiCfeOutcome.Received;
    public bool IsDgiRejection => Outcome == FiscalDailyReportDgiCfeOutcome.Rejected;

    public static FiscalDailyReportDgiOutcomeEvidence Capture(
        FiscalDailyReportCfeIdentityEvidence identity,
        FiscalDailyReportDgiCfeOutcome outcome,
        DateTimeOffset observedAt,
        string responseArtifactFingerprint,
        string? supersedesEvidenceFingerprint = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        identity.EnsureIntegrity();

        if (!Enum.IsDefined(outcome))
            throw Rule("fiscal.daily_report.dgi_outcome.invalid", "Daily-report DGI CFE outcome is invalid.");

        var signingTimestamp = identity.SigningTimestamp.ToUniversalTime();
        var observedAtUtc = FiscalDailyReportDocumentEvidence.NormalizeToSecond(observedAt.ToUniversalTime());
        if (observedAtUtc < signingTimestamp)
            throw Rule("fiscal.daily_report.dgi_outcome.observed_before_signing", "DGI outcome evidence cannot predate the signed CFE.");

        var provisional = new FiscalDailyReportDgiOutcomeEvidence(
            identity.EvidenceFingerprint,
            signingTimestamp,
            outcome,
            StatusCode(outcome),
            observedAtUtc,
            Fingerprint(responseArtifactFingerprint, "fiscal.daily_report.dgi_outcome.response_fingerprint_invalid"),
            string.IsNullOrWhiteSpace(supersedesEvidenceFingerprint)
                ? null
                : Fingerprint(supersedesEvidenceFingerprint, "fiscal.daily_report.dgi_outcome.supersedes_fingerprint_invalid"),
            new string('0', 64));

        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        Fingerprint(CfeIdentityFingerprint, "fiscal.daily_report.dgi_outcome.identity_fingerprint_invalid");
        if (!Enum.IsDefined(Outcome))
            throw Rule("fiscal.daily_report.dgi_outcome.invalid", "Daily-report DGI CFE outcome is invalid.");
        if (!string.Equals(DgiStatusCode, StatusCode(Outcome), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.dgi_outcome.status_code_mismatch", "DGI status code does not match the frozen CFE outcome.");
        if (CfeSigningTimestamp == default || ObservedAtUtc == default)
            throw Rule("fiscal.daily_report.dgi_outcome.timestamp_required", "DGI outcome evidence requires signing and observation timestamps.");
        if (ObservedAtUtc < CfeSigningTimestamp)
            throw Rule("fiscal.daily_report.dgi_outcome.observed_before_signing", "DGI outcome evidence cannot predate the signed CFE.");

        Fingerprint(ResponseArtifactFingerprint, "fiscal.daily_report.dgi_outcome.response_fingerprint_invalid");
        if (SupersedesEvidenceFingerprint is not null)
            Fingerprint(SupersedesEvidenceFingerprint, "fiscal.daily_report.dgi_outcome.supersedes_fingerprint_invalid");
        Fingerprint(EvidenceFingerprint, "fiscal.daily_report.dgi_outcome.evidence_fingerprint_invalid");

        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.dgi_outcome.evidence_fingerprint_mismatch", "Daily-report DGI outcome fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint()
    {
        var material = string.Join(
            "|",
            CfeIdentityFingerprint,
            CfeSigningTimestamp.ToString("O", CultureInfo.InvariantCulture),
            ((int)Outcome).ToString(CultureInfo.InvariantCulture),
            DgiStatusCode,
            ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            ResponseArtifactFingerprint,
            SupersedesEvidenceFingerprint ?? "-");
        return Hash(material);
    }

    private static string StatusCode(FiscalDailyReportDgiCfeOutcome outcome) => outcome switch
    {
        FiscalDailyReportDgiCfeOutcome.Received => "AE",
        FiscalDailyReportDgiCfeOutcome.Rejected => "BE",
        _ => throw Rule("fiscal.daily_report.dgi_outcome.invalid", "Daily-report DGI CFE outcome is invalid.")
    };

    private static string Fingerprint(string value, string code) => FiscalDailyReportCfeIdentityEvidence.Fingerprint(value, code);
    private static string Hash(string value) => FiscalDailyReportCfeIdentityEvidence.Hash(value);
    private static DomainRuleException Rule(string code, string message) => FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}

/// <summary>
/// Exact source semantics for CFE field A-C19. The DGI value 1 means pagos por cuenta de terceros;
/// null means the field was absent. Any other numeric value is rejected instead of being coerced to false.
/// </summary>
public sealed record FiscalDailyReportThirdPartyPaymentEvidence(
    string CfeIdentityFingerprint,
    int? A19Indicator,
    string SourceArtifactFingerprint,
    string EvidenceFingerprint)
{
    public bool PaymentOnBehalfOfThirdParty => A19Indicator == 1;

    public static FiscalDailyReportThirdPartyPaymentEvidence Capture(
        FiscalDailyReportCfeIdentityEvidence identity,
        int? a19Indicator,
        string sourceArtifactFingerprint)
    {
        ArgumentNullException.ThrowIfNull(identity);
        identity.EnsureIntegrity();

        EnsureIndicator(a19Indicator);
        var provisional = new FiscalDailyReportThirdPartyPaymentEvidence(
            identity.EvidenceFingerprint,
            a19Indicator,
            Fingerprint(sourceArtifactFingerprint, "fiscal.daily_report.third_party.source_fingerprint_invalid"),
            new string('0', 64));
        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        Fingerprint(CfeIdentityFingerprint, "fiscal.daily_report.third_party.identity_fingerprint_invalid");
        EnsureIndicator(A19Indicator);
        Fingerprint(SourceArtifactFingerprint, "fiscal.daily_report.third_party.source_fingerprint_invalid");
        Fingerprint(EvidenceFingerprint, "fiscal.daily_report.third_party.evidence_fingerprint_invalid");
        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.third_party.evidence_fingerprint_mismatch", "Daily-report A-C19 evidence fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint() => Hash(string.Join(
        "|",
        CfeIdentityFingerprint,
        A19Indicator?.ToString(CultureInfo.InvariantCulture) ?? "-",
        SourceArtifactFingerprint));

    private static void EnsureIndicator(int? value)
    {
        if (value is not null and not 1)
            throw Rule("fiscal.daily_report.third_party.a19_invalid", "DGI CFE A-C19 may only be absent or contain the value 1.");
    }

    private static string Fingerprint(string value, string code) => FiscalDailyReportCfeIdentityEvidence.Fingerprint(value, code);
    private static string Hash(string value) => FiscalDailyReportCfeIdentityEvidence.Hash(value);
    private static DomainRuleException Rule(string code, string message) => FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}

/// <summary>
/// Frozen provenance for conversion of one non-UYU CFE into UYU for Reporte Diario reconciliation.
/// The exact rate is preserved without rounding; wire-format rounding/precision belongs to the later
/// Reporte Diario serializer contract.
/// </summary>
public sealed record FiscalDailyReportCurrencyConversionEvidence(
    string CfeIdentityFingerprint,
    CfeFamily CfeType,
    DateOnly FiscalDate,
    string CurrencyCode,
    FiscalDailyReportFxRateSource RateSource,
    decimal RateToUyu,
    DateOnly RateSourceDate,
    string SourceEvidenceFingerprint,
    bool RequiresReliquidation,
    string EvidenceFingerprint)
{
    public static FiscalDailyReportCurrencyConversionEvidence Capture(
        FiscalDailyReportCfeIdentityEvidence identity,
        FiscalDailyReportFxRateSource rateSource,
        decimal rateToUyu,
        DateOnly rateSourceDate,
        string sourceEvidenceFingerprint,
        bool requiresReliquidation = false)
    {
        ArgumentNullException.ThrowIfNull(identity);
        identity.EnsureIntegrity();

        if (string.Equals(identity.CurrencyCode, "UYU", StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.fx.not_required_for_uyu", "UYU CFE does not require Daily Report currency-conversion evidence.");
        if (!Enum.IsDefined(rateSource))
            throw Rule("fiscal.daily_report.fx.source_invalid", "Daily-report currency-conversion source is invalid.");
        if (rateToUyu <= 0m)
            throw Rule("fiscal.daily_report.fx.rate_invalid", "Daily-report conversion rate to UYU must be positive.");

        EnsureSourceRule(identity.CfeType, rateSource, requiresReliquidation);
        if (rateSource == FiscalDailyReportFxRateSource.BcuFiscalQuotation && rateSourceDate > identity.FiscalDate)
            throw Rule("fiscal.daily_report.fx.bcu_rate_date_invalid", "BCU fiscal quotation source date cannot be after the CFE fiscal date.");

        var provisional = new FiscalDailyReportCurrencyConversionEvidence(
            identity.EvidenceFingerprint,
            identity.CfeType,
            identity.FiscalDate,
            identity.CurrencyCode,
            rateSource,
            rateToUyu,
            rateSourceDate,
            Fingerprint(sourceEvidenceFingerprint, "fiscal.daily_report.fx.source_fingerprint_invalid"),
            requiresReliquidation,
            new string('0', 64));
        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public decimal ConvertToUyu(decimal amount)
    {
        if (amount < 0m)
            throw Rule("fiscal.daily_report.fx.amount_invalid", "Daily-report conversion amount cannot be negative.");
        return checked(amount * RateToUyu);
    }

    public void EnsureIntegrity()
    {
        Fingerprint(CfeIdentityFingerprint, "fiscal.daily_report.fx.identity_fingerprint_invalid");
        FiscalDailyReportDocumentEvidence.EnsureSupportedFamily(CfeType);
        FiscalDailyReportCfeIdentityEvidence.Currency(CurrencyCode);
        if (string.Equals(CurrencyCode, "UYU", StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.fx.not_required_for_uyu", "UYU CFE does not require Daily Report currency-conversion evidence.");
        if (!Enum.IsDefined(RateSource))
            throw Rule("fiscal.daily_report.fx.source_invalid", "Daily-report currency-conversion source is invalid.");
        if (RateToUyu <= 0m)
            throw Rule("fiscal.daily_report.fx.rate_invalid", "Daily-report conversion rate to UYU must be positive.");
        EnsureSourceRule(CfeType, RateSource, RequiresReliquidation);
        if (RateSource == FiscalDailyReportFxRateSource.BcuFiscalQuotation && RateSourceDate > FiscalDate)
            throw Rule("fiscal.daily_report.fx.bcu_rate_date_invalid", "BCU fiscal quotation source date cannot be after the CFE fiscal date.");

        Fingerprint(SourceEvidenceFingerprint, "fiscal.daily_report.fx.source_fingerprint_invalid");
        Fingerprint(EvidenceFingerprint, "fiscal.daily_report.fx.evidence_fingerprint_invalid");
        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.fx.evidence_fingerprint_mismatch", "Daily-report conversion evidence fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint() => Hash(string.Join(
        "|",
        CfeIdentityFingerprint,
        ((int)CfeType).ToString(CultureInfo.InvariantCulture),
        FiscalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        CurrencyCode,
        ((int)RateSource).ToString(CultureInfo.InvariantCulture),
        Decimal(RateToUyu),
        RateSourceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        SourceEvidenceFingerprint,
        RequiresReliquidation ? "1" : "0"));

    private static void EnsureSourceRule(
        CfeFamily family,
        FiscalDailyReportFxRateSource source,
        bool requiresReliquidation)
    {
        var correction = family is
            CfeFamily.ETicketCreditNote or
            CfeFamily.ETicketDebitNote or
            CfeFamily.EFacturaCreditNote or
            CfeFamily.EFacturaDebitNote;

        if (correction && source != FiscalDailyReportFxRateSource.CorrectionCfeRate)
            throw Rule("fiscal.daily_report.fx.correction_rate_source_required", "Domestic correction notes must freeze the exchange rate from the correction CFE for Daily Report conversion.");
        if (!correction && source == FiscalDailyReportFxRateSource.CorrectionCfeRate)
            throw Rule("fiscal.daily_report.fx.correction_rate_source_forbidden", "Correction-CFE exchange-rate source cannot be used for a non-correction CFE.");

        if (source == FiscalDailyReportFxRateSource.FutureDateCfeRate && !requiresReliquidation)
            throw Rule("fiscal.daily_report.fx.reliquidation_marker_required", "Future-date CFE exchange-rate fallback must preserve that later Reporte Diario reliquidation may be required.");
        if (source != FiscalDailyReportFxRateSource.FutureDateCfeRate && requiresReliquidation)
            throw Rule("fiscal.daily_report.fx.reliquidation_marker_invalid", "Reliquidation marker is only valid for the future-date CFE exchange-rate fallback rule.");
    }

    private static string Fingerprint(string value, string code) => FiscalDailyReportCfeIdentityEvidence.Fingerprint(value, code);
    private static string Hash(string value) => FiscalDailyReportCfeIdentityEvidence.Hash(value);
    private static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    private static DomainRuleException Rule(string code, string message) => FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}

/// <summary>
/// Composition boundary that replaces raw status/A-C19 fingerprints with typed immutable evidence
/// wherever the current reconciliation model can preserve those facts losslessly.
/// </summary>
public static class FiscalDailyReportSourceFactComposer
{
    public static FiscalDailyReportDocumentEvidence CaptureEmittedDocument(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        FiscalDailyReportCfeIdentityEvidence identity,
        FiscalDailyReportDgiOutcomeEvidence dgiOutcome,
        FiscalDailyReportThirdPartyPaymentEvidence thirdPartyPayment,
        FiscalDailyReportCurrencyConversionEvidence? currencyConversion = null)
    {
        ValidateBindings(document, snapshot, identity, dgiOutcome, thirdPartyPayment, currencyConversion);

        if (!dgiOutcome.IsEligibleForEmittedPopulation)
            throw Rule("fiscal.daily_report.source_facts.received_outcome_required", "Only the current authoritative AE/received DGI outcome may contribute to the emitted/not-rejected population.");

        if (!string.Equals(identity.CurrencyCode, "UYU", StringComparison.Ordinal))
        {
            if (currencyConversion is null)
                throw Rule("fiscal.daily_report.source_facts.currency_conversion_required", "Non-UYU CFE requires frozen Daily Report currency-conversion evidence.");

            throw Rule(
                "fiscal.daily_report.source_facts.foreign_currency_integration_required",
                "Foreign-currency source facts are frozen, but the current Daily Report document evidence cannot yet preserve the conversion fingerprint losslessly.");
        }

        if (currencyConversion is not null)
            throw Rule("fiscal.daily_report.source_facts.currency_conversion_forbidden_for_uyu", "UYU CFE must not carry Daily Report currency-conversion evidence.");

        return FiscalDailyReportDocumentEvidence.Capture(
            document,
            snapshot,
            identity.SignedArtifactId,
            identity.SigningEvidenceId,
            identity.SigningTimestamp,
            identity.FiscalContentFingerprint,
            identity.SignedContentHash,
            dgiOutcome.EvidenceFingerprint,
            thirdPartyPayment.PaymentOnBehalfOfThirdParty,
            thirdPartyPayment.EvidenceFingerprint);
    }

    public static FiscalDailyReportAnnulmentEvidence CaptureDgiRejectedAnnulment(
        FiscalDailyReportCfeIdentityEvidence identity,
        FiscalDailyReportDgiOutcomeEvidence dgiOutcome)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(dgiOutcome);
        identity.EnsureIntegrity();
        dgiOutcome.EnsureIntegrity();

        if (!string.Equals(dgiOutcome.CfeIdentityFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.source_facts.outcome_identity_mismatch", "DGI outcome evidence belongs to a different signed CFE identity.");
        if (!dgiOutcome.IsDgiRejection)
            throw Rule("fiscal.daily_report.source_facts.rejected_outcome_required", "DGI-rejection annulment requires the current authoritative BE/rejected outcome.");

        var summaryDate = new DateOnly(
            identity.SigningTimestamp.Year,
            identity.SigningTimestamp.Month,
            identity.SigningTimestamp.Day);

        return FiscalDailyReportAnnulmentEvidence.Capture(
            identity.OrganizationId,
            identity.IssuerRuc,
            identity.CfeType,
            identity.Series,
            identity.Number,
            summaryDate,
            FiscalDailyReportAnnulmentKind.DgiRejection,
            dgiOutcome.EvidenceFingerprint,
            identity.FiscalDocumentId,
            identity.SignedArtifactId);
    }

    private static void ValidateBindings(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        FiscalDailyReportCfeIdentityEvidence identity,
        FiscalDailyReportDgiOutcomeEvidence dgiOutcome,
        FiscalDailyReportThirdPartyPaymentEvidence thirdPartyPayment,
        FiscalDailyReportCurrencyConversionEvidence? currencyConversion)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(dgiOutcome);
        ArgumentNullException.ThrowIfNull(thirdPartyPayment);

        identity.EnsureIntegrity();
        dgiOutcome.EnsureIntegrity();
        thirdPartyPayment.EnsureIntegrity();
        currencyConversion?.EnsureIntegrity();

        var recaptured = FiscalDailyReportCfeIdentityEvidence.Capture(
            document,
            snapshot,
            identity.SignedArtifactId,
            identity.SigningEvidenceId,
            identity.SigningTimestamp,
            identity.SignedContentHash);
        if (!string.Equals(recaptured.EvidenceFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.source_facts.identity_input_mismatch", "Frozen source identity does not match the supplied fiscal document/snapshot inputs.");

        if (!string.Equals(dgiOutcome.CfeIdentityFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.source_facts.outcome_identity_mismatch", "DGI outcome evidence belongs to a different signed CFE identity.");
        if (!string.Equals(thirdPartyPayment.CfeIdentityFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.source_facts.third_party_identity_mismatch", "A-C19 evidence belongs to a different signed CFE identity.");
        if (currencyConversion is not null
            && !string.Equals(currencyConversion.CfeIdentityFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
        {
            throw Rule("fiscal.daily_report.source_facts.fx_identity_mismatch", "Currency-conversion evidence belongs to a different signed CFE identity.");
        }
    }

    private static DomainRuleException Rule(string code, string message) => FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}
