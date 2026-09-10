using System.Globalization;
using System.Text;

namespace EFactura.Domain.Fiscal;

/// <summary>
/// Immutable internal reconciliation snapshot for the DGI Reporte Diario foundation.
///
/// This is deliberately not the final DGI v13.2 XML artifact. It preserves deterministic evidence
/// needed to build that artifact later without rereading mutable fiscal masters or inferring DGI
/// outcomes. Report XML serialization, advanced signature, persistence and submission are separate
/// gated capabilities.
/// </summary>
public sealed record FiscalDailyReportSnapshot(
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    string FormatVersion,
    int UsedCfeCount,
    IReadOnlyCollection<FiscalDailyReportDocumentEvidence> Documents,
    IReadOnlyCollection<FiscalDailyReportAnnulmentEvidence> Annulments,
    IReadOnlyCollection<FiscalDailyReportSummaryBucket> Summaries,
    IReadOnlyCollection<FiscalDailyReportTypeConsumption> Consumptions,
    string ReconciliationFingerprint)
{
    public const string CurrentFormatVersion = "13.2";
    private const int MaximumRangeRepetitions = 50_000;

    public static FiscalDailyReportSnapshot Create(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        IReadOnlyCollection<FiscalDailyReportDocumentEvidence>? documents = null,
        IReadOnlyCollection<FiscalDailyReportAnnulmentEvidence>? annulments = null)
    {
        if (sequence <= 0)
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.sequence_invalid", "Daily Report sequence must be positive.");

        var frozenDocuments = (documents ?? Array.Empty<FiscalDailyReportDocumentEvidence>()).ToArray();
        var frozenAnnulments = (annulments ?? Array.Empty<FiscalDailyReportAnnulmentEvidence>()).ToArray();

        var provisional = new FiscalDailyReportSnapshot(
            FiscalDailyReportDocumentEvidence.Required(organizationId, 200, "fiscal.daily_report.organization_required"),
            FiscalDailyReportDocumentEvidence.Ruc(issuerRuc),
            summaryDate,
            sequence,
            CurrentFormatVersion,
            checked(frozenDocuments.Length + frozenAnnulments.Length),
            frozenDocuments,
            frozenAnnulments,
            BuildSummaries(frozenDocuments),
            BuildConsumptions(frozenDocuments, frozenAnnulments),
            new string('0', 64));

        var result = provisional with { ReconciliationFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        FiscalDailyReportDocumentEvidence.Required(OrganizationId, 200, "fiscal.daily_report.organization_required");
        FiscalDailyReportDocumentEvidence.Ruc(IssuerRuc);
        if (Sequence <= 0)
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.sequence_invalid", "Daily Report sequence must be positive.");
        if (!string.Equals(FormatVersion, CurrentFormatVersion, StringComparison.Ordinal))
        {
            throw FiscalDailyReportDocumentEvidence.Rule(
                "fiscal.daily_report.format_version_invalid",
                $"Daily Report foundation is pinned to format {CurrentFormatVersion}.");
        }

        if (Documents is null || Annulments is null || Summaries is null || Consumptions is null)
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.collections_required", "Daily Report collections cannot be null.");

        foreach (var document in Documents)
        {
            if (document is null)
                throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.document_invalid", "Daily Report contains null document evidence.");
            document.EnsureIntegrity();
            if (!string.Equals(document.OrganizationId, OrganizationId, StringComparison.Ordinal)
                || !string.Equals(document.IssuerRuc, IssuerRuc, StringComparison.Ordinal))
            {
                throw FiscalDailyReportDocumentEvidence.Rule(
                    "fiscal.daily_report.document_scope_mismatch",
                    "Daily-report document evidence belongs to a different organization or issuer RUC.");
            }

            var signingDate = DateOnly.FromDateTime(document.SigningTimestamp.Date);
            if (signingDate != SummaryDate)
            {
                throw FiscalDailyReportDocumentEvidence.Rule(
                    "fiscal.daily_report.signing_date_mismatch",
                    "Reported CFE advanced-signature date must equal the Daily Report summary date.");
            }
        }

        foreach (var annulment in Annulments)
        {
            if (annulment is null)
                throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.annulment_invalid", "Daily Report contains null annulment evidence.");
            annulment.EnsureIntegrity();
            if (!string.Equals(annulment.OrganizationId, OrganizationId, StringComparison.Ordinal)
                || !string.Equals(annulment.IssuerRuc, IssuerRuc, StringComparison.Ordinal)
                || annulment.SummaryDate != SummaryDate)
            {
                throw FiscalDailyReportDocumentEvidence.Rule(
                    "fiscal.daily_report.annulment_scope_mismatch",
                    "Daily-report annulment evidence belongs to a different organization, issuer or summary date.");
            }
        }

        var emittedKeys = Documents.Select(document => Identity(document.CfeType, document.Series, document.Number)).ToArray();
        var annulledKeys = Annulments.Select(annulment => Identity(annulment.CfeType, annulment.Series, annulment.Number)).ToArray();

        if (emittedKeys.Distinct().Count() != emittedKeys.Length)
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.document_duplicate", "Daily Report contains a duplicated emitted CFE identity.");
        if (annulledKeys.Distinct().Count() != annulledKeys.Length)
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.annulment_duplicate", "Daily Report contains a duplicated annulled CFE identity.");
        if (emittedKeys.Intersect(annulledKeys).Any())
        {
            throw FiscalDailyReportDocumentEvidence.Rule(
                "fiscal.daily_report.emitted_annulled_overlap",
                "One CFE identity cannot be reported simultaneously as emitted/non-rejected and annulled.");
        }

        if (UsedCfeCount != Documents.Count + Annulments.Count)
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.used_count_mismatch", "Daily Report used-CFE count is inconsistent with its frozen evidence.");

        var expectedSummaries = BuildSummaries(Documents);
        if (!Summaries.SequenceEqual(expectedSummaries))
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.summary_mismatch", "Daily Report monetary summaries do not match frozen CFE evidence.");

        var expectedConsumptions = BuildConsumptions(Documents, Annulments);
        if (!SameConsumptions(Consumptions, expectedConsumptions))
            throw FiscalDailyReportDocumentEvidence.Rule("fiscal.daily_report.consumption_mismatch", "Daily Report numbering consumption does not match frozen evidence.");

        if (Consumptions.SelectMany(consumption => consumption.UsedRanges).Count() > MaximumRangeRepetitions
            || Consumptions.SelectMany(consumption => consumption.AnnulledRanges).Count() > MaximumRangeRepetitions)
        {
            throw FiscalDailyReportDocumentEvidence.Rule(
                "fiscal.daily_report.range_repetition_limit_exceeded",
                "Daily Report numbering-range repetitions exceed the v13.2 foundation limit.");
        }

        FiscalDailyReportDocumentEvidence.Fingerprint(
            ReconciliationFingerprint,
            "fiscal.daily_report.reconciliation_fingerprint_invalid");
        if (!string.Equals(ReconciliationFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
        {
            throw FiscalDailyReportDocumentEvidence.Rule(
                "fiscal.daily_report.reconciliation_fingerprint_mismatch",
                "Daily Report reconciliation fingerprint does not match its immutable evidence.");
        }
    }

    public string ComputeFingerprint()
    {
        var material = new StringBuilder()
            .Append(OrganizationId).Append('|')
            .Append(IssuerRuc).Append('|')
            .Append(SummaryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
            .Append(Sequence.ToString(CultureInfo.InvariantCulture)).Append('|')
            .Append(FormatVersion).Append('|')
            .Append(UsedCfeCount.ToString(CultureInfo.InvariantCulture));

        foreach (var document in Documents.OrderBy(document => (int)document.CfeType)
                     .ThenBy(document => document.Series, StringComparer.Ordinal)
                     .ThenBy(document => document.Number))
        {
            material.Append("|DOC:").Append(document.EvidenceFingerprint);
        }

        foreach (var annulment in Annulments.OrderBy(annulment => (int)annulment.CfeType)
                     .ThenBy(annulment => annulment.Series, StringComparer.Ordinal)
                     .ThenBy(annulment => annulment.Number))
        {
            material.Append("|ANN:").Append(annulment.EvidenceFingerprint);
        }

        return FiscalDailyReportDocumentEvidence.Hash(material.ToString());
    }

    private static FiscalDailyReportSummaryBucket[] BuildSummaries(
        IEnumerable<FiscalDailyReportDocumentEvidence> documents) =>
        documents
            .GroupBy(document => new
            {
                document.CfeType,
                document.FiscalDate,
                document.DgiBranchCode,
                document.PaymentOnBehalfOfThirdParty
            })
            .Select(group => new FiscalDailyReportSummaryBucket(
                group.Key.CfeType,
                group.Key.FiscalDate,
                group.Key.DgiBranchCode,
                group.Key.PaymentOnBehalfOfThirdParty,
                group.Count(),
                group.Sum(document => document.NetAmount),
                group.Sum(document => document.NonTaxedAmount),
                group.Sum(document => document.MinimumTaxableAmount),
                group.Sum(document => document.BasicTaxableAmount),
                group.Sum(document => document.ExportAmount),
                group.Sum(document => document.MinimumVatAmount),
                group.Sum(document => document.BasicVatAmount),
                group.Sum(document => document.VatAmount),
                group.Sum(document => document.TotalAmount)))
            .OrderBy(summary => (int)summary.CfeType)
            .ThenBy(summary => summary.FiscalDate)
            .ThenBy(summary => summary.DgiBranchCode, StringComparer.Ordinal)
            .ThenBy(summary => summary.PaymentOnBehalfOfThirdParty)
            .ToArray();

    private static FiscalDailyReportTypeConsumption[] BuildConsumptions(
        IEnumerable<FiscalDailyReportDocumentEvidence> documents,
        IEnumerable<FiscalDailyReportAnnulmentEvidence> annulments)
    {
        var documentArray = documents.ToArray();
        var annulmentArray = annulments.ToArray();

        var families = documentArray.Select(document => document.CfeType)
            .Concat(annulmentArray.Select(annulment => annulment.CfeType))
            .Distinct()
            .OrderBy(family => (int)family)
            .ToArray();

        return families.Select(family =>
        {
            var emitted = documentArray
                .Where(document => document.CfeType == family)
                .Select(document => new NumberUse(document.Series, document.Number))
                .ToArray();
            var cancelled = annulmentArray
                .Where(annulment => annulment.CfeType == family)
                .Select(annulment => new NumberUse(annulment.Series, annulment.Number))
                .ToArray();
            var used = emitted.Concat(cancelled).ToArray();

            return new FiscalDailyReportTypeConsumption(
                family,
                used.Length,
                emitted.Length,
                cancelled.Length,
                CollapseRanges(used),
                CollapseRanges(cancelled));
        }).ToArray();
    }

    private static FiscalDailyReportNumberRange[] CollapseRanges(IEnumerable<NumberUse> uses)
    {
        var ranges = new List<FiscalDailyReportNumberRange>();
        foreach (var seriesGroup in uses
                     .GroupBy(use => use.Series, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var numbers = seriesGroup.Select(use => use.Number).Distinct().OrderBy(number => number).ToArray();
            if (numbers.Length == 0)
                continue;

            var from = numbers[0];
            var to = from;
            for (var index = 1; index < numbers.Length; index++)
            {
                if (numbers[index] == to + 1)
                {
                    to = numbers[index];
                    continue;
                }

                ranges.Add(new FiscalDailyReportNumberRange(seriesGroup.Key, from, to));
                from = to = numbers[index];
            }
            ranges.Add(new FiscalDailyReportNumberRange(seriesGroup.Key, from, to));
        }
        return ranges.ToArray();
    }

    private static bool SameConsumptions(
        IReadOnlyCollection<FiscalDailyReportTypeConsumption> actual,
        IReadOnlyCollection<FiscalDailyReportTypeConsumption> expected)
    {
        if (actual.Count != expected.Count)
            return false;

        var orderedActual = actual.OrderBy(item => (int)item.CfeType).ToArray();
        var orderedExpected = expected.OrderBy(item => (int)item.CfeType).ToArray();

        for (var index = 0; index < orderedActual.Length; index++)
        {
            var left = orderedActual[index];
            var right = orderedExpected[index];
            if (left.CfeType != right.CfeType
                || left.UsedCount != right.UsedCount
                || left.EmittedCount != right.EmittedCount
                || left.AnnulledCount != right.AnnulledCount
                || !left.UsedRanges.SequenceEqual(right.UsedRanges)
                || !left.AnnulledRanges.SequenceEqual(right.AnnulledRanges))
            {
                return false;
            }
        }

        return true;
    }

    private static (CfeFamily Family, string Series, long Number) Identity(
        CfeFamily family,
        string series,
        long number) => (family, series, number);

    private sealed record NumberUse(string Series, long Number);
}
