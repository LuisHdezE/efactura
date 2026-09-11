namespace EFactura.Domain.Fiscal;

/// <summary>
/// Explicit foreign-currency composition boundary for Reporte Diario.
/// The ordinary source-fact composer remains UYU-only and fail-closed so an unconverted CFE cannot
/// accidentally enter monetary reconciliation. This path requires a previously frozen signed-CFE
/// identity plus immutable FX evidence and emits only UYU monetary partitions.
/// </summary>
public static class FiscalDailyReportForeignCurrencyComposer
{
    public static FiscalDailyReportDocumentEvidence CaptureEmittedDocument(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        FiscalDailyReportCfeIdentityEvidence identity,
        FiscalDailyReportDgiOutcomeEvidence dgiOutcome,
        FiscalDailyReportThirdPartyPaymentEvidence thirdPartyPayment,
        FiscalDailyReportCurrencyConversionEvidence currencyConversion)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(dgiOutcome);
        ArgumentNullException.ThrowIfNull(thirdPartyPayment);
        ArgumentNullException.ThrowIfNull(currencyConversion);

        identity.EnsureIntegrity();
        dgiOutcome.EnsureIntegrity();
        thirdPartyPayment.EnsureIntegrity();
        currencyConversion.EnsureIntegrity();

        if (string.Equals(identity.CurrencyCode, FiscalDailyReportDocumentEvidence.DailyReportCurrencyCode, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.source_facts.foreign_currency_required",
                "The explicit FX composer only accepts CFE whose original currency is not UYU.");
        }

        if (!dgiOutcome.IsEligibleForEmittedPopulation)
        {
            throw Rule(
                "fiscal.daily_report.source_facts.received_outcome_required",
                "Only the current authoritative AE/received DGI outcome may contribute to the emitted/not-rejected population.");
        }

        var recaptured = FiscalDailyReportCfeIdentityEvidence.Capture(
            document,
            snapshot,
            identity.SignedArtifactId,
            identity.SigningEvidenceId,
            identity.SigningTimestamp,
            identity.FiscalContentFingerprint,
            identity.SignedContentHash);
        if (!string.Equals(recaptured.EvidenceFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.source_facts.identity_input_mismatch",
                "Frozen source identity does not match the supplied fiscal document/snapshot inputs.");
        }

        if (!string.Equals(dgiOutcome.CfeIdentityFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.source_facts.outcome_identity_mismatch",
                "DGI outcome evidence belongs to a different signed CFE identity.");
        }

        if (!string.Equals(thirdPartyPayment.CfeIdentityFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.source_facts.third_party_identity_mismatch",
                "A-C19 evidence belongs to a different signed CFE identity.");
        }

        if (!string.Equals(currencyConversion.CfeIdentityFingerprint, identity.EvidenceFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.source_facts.fx_identity_mismatch",
                "Currency-conversion evidence belongs to a different signed CFE identity.");
        }

        return FiscalDailyReportDocumentEvidence.CaptureConverted(
            document,
            snapshot,
            identity,
            currencyConversion,
            dgiOutcome.EvidenceFingerprint,
            thirdPartyPayment.PaymentOnBehalfOfThirdParty,
            thirdPartyPayment.EvidenceFingerprint);
    }

    private static EFactura.Domain.Common.DomainRuleException Rule(string code, string message) =>
        FiscalDailyReportDocumentEvidence.Rule(code, message);
}
