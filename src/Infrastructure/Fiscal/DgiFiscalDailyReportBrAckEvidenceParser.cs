using System.Xml;
using EFactura.Application.Fiscal;

namespace Infrastructure.Fiscal;

/// <summary>
/// Parses only the immediate ACKRepDiario BR rejection-reason evidence required to authorize a
/// same-SecEnvio correction. It does not interpret later DR/ER/FR response lifecycle states.
/// </summary>
public sealed class DgiFiscalDailyReportBrAckEvidenceParser : IFiscalDailyReportBrAckEvidenceParser
{
    private const string DgiCfeNamespace = "http://cfe.dgi.gub.uy";

    public FiscalDailyReportBrAckParseResult Parse(string ackXml)
    {
        if (string.IsNullOrWhiteSpace(ackXml))
            return Invalid("fiscal.daily_report.br_ack.empty");

        XmlDocument document;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var reader = XmlReader.Create(new StringReader(ackXml), settings);
            document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            document.Load(reader);
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            return Invalid("fiscal.daily_report.br_ack.xml_invalid");
        }

        var root = document.DocumentElement;
        if (root is null
            || !string.Equals(root.LocalName, "ACKRepDiario", StringComparison.Ordinal)
            || !string.Equals(root.NamespaceURI, DgiCfeNamespace, StringComparison.Ordinal))
        {
            return Invalid("fiscal.daily_report.br_ack.root_invalid");
        }

        var detail = root.SelectSingleNode("./*[local-name()='Detalle']") as XmlElement;
        var state = detail?.SelectSingleNode("./*[local-name()='Estado']")?.InnerText.Trim();
        if (!string.Equals(state, "BR", StringComparison.Ordinal))
            return Invalid("fiscal.daily_report.br_ack.state_not_rejected");

        var nodes = detail!.SelectNodes("./*[local-name()='MotivosRechazo']");
        if (nodes is null || nodes.Count == 0)
            return Invalid("fiscal.daily_report.br_ack.reasons_required");

        var reasons = new List<FiscalDailyReportRejectionReason>(nodes.Count);
        foreach (XmlNode node in nodes)
        {
            var code = node.SelectSingleNode("./*[local-name()='Motivo']")?.InnerText.Trim();
            var glosa = node.SelectSingleNode("./*[local-name()='Glosa']")?.InnerText.Trim();
            var rawDetail = node.SelectSingleNode("./*[local-name()='Detalle']")?.InnerText.Trim();
            reasons.Add(new FiscalDailyReportRejectionReason(
                code ?? string.Empty,
                glosa ?? string.Empty,
                string.IsNullOrWhiteSpace(rawDetail) ? null : rawDetail));
        }

        if (!FiscalDailyReportRejectionReasonEvidence.TryValidate(reasons, out _))
            return Invalid("fiscal.daily_report.br_ack.reasons_invalid");

        return new FiscalDailyReportBrAckParseResult(true, reasons, null);
    }

    private static FiscalDailyReportBrAckParseResult Invalid(string code) =>
        new(false, Array.Empty<FiscalDailyReportRejectionReason>(), code);
}
