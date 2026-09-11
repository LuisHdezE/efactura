using System.Globalization;

namespace EFactura.Domain.Fiscal;

/// <summary>
/// Shared current DGI threshold used by e-Ticket identification/send rules and Reporte Diario B-C27.
/// The 5,000 UI threshold applies to documents issued on or after 2022-11-01.
/// Historical threshold handling remains outside the current Release-1 boundary.
/// </summary>
public static class UruguayCfeHighValueThresholdPolicy
{
    public const decimal CurrentThresholdUi = 5_000m;
    public static readonly DateOnly CurrentThresholdEffectiveFrom = new(2022, 11, 1);
}

/// <summary>
/// Immutable annual UI quotation evidence used to evaluate the DGI 5,000 UI threshold.
/// DGI requires the UI quotation corresponding to 31 December of the previous calendar year.
/// The quotation value is source evidence only; no live lookup is performed by the domain.
/// </summary>
public sealed record FiscalDailyReportAnnualUiQuoteEvidence(
    int ApplicableYear,
    DateOnly QuoteDate,
    decimal UyuPerUi,
    decimal ThresholdUi,
    decimal ThresholdUyu,
    string SourceUri,
    string SourceArtifactFingerprint,
    string EvidenceFingerprint)
{
    public static FiscalDailyReportAnnualUiQuoteEvidence Capture(
        DateOnly reportDate,
        DateOnly quoteDate,
        decimal uyuPerUi,
        string sourceUri,
        string sourceArtifactFingerprint)
    {
        if (reportDate < UruguayCfeHighValueThresholdPolicy.CurrentThresholdEffectiveFrom)
        {
            throw Rule(
                "fiscal.daily_report.wire.high_value_historical_threshold_unsupported",
                "The current Release-1 Daily Report boundary only supports the 5,000 UI threshold effective from 2022-11-01.");
        }

        var expectedQuoteDate = new DateOnly(reportDate.Year - 1, 12, 31);
        if (quoteDate != expectedQuoteDate)
        {
            throw Rule(
                "fiscal.daily_report.wire.ui_quote_date_invalid",
                "Reporte Diario high-value evaluation requires the UI quotation corresponding to 31 December of the previous calendar year.");
        }
        if (uyuPerUi <= 0m)
            throw Rule("fiscal.daily_report.wire.ui_quote_value_invalid", "Annual UI quotation must be greater than zero.");

        var normalizedUri = NormalizeSourceUri(sourceUri);
        var sourceFingerprint = FiscalDailyReportCfeIdentityEvidence.Fingerprint(
            sourceArtifactFingerprint,
            "fiscal.daily_report.wire.ui_quote_source_fingerprint_invalid");
        var thresholdUi = UruguayCfeHighValueThresholdPolicy.CurrentThresholdUi;
        var thresholdUyu = checked(thresholdUi * uyuPerUi);

        var provisional = new FiscalDailyReportAnnualUiQuoteEvidence(
            reportDate.Year,
            quoteDate,
            uyuPerUi,
            thresholdUi,
            thresholdUyu,
            normalizedUri,
            sourceFingerprint,
            new string('0', 64));
        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureAppliesTo(DateOnly reportDate)
    {
        EnsureIntegrity();
        if (reportDate.Year != ApplicableYear)
            throw Rule("fiscal.daily_report.wire.ui_quote_year_mismatch", "Annual UI quotation evidence belongs to a different Reporte Diario year.");
        if (QuoteDate != new DateOnly(reportDate.Year - 1, 12, 31))
            throw Rule("fiscal.daily_report.wire.ui_quote_date_invalid", "Annual UI quotation date does not match the report year.");
    }

    public void EnsureIntegrity()
    {
        if (ApplicableYear < 2022)
            throw Rule("fiscal.daily_report.wire.ui_quote_year_invalid", "Annual UI quotation evidence contains an invalid applicable year.");
        if (QuoteDate != new DateOnly(ApplicableYear - 1, 12, 31))
            throw Rule("fiscal.daily_report.wire.ui_quote_date_invalid", "Annual UI quotation must correspond to 31 December of the previous calendar year.");
        if (UyuPerUi <= 0m)
            throw Rule("fiscal.daily_report.wire.ui_quote_value_invalid", "Annual UI quotation must be greater than zero.");
        if (ThresholdUi != UruguayCfeHighValueThresholdPolicy.CurrentThresholdUi)
            throw Rule("fiscal.daily_report.wire.ui_threshold_mismatch", "Annual UI quotation evidence must use the current supported 5,000 UI threshold.");
        if (ThresholdUyu != checked(ThresholdUi * UyuPerUi))
            throw Rule("fiscal.daily_report.wire.ui_threshold_uyu_mismatch", "Annual UI quotation UYU threshold does not match its frozen quotation.");

        NormalizeSourceUri(SourceUri);
        FiscalDailyReportCfeIdentityEvidence.Fingerprint(
            SourceArtifactFingerprint,
            "fiscal.daily_report.wire.ui_quote_source_fingerprint_invalid");
        FiscalDailyReportCfeIdentityEvidence.Fingerprint(
            EvidenceFingerprint,
            "fiscal.daily_report.wire.ui_quote_evidence_fingerprint_invalid");
        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.wire.ui_quote_evidence_fingerprint_mismatch", "Annual UI quotation evidence fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint() => FiscalDailyReportCfeIdentityEvidence.Hash(string.Join(
        "|",
        ApplicableYear.ToString(CultureInfo.InvariantCulture),
        QuoteDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Decimal(UyuPerUi),
        Decimal(ThresholdUi),
        Decimal(ThresholdUyu),
        SourceUri,
        SourceArtifactFingerprint));

    private static string NormalizeSourceUri(string value)
    {
        var normalized = FiscalDailyReportDocumentEvidence.Required(
            value,
            500,
            "fiscal.daily_report.wire.ui_quote_source_uri_required");
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw Rule("fiscal.daily_report.wire.ui_quote_source_uri_invalid", "Annual UI quotation evidence requires an absolute HTTPS source URI.");
        return uri.AbsoluteUri;
    }

    private static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    private static EFactura.Domain.Common.DomainRuleException Rule(string code, string message) =>
        FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}

/// <summary>
/// Deterministically derives Reporte Diario B-C27 evidence from emitted/not-rejected e-Ticket
/// document evidence and one frozen annual UI quotation. Only original UYU CFE are automated in
/// this slice. Foreign/UYI CFE remain fail-closed until their threshold-conversion semantics are
/// explicitly pinned from DGI material.
/// </summary>
public static class FiscalDailyReportHighValueCounterComposer
{
    public static IReadOnlyCollection<FiscalDailyReportHighValueCounterEvidence> Capture(
        FiscalDailyReportSnapshot snapshot,
        FiscalDailyReportAnnualUiQuoteEvidence annualUiQuote)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(annualUiQuote);
        snapshot.EnsureIntegrity();
        annualUiQuote.EnsureAppliesTo(snapshot.SummaryDate);

        var counters = new List<FiscalDailyReportHighValueCounterEvidence>();
        foreach (var consumption in snapshot.Consumptions
                     .Where(item => FiscalDailyReportHighValueCounterEvidence.IsETicketFamily(item.CfeType))
                     .OrderBy(item => (int)item.CfeType))
        {
            var documents = snapshot.Documents
                .Where(document => document.CfeType == consumption.CfeType)
                .OrderBy(document => document.Series, StringComparer.Ordinal)
                .ThenBy(document => document.Number)
                .ToArray();

            if (documents.Any(document => !string.Equals(
                    document.OriginalCurrencyCode,
                    FiscalDailyReportDocumentEvidence.DailyReportCurrencyCode,
                    StringComparison.Ordinal)))
            {
                throw Rule(
                    "fiscal.daily_report.wire.high_value_foreign_currency_policy_required",
                    "Automatic B-C27 evaluation currently requires original UYU CFE; DGI threshold conversion semantics for other currencies are not pinned.");
            }

            var count = documents.Count(document => document.NetAmount > annualUiQuote.ThresholdUyu);
            var sourceFingerprint = BuildSourceFingerprint(
                snapshot,
                annualUiQuote,
                consumption.CfeType,
                documents);
            counters.Add(FiscalDailyReportHighValueCounterEvidence.Capture(
                consumption.CfeType,
                count,
                sourceFingerprint));
        }

        return counters;
    }

    private static string BuildSourceFingerprint(
        FiscalDailyReportSnapshot snapshot,
        FiscalDailyReportAnnualUiQuoteEvidence annualUiQuote,
        CfeFamily cfeType,
        IReadOnlyCollection<FiscalDailyReportDocumentEvidence> documents)
    {
        var material = new System.Text.StringBuilder()
            .Append(snapshot.ReconciliationFingerprint).Append('|')
            .Append(annualUiQuote.EvidenceFingerprint).Append('|')
            .Append((int)cfeType);
        foreach (var document in documents)
            material.Append('|').Append(document.EvidenceFingerprint);
        return FiscalDailyReportCfeIdentityEvidence.Hash(material.ToString());
    }

    private static EFactura.Domain.Common.DomainRuleException Rule(string code, string message) =>
        FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}
