using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record UnsignedDailyReportArtifact(
    string FormatVersion,
    string ProjectionFingerprint,
    DateTimeOffset SigningTimestamp,
    string Xml,
    string ContentHash);

public interface IFiscalDailyReportXmlBuilder
{
    UnsignedDailyReportArtifact Build(
        FiscalDailyReportWireProjection projection,
        DateTimeOffset signingTimestamp);
}

/// <summary>
/// Deterministically materializes the current Release-1 Reporte Diario v13.2 semantic projection
/// as the XML payload that precedes XMLDSig. The required final ds:Signature is deliberately absent.
/// No mutable fiscal master, certificate, private key, persistence adapter or DGI transport is read.
/// </summary>
public sealed class DeterministicUnsignedDailyReportXmlBuilder : IFiscalDailyReportXmlBuilder
{
    private static readonly XNamespace CfeNamespace = FiscalDailyReportV13_2WireContract.XmlNamespace;
    private static readonly DateOnly MinimumSummaryDate = new(2011, 10, 1);
    private static readonly DateOnly MaximumSummaryDate = new(2050, 12, 31);
    private static readonly CfeFamily[] SupportedOrder =
    [
        CfeFamily.ETicket,
        CfeFamily.ETicketCreditNote,
        CfeFamily.ETicketDebitNote,
        CfeFamily.EFactura,
        CfeFamily.EFacturaCreditNote,
        CfeFamily.EFacturaDebitNote
    ];

    public UnsignedDailyReportArtifact Build(
        FiscalDailyReportWireProjection projection,
        DateTimeOffset signingTimestamp)
    {
        ArgumentNullException.ThrowIfNull(projection);
        EnsureProjectionShape(projection);

        if (signingTimestamp.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw Rule(
                "fiscal.daily_report.xml.signing_timestamp_precision_invalid",
                "Reporte Diario signing timestamp must already be frozen at whole-second precision before XML materialization.");
        }

        var root = new XElement(
            CfeNamespace + FiscalDailyReportV13_2WireContract.XmlRootElementName,
            BuildCaratula(projection, signingTimestamp));

        var countersByType = projection.Counters.ToDictionary(counter => counter.CfeType);
        foreach (var family in SupportedOrder)
        {
            if (!countersByType.TryGetValue(family, out var counter))
                continue;

            var rows = projection.AmountRows
                .Where(row => row.CfeType == family)
                .OrderBy(row => row.FiscalDate)
                .ThenBy(row => row.DgiBranchCode, StringComparer.Ordinal)
                .ThenBy(row => row.PaymentOnBehalfOfThirdParty)
                .ToArray();
            root.Add(BuildSummary(counter, rows));
        }

        var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), root)
            .ToString(SaveOptions.DisableFormatting);
        return new UnsignedDailyReportArtifact(
            FiscalDailyReportV13_2WireContract.Version,
            projection.ProjectionFingerprint,
            signingTimestamp,
            xml,
            Sha256(xml));
    }

    private static XElement BuildCaratula(
        FiscalDailyReportWireProjection projection,
        DateTimeOffset signingTimestamp) =>
        new(
            CfeNamespace + FiscalDailyReportV13_2WireContract.XmlCaratulaElementName,
            new XAttribute("version", FiscalDailyReportV13_2WireContract.XmlCaratulaVersion),
            Element("RUCEmisor", projection.IssuerRuc),
            Element("FechaResumen", Date(projection.SummaryDate)),
            Element("SecEnvio", projection.Sequence),
            Element("TmstFirmaEnv", signingTimestamp.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)),
            Element("CantComprobantes", projection.UsedCfeCount));

    private static XElement BuildSummary(
        FiscalDailyReportWireTypeCounters counter,
        IReadOnlyCollection<FiscalDailyReportWireAmountRow> rows)
    {
        var data = new XElement(CfeNamespace + "RsmnData");
        if (rows.Count != 0)
            data.Add(new XElement(CfeNamespace + "Montos", rows.Select(BuildAmountRow)));

        data.Add(Element("CantDocsUtil", counter.UsedCount));
        if (IsETicketFamily(counter.CfeType))
            data.Add(Element("CantDocsMay_topeUI", counter.HighValueCount));
        data.Add(
            Element("CantDocsAnulados", counter.AnnulledCount),
            Element("CantDocsEmi", counter.EmittedCount));

        if (counter.UsedRanges.Count != 0)
            data.Add(BuildRanges("RngDocsUtil", "RDU_Item", counter.UsedRanges));
        if (counter.AnnulledRanges.Count != 0)
            data.Add(BuildRanges("RngDocsAnulados", "RDA_Item", counter.AnnulledRanges));

        return new XElement(
            CfeNamespace + SummaryElement(counter.CfeType),
            Element("TipoComp", (int)counter.CfeType),
            data);
    }

    private static XElement BuildAmountRow(FiscalDailyReportWireAmountRow row)
    {
        var item = new XElement(
            CfeNamespace + "Mnts_FyT_Item",
            Element("Fecha", Date(row.FiscalDate)),
            Element("CodSuc", Branch(row.DgiBranchCode)));

        if (row.PaymentOnBehalfOfThirdParty)
            item.Add(Element("IndPagCta3ros", FiscalDailyReportV13_2WireContract.ThirdPartyPaymentIndicator));

        item.Add(
            Money("TotMntNoGrv", row.NonTaxedAmount),
            Money("TotMntExpyAsim", row.ExportAmount),
            Money("TotMntImpPerc", row.PerceivedTaxAmount),
            Money("TotMntIVAenSusp", row.VatInSuspenseAmount),
            Money("TotMntIVATasaMin", row.MinimumTaxableAmount),
            Money("TotMntIVATasaBas", row.BasicTaxableAmount),
            Money("TotMntIVAOtra", row.OtherVatTaxableAmount),
            Money("MntIVATasaMin", row.MinimumVatAmount),
            Money("MntIVATasaBas", row.BasicVatAmount),
            Money("MntIVAOtra", row.OtherVatAmount));

        if (row.MinimumVatRatePercent.HasValue)
            item.Add(Rate("IVATasaMin", row.MinimumVatRatePercent.Value));
        if (row.BasicVatRatePercent.HasValue)
            item.Add(Rate("IVATasaBas", row.BasicVatRatePercent.Value));

        item.Add(
            Money("TotMntTotal", row.TotalAmount),
            Money("TotMntRetenido", row.RetainedOrPerceivedAmount),
            Money("TotMntCredFisc", row.FiscalCreditAmount));
        return item;
    }

    private static XElement BuildRanges(
        string containerName,
        string itemName,
        IEnumerable<FiscalDailyReportNumberRange> ranges) =>
        new(
            CfeNamespace + containerName,
            ranges.OrderBy(range => range.Series, StringComparer.Ordinal)
                .ThenBy(range => range.From)
                .Select(range => new XElement(
                    CfeNamespace + itemName,
                    Element("Serie", range.Series),
                    Element("NroDesde", range.From),
                    Element("NroHasta", range.To))));

    private static void EnsureProjectionShape(FiscalDailyReportWireProjection projection)
    {
        if (projection.SummaryDate < MinimumSummaryDate || projection.SummaryDate > MaximumSummaryDate)
            throw Rule("fiscal.daily_report.xml.summary_date_invalid", "Reporte Diario summary date is outside the pinned XSD range.");
        if (projection.Sequence is < 1 or > 99)
            throw Rule("fiscal.daily_report.xml.sequence_invalid", "Reporte Diario sequence must fit the pinned two-digit XSD field.");
        if (projection.UsedCfeCount < 0)
            throw Rule("fiscal.daily_report.xml.used_count_invalid", "Reporte Diario total used-CFE count cannot be negative.");
        if (projection.AmountRows is null || projection.Counters is null)
            throw Rule("fiscal.daily_report.xml.collections_required", "Reporte Diario wire projection collections are required.");
        if (projection.AmountRows.Any(row => row is null) || projection.Counters.Any(counter => counter is null))
            throw Rule("fiscal.daily_report.xml.entry_invalid", "Reporte Diario wire projection cannot contain null entries.");
        if (projection.Counters.Select(counter => counter.CfeType).Distinct().Count() != projection.Counters.Count)
            throw Rule("fiscal.daily_report.xml.counter_duplicate", "Reporte Diario wire projection contains duplicate type counters.");
        if (projection.Counters.Any(counter => !SupportedOrder.Contains(counter.CfeType))
            || projection.AmountRows.Any(row => !SupportedOrder.Contains(row.CfeType)))
        {
            throw Rule("fiscal.daily_report.xml.family_not_supported", "Unsigned Reporte Diario Release-1 XML supports only 101/102/103/111/112/113.");
        }

        var counterTypes = projection.Counters.Select(counter => counter.CfeType).ToHashSet();
        if (projection.AmountRows.Any(row => !counterTypes.Contains(row.CfeType)))
            throw Rule("fiscal.daily_report.xml.amount_without_counter", "Every Reporte Diario amount row requires its matching type counter.");
        if (projection.UsedCfeCount == 0 && (projection.Counters.Count != 0 || projection.AmountRows.Count != 0))
            throw Rule("fiscal.daily_report.xml.zero_report_not_empty", "A zero-count Reporte Diario must omit the Resumen zone.");
        if (projection.UsedCfeCount > 0 && projection.Counters.Count == 0)
            throw Rule("fiscal.daily_report.xml.summary_required", "A non-empty Reporte Diario requires at least one Resumen type.");

        foreach (var counter in projection.Counters)
        {
            if (counter.UsedCount < 0 || counter.AnnulledCount < 0 || counter.EmittedCount < 0 || counter.HighValueCount < 0
                || counter.UsedCount != counter.AnnulledCount + counter.EmittedCount)
            {
                throw Rule("fiscal.daily_report.xml.counter_invalid", "Reporte Diario counters do not satisfy the accepted C26/C28/C29 invariant.");
            }
            if (counter.HighValueCount > counter.EmittedCount
                || (!IsETicketFamily(counter.CfeType) && counter.HighValueCount != 0))
            {
                throw Rule("fiscal.daily_report.xml.high_value_invalid", "Reporte Diario B-C27 is invalid for the projected CFE family/count.");
            }
        }
    }

    private static string SummaryElement(CfeFamily family) => family switch
    {
        CfeFamily.ETicket => "Rsmn_Tck",
        CfeFamily.ETicketCreditNote => "Rsmn_Tck_Nota_Credito",
        CfeFamily.ETicketDebitNote => "Rsmn_Tck_Nota_Debito",
        CfeFamily.EFactura => "Rsmn_Fac",
        CfeFamily.EFacturaCreditNote => "Rsmn_Fac_Nota_Credito",
        CfeFamily.EFacturaDebitNote => "Rsmn_Fac_Nota_Debito",
        _ => throw Rule("fiscal.daily_report.xml.family_not_supported", "CFE family is not supported by the unsigned Reporte Diario serializer.")
    };

    private static bool IsETicketFamily(CfeFamily family) => family is
        CfeFamily.ETicket or CfeFamily.ETicketCreditNote or CfeFamily.ETicketDebitNote;

    private static int Branch(string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var branch)
            && branch is >= 0 and <= 9999
            ? branch
            : throw Rule("fiscal.daily_report.xml.branch_invalid", "Reporte Diario branch code must fit the pinned four-digit XSD type.");

    private static XElement Money(string name, decimal value) =>
        new(CfeNamespace + name, value.ToString("0.00", CultureInfo.InvariantCulture));

    private static XElement Rate(string name, decimal value) =>
        new(CfeNamespace + name, value.ToString("0.###", CultureInfo.InvariantCulture));

    private static XElement Element(string name, object value) => new(CfeNamespace + name, value);
    private static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
