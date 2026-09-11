using System.Globalization;
using System.Text;
using EFactura.Domain.Taxation;

namespace EFactura.Domain.Fiscal;

/// <summary>
/// Frozen VAT-rate evidence required by Reporte Diario B-C22/B-C23.
/// The evidence is bound to the exact signed-CFE identity and immutable fiscal snapshot.
/// </summary>
public sealed record FiscalDailyReportVatRateEvidence(
    Guid FiscalDocumentId,
    CfeFamily CfeType,
    string Series,
    long Number,
    string CfeIdentityFingerprint,
    decimal? MinimumVatRatePercent,
    decimal? BasicVatRatePercent,
    string EvidenceFingerprint)
{
    public static FiscalDailyReportVatRateEvidence Capture(
        FiscalDailyReportCfeIdentityEvidence identity,
        FiscalContentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(snapshot);
        identity.EnsureIntegrity();
        snapshot.EnsureIntegrity();

        if (!string.Equals(identity.OrganizationId, snapshot.OrganizationId, StringComparison.Ordinal)
            || identity.CfeType != snapshot.CfeFamily
            || !string.Equals(identity.FiscalContentFingerprint, snapshot.ContentFingerprint, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.daily_report.wire.vat_rate_snapshot_mismatch",
                "Daily Report VAT-rate evidence must belong to the exact immutable signed-CFE snapshot.");
        }

        var minimum = ResolveRate(snapshot, VatRateKind.Minimum);
        var basic = ResolveRate(snapshot, VatRateKind.Basic);

        var provisional = new FiscalDailyReportVatRateEvidence(
            identity.FiscalDocumentId,
            identity.CfeType,
            identity.Series,
            identity.Number,
            identity.EvidenceFingerprint,
            minimum,
            basic,
            new string('0', 64));

        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        if (FiscalDocumentId == Guid.Empty)
            throw Rule("fiscal.daily_report.wire.vat_rate_document_id_required", "Daily Report VAT-rate evidence requires a fiscal-document identity.");
        FiscalDailyReportDocumentEvidence.EnsureSupportedFamily(CfeType);
        FiscalDailyReportDocumentEvidence.Required(Series, 20, "fiscal.daily_report.wire.vat_rate_series_required");
        if (Number is < 1 or > FiscalDailyReportV13_2WireContract.MaximumDocumentNumber)
            throw Rule("fiscal.daily_report.wire.vat_rate_number_invalid", "Daily Report VAT-rate evidence contains an invalid CFE number.");

        FiscalDailyReportCfeIdentityEvidence.Fingerprint(
            CfeIdentityFingerprint,
            "fiscal.daily_report.wire.vat_rate_identity_fingerprint_invalid");
        EnsureRate(MinimumVatRatePercent, "fiscal.daily_report.wire.minimum_vat_rate_invalid");
        EnsureRate(BasicVatRatePercent, "fiscal.daily_report.wire.basic_vat_rate_invalid");
        FiscalDailyReportCfeIdentityEvidence.Fingerprint(
            EvidenceFingerprint,
            "fiscal.daily_report.wire.vat_rate_evidence_fingerprint_invalid");

        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.wire.vat_rate_evidence_fingerprint_mismatch", "Daily Report VAT-rate evidence fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint() => FiscalDailyReportCfeIdentityEvidence.Hash(string.Join(
        "|",
        FiscalDocumentId.ToString("N"),
        ((int)CfeType).ToString(CultureInfo.InvariantCulture),
        Series,
        Number.ToString(CultureInfo.InvariantCulture),
        CfeIdentityFingerprint,
        MinimumVatRatePercent.HasValue ? Decimal(MinimumVatRatePercent.Value) : "-",
        BasicVatRatePercent.HasValue ? Decimal(BasicVatRatePercent.Value) : "-"));

    private static decimal? ResolveRate(FiscalContentSnapshot snapshot, VatRateKind kind)
    {
        var rates = snapshot.FiscalEvidence.Lines
            .Where(line => line.VatLiability == VatLiabilityKind.VatDue && line.VatRateKind == kind)
            .Select(line => line.AppliedRatePercent)
            .Distinct()
            .ToArray();

        if (rates.Length > 1)
            throw Rule("fiscal.daily_report.wire.vat_rate_ambiguous", "One signed CFE cannot contribute multiple rates to the same Reporte Diario VAT-rate bucket.");
        return rates.Length == 0 ? null : rates[0];
    }

    private static void EnsureRate(decimal? rate, string code)
    {
        if (!rate.HasValue)
            return;
        if (rate.Value <= 0m || rate.Value >= 1000m || DecimalScale(rate.Value) > FiscalDailyReportV13_2WireContract.VatRateDecimalDigits)
            throw Rule(code, "Reporte Diario VAT-rate evidence must be positive and fit NUM 6 (3 integer + 3 decimal digits).");
    }

    private static int DecimalScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;
    private static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    private static EFactura.Domain.Common.DomainRuleException Rule(string code, string message) =>
        FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}

/// <summary>
/// Explicit source evidence for Reporte Diario B-C27. The source calculation is intentionally
/// separate from XML serialization because the UI threshold and quotation facts must be auditable.
/// </summary>
public sealed record FiscalDailyReportHighValueCounterEvidence(
    CfeFamily CfeType,
    int Count,
    string SourceEvidenceFingerprint,
    string EvidenceFingerprint)
{
    public static FiscalDailyReportHighValueCounterEvidence Capture(
        CfeFamily cfeType,
        int count,
        string sourceEvidenceFingerprint)
    {
        FiscalDailyReportDocumentEvidence.EnsureSupportedFamily(cfeType);
        if (count < 0)
            throw Rule("fiscal.daily_report.wire.high_value_count_invalid", "Reporte Diario B-C27 cannot be negative.");
        if (!IsETicketFamily(cfeType) && count != 0)
            throw Rule("fiscal.daily_report.wire.high_value_not_applicable", "B-C27 is not applicable to the accepted e-Factura family boundary.");

        var source = FiscalDailyReportCfeIdentityEvidence.Fingerprint(
            sourceEvidenceFingerprint,
            "fiscal.daily_report.wire.high_value_source_fingerprint_invalid");
        var provisional = new FiscalDailyReportHighValueCounterEvidence(
            cfeType,
            count,
            source,
            new string('0', 64));
        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        FiscalDailyReportDocumentEvidence.EnsureSupportedFamily(CfeType);
        if (Count < 0)
            throw Rule("fiscal.daily_report.wire.high_value_count_invalid", "Reporte Diario B-C27 cannot be negative.");
        if (!IsETicketFamily(CfeType) && Count != 0)
            throw Rule("fiscal.daily_report.wire.high_value_not_applicable", "B-C27 is not applicable to the accepted e-Factura family boundary.");
        FiscalDailyReportCfeIdentityEvidence.Fingerprint(
            SourceEvidenceFingerprint,
            "fiscal.daily_report.wire.high_value_source_fingerprint_invalid");
        FiscalDailyReportCfeIdentityEvidence.Fingerprint(
            EvidenceFingerprint,
            "fiscal.daily_report.wire.high_value_evidence_fingerprint_invalid");
        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.daily_report.wire.high_value_evidence_fingerprint_mismatch", "Reporte Diario B-C27 evidence fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint() => FiscalDailyReportCfeIdentityEvidence.Hash(string.Join(
        "|",
        ((int)CfeType).ToString(CultureInfo.InvariantCulture),
        Count.ToString(CultureInfo.InvariantCulture),
        SourceEvidenceFingerprint));

    internal static bool IsETicketFamily(CfeFamily family) => family is
        CfeFamily.ETicket or CfeFamily.ETicketCreditNote or CfeFamily.ETicketDebitNote;

    private static EFactura.Domain.Common.DomainRuleException Rule(string code, string message) =>
        FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}

/// <summary>
/// Semantic wire row for B-C10..B-C25.1. This is still not XML. Concepts unsupported by the
/// current Release-1 fiscal arithmetic profile are explicitly frozen as zero rather than inferred
/// by an XML serializer.
/// </summary>
public sealed record FiscalDailyReportWireAmountRow(
    CfeFamily CfeType,
    DateOnly FiscalDate,
    string DgiBranchCode,
    bool PaymentOnBehalfOfThirdParty,
    decimal NonTaxedAmount,
    decimal ExportAmount,
    decimal PerceivedTaxAmount,
    decimal VatInSuspenseAmount,
    decimal MinimumTaxableAmount,
    decimal BasicTaxableAmount,
    decimal OtherVatTaxableAmount,
    decimal MinimumVatAmount,
    decimal BasicVatAmount,
    decimal OtherVatAmount,
    decimal? MinimumVatRatePercent,
    decimal? BasicVatRatePercent,
    decimal TotalAmount,
    decimal RetainedOrPerceivedAmount,
    decimal FiscalCreditAmount);

public sealed record FiscalDailyReportWireTypeCounters(
    CfeFamily CfeType,
    int UsedCount,
    int HighValueCount,
    int AnnulledCount,
    int EmittedCount,
    IReadOnlyCollection<FiscalDailyReportNumberRange> UsedRanges,
    IReadOnlyCollection<FiscalDailyReportNumberRange> AnnulledRanges);

public sealed record FiscalDailyReportWireProjection(
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int UsedCfeCount,
    IReadOnlyCollection<FiscalDailyReportWireAmountRow> AmountRows,
    IReadOnlyCollection<FiscalDailyReportWireTypeCounters> Counters,
    string SourceReconciliationFingerprint,
    string ProjectionFingerprint);

/// <summary>
/// Produces a deterministic semantic projection only when no unresolved wire policy is needed.
/// Foreign-currency rows with more than two decimals remain fail-closed until DGI quantization
/// semantics are authoritatively pinned.
/// </summary>
public static class FiscalDailyReportWireReadinessProjector
{
    public const string SupportedReleaseProfile = "release1-minimum-basic-export-only";

    public static FiscalDailyReportWireProjection Project(
        FiscalDailyReportSnapshot snapshot,
        IReadOnlyCollection<FiscalDailyReportVatRateEvidence> vatRateEvidence,
        IReadOnlyCollection<FiscalDailyReportHighValueCounterEvidence> highValueCounters)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(vatRateEvidence);
        ArgumentNullException.ThrowIfNull(highValueCounters);
        snapshot.EnsureIntegrity();

        if (vatRateEvidence.Any(item => item is null) || highValueCounters.Any(item => item is null))
            throw Rule("fiscal.daily_report.wire.evidence_null", "Reporte Diario wire evidence cannot contain null entries.");
        foreach (var evidence in vatRateEvidence)
            evidence.EnsureIntegrity();
        foreach (var evidence in highValueCounters)
            evidence.EnsureIntegrity();

        if (vatRateEvidence.Select(item => item.FiscalDocumentId).Distinct().Count() != vatRateEvidence.Count)
            throw Rule("fiscal.daily_report.wire.vat_rate_duplicate", "Daily Report contains duplicate VAT-rate evidence for one fiscal document.");
        if (highValueCounters.Select(item => item.CfeType).Distinct().Count() != highValueCounters.Count)
            throw Rule("fiscal.daily_report.wire.high_value_duplicate", "Daily Report contains duplicate B-C27 evidence for one CFE type.");

        var ratesByDocument = vatRateEvidence.ToDictionary(item => item.FiscalDocumentId);
        foreach (var document in snapshot.Documents)
        {
            if (!ratesByDocument.TryGetValue(document.FiscalDocumentId, out var evidence)
                || evidence.CfeType != document.CfeType
                || !string.Equals(evidence.Series, document.Series, StringComparison.Ordinal)
                || evidence.Number != document.Number)
            {
                throw Rule("fiscal.daily_report.wire.vat_rate_evidence_required", "Every emitted CFE in a wire-ready Reporte Diario requires matching VAT-rate evidence.");
            }
        }
        if (ratesByDocument.Keys.Except(snapshot.Documents.Select(item => item.FiscalDocumentId)).Any())
            throw Rule("fiscal.daily_report.wire.vat_rate_evidence_unexpected", "VAT-rate evidence contains a CFE outside the Daily Report snapshot.");

        var rows = snapshot.Summaries.Select(summary => BuildAmountRow(snapshot, summary, ratesByDocument)).ToArray();
        var highValueByType = highValueCounters.ToDictionary(item => item.CfeType);
        var counters = snapshot.Consumptions.Select(consumption =>
        {
            var highValue = 0;
            if (FiscalDailyReportHighValueCounterEvidence.IsETicketFamily(consumption.CfeType))
            {
                if (!highValueByType.TryGetValue(consumption.CfeType, out var evidence))
                    throw Rule("fiscal.daily_report.wire.high_value_evidence_required", "e-Ticket families require explicit B-C27 source evidence before wire projection.");
                if (evidence.Count > consumption.EmittedCount)
                    throw Rule("fiscal.daily_report.wire.high_value_count_exceeds_emitted", "B-C27 cannot exceed the emitted/non-rejected CFE population B-C29.");
                highValue = evidence.Count;
            }
            else if (highValueByType.TryGetValue(consumption.CfeType, out var nonApplicable) && nonApplicable.Count != 0)
            {
                throw Rule("fiscal.daily_report.wire.high_value_not_applicable", "B-C27 must not be populated for this CFE family.");
            }

            return new FiscalDailyReportWireTypeCounters(
                consumption.CfeType,
                consumption.UsedCount,
                highValue,
                consumption.AnnulledCount,
                consumption.EmittedCount,
                consumption.UsedRanges.ToArray(),
                consumption.AnnulledRanges.ToArray());
        }).ToArray();

        if (highValueByType.Keys.Except(snapshot.Consumptions.Select(item => item.CfeType)).Any())
            throw Rule("fiscal.daily_report.wire.high_value_evidence_unexpected", "B-C27 evidence contains a CFE type outside the Daily Report snapshot.");

        var provisional = new FiscalDailyReportWireProjection(
            snapshot.OrganizationId,
            snapshot.IssuerRuc,
            snapshot.SummaryDate,
            snapshot.Sequence,
            snapshot.UsedCfeCount,
            rows,
            counters,
            snapshot.ReconciliationFingerprint,
            new string('0', 64));

        return provisional with { ProjectionFingerprint = ComputeFingerprint(provisional) };
    }

    private static FiscalDailyReportWireAmountRow BuildAmountRow(
        FiscalDailyReportSnapshot snapshot,
        FiscalDailyReportSummaryBucket summary,
        IReadOnlyDictionary<Guid, FiscalDailyReportVatRateEvidence> ratesByDocument)
    {
        var documents = snapshot.Documents.Where(document =>
            document.CfeType == summary.CfeType
            && document.FiscalDate == summary.FiscalDate
            && string.Equals(document.DgiBranchCode, summary.DgiBranchCode, StringComparison.Ordinal)
            && document.PaymentOnBehalfOfThirdParty == summary.PaymentOnBehalfOfThirdParty).ToArray();

        var minimumRate = OneRate(documents.Select(document => ratesByDocument[document.FiscalDocumentId].MinimumVatRatePercent),
            summary.MinimumTaxableAmount,
            "fiscal.daily_report.wire.minimum_vat_rate_required",
            "fiscal.daily_report.wire.minimum_vat_rate_mismatch");
        var basicRate = OneRate(documents.Select(document => ratesByDocument[document.FiscalDocumentId].BasicVatRatePercent),
            summary.BasicTaxableAmount,
            "fiscal.daily_report.wire.basic_vat_rate_required",
            "fiscal.daily_report.wire.basic_vat_rate_mismatch");

        var values = new[]
        {
            summary.NonTaxedAmount,
            summary.ExportAmount,
            summary.MinimumTaxableAmount,
            summary.BasicTaxableAmount,
            summary.MinimumVatAmount,
            summary.BasicVatAmount,
            summary.TotalAmount
        };
        if (values.Any(value => DecimalScale(value) > FiscalDailyReportV13_2WireContract.MonetaryDecimalDigits))
        {
            throw Rule(
                "fiscal.daily_report.wire.quantization_required",
                "Reporte Diario contains UYU monetary evidence with more than two decimal places; no DGI quantization rule is pinned, so wire projection remains fail-closed.");
        }

        return new FiscalDailyReportWireAmountRow(
            summary.CfeType,
            summary.FiscalDate,
            summary.DgiBranchCode,
            summary.PaymentOnBehalfOfThirdParty,
            summary.NonTaxedAmount,
            summary.ExportAmount,
            0m,
            0m,
            summary.MinimumTaxableAmount,
            summary.BasicTaxableAmount,
            0m,
            summary.MinimumVatAmount,
            summary.BasicVatAmount,
            0m,
            minimumRate,
            basicRate,
            summary.TotalAmount,
            0m,
            0m);
    }

    private static decimal? OneRate(
        IEnumerable<decimal?> candidates,
        decimal taxableAmount,
        string missingCode,
        string mismatchCode)
    {
        var rates = candidates.Where(value => value.HasValue).Select(value => value!.Value).Distinct().ToArray();
        if (taxableAmount > 0m && rates.Length == 0)
            throw Rule(missingCode, "A positive Reporte Diario taxable amount requires its frozen VAT-rate evidence.");
        if (rates.Length > 1)
            throw Rule(mismatchCode, "One Reporte Diario amount row cannot carry multiple VAT rates for the same rate bucket.");
        return rates.Length == 0 ? null : rates[0];
    }

    private static string ComputeFingerprint(FiscalDailyReportWireProjection projection)
    {
        var material = new StringBuilder()
            .Append(projection.OrganizationId).Append('|')
            .Append(projection.IssuerRuc).Append('|')
            .Append(projection.SummaryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
            .Append(projection.Sequence.ToString(CultureInfo.InvariantCulture)).Append('|')
            .Append(projection.UsedCfeCount.ToString(CultureInfo.InvariantCulture)).Append('|')
            .Append(projection.SourceReconciliationFingerprint).Append('|')
            .Append(SupportedReleaseProfile);

        foreach (var row in projection.AmountRows.OrderBy(item => (int)item.CfeType)
                     .ThenBy(item => item.FiscalDate)
                     .ThenBy(item => item.DgiBranchCode, StringComparer.Ordinal)
                     .ThenBy(item => item.PaymentOnBehalfOfThirdParty))
        {
            material.Append("|ROW:")
                .Append((int)row.CfeType).Append(':')
                .Append(row.FiscalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(':')
                .Append(row.DgiBranchCode).Append(':')
                .Append(row.PaymentOnBehalfOfThirdParty ? '1' : '0').Append(':')
                .Append(Decimal(row.NonTaxedAmount)).Append(':')
                .Append(Decimal(row.ExportAmount)).Append(':')
                .Append(Decimal(row.PerceivedTaxAmount)).Append(':')
                .Append(Decimal(row.VatInSuspenseAmount)).Append(':')
                .Append(Decimal(row.MinimumTaxableAmount)).Append(':')
                .Append(Decimal(row.BasicTaxableAmount)).Append(':')
                .Append(Decimal(row.OtherVatTaxableAmount)).Append(':')
                .Append(Decimal(row.MinimumVatAmount)).Append(':')
                .Append(Decimal(row.BasicVatAmount)).Append(':')
                .Append(Decimal(row.OtherVatAmount)).Append(':')
                .Append(row.MinimumVatRatePercent.HasValue ? Decimal(row.MinimumVatRatePercent.Value) : "-").Append(':')
                .Append(row.BasicVatRatePercent.HasValue ? Decimal(row.BasicVatRatePercent.Value) : "-").Append(':')
                .Append(Decimal(row.TotalAmount)).Append(':')
                .Append(Decimal(row.RetainedOrPerceivedAmount)).Append(':')
                .Append(Decimal(row.FiscalCreditAmount));
        }

        foreach (var counter in projection.Counters.OrderBy(item => (int)item.CfeType))
        {
            material.Append("|CNT:")
                .Append((int)counter.CfeType).Append(':')
                .Append(counter.UsedCount).Append(':')
                .Append(counter.HighValueCount).Append(':')
                .Append(counter.AnnulledCount).Append(':')
                .Append(counter.EmittedCount);
            foreach (var range in counter.UsedRanges)
                material.Append(":U:").Append(range.Series).Append(':').Append(range.From).Append(':').Append(range.To);
            foreach (var range in counter.AnnulledRanges)
                material.Append(":A:").Append(range.Series).Append(':').Append(range.From).Append(':').Append(range.To);
        }

        return FiscalDailyReportCfeIdentityEvidence.Hash(material.ToString());
    }

    private static int DecimalScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;
    private static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    private static EFactura.Domain.Common.DomainRuleException Rule(string code, string message) =>
        FiscalDailyReportCfeIdentityEvidence.Rule(code, message);
}
