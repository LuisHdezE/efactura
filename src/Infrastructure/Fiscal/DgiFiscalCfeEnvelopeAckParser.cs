using System.Xml;
using EFactura.Application.Fiscal;

namespace Infrastructure.Fiscal;

/// <summary>
/// Parses the immediate DGI ACKSobre response into typed, non-mutating evidence. This parser
/// validates structure and correlation fields only. Cryptographic verification of the DGI XMLDSig
/// remains a separate governed capability.
/// </summary>
public sealed class DgiFiscalCfeEnvelopeAckParser : IFiscalCfeEnvelopeAckParser
{
    private const string DgiNamespace = "http://cfe.dgi.gub.uy";
    private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    public FiscalCfeEnvelopeAckParseResult Parse(string responseXml)
    {
        if (string.IsNullOrWhiteSpace(responseXml))
            return Invalid("fiscal.envelope.ack.empty");

        XmlDocument document;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var reader = XmlReader.Create(new StringReader(responseXml), settings);
            document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            document.Load(reader);
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            return Invalid("fiscal.envelope.ack.xml_invalid");
        }

        var root = document.DocumentElement;
        if (root is null
            || !string.Equals(root.LocalName, "ACKSobre", StringComparison.Ordinal)
            || !string.Equals(root.NamespaceURI, DgiNamespace, StringComparison.Ordinal))
        {
            return Invalid("fiscal.envelope.ack.root_invalid");
        }

        var caratulas = DirectChildren(root, "Caratula", DgiNamespace);
        var detalles = DirectChildren(root, "Detalle", DgiNamespace);
        var signatures = DirectChildren(root, "Signature", XmlDsigNamespace);
        if (caratulas.Count != 1 || detalles.Count != 1 || signatures.Count != 1)
            return Invalid("fiscal.envelope.ack.structure_invalid");

        var caratula = caratulas[0];
        var detalle = detalles[0];

        var receiverRut = RequiredText(caratula, "RUCReceptor");
        var issuerRuc = RequiredText(caratula, "RUCEmisor");
        var receptionTimestamp = RequiredText(caratula, "FecHRecibido");
        var signingTimestamp = RequiredText(caratula, "Tmst");
        if (!TwelveDigits(receiverRut)
            || !TwelveDigits(issuerRuc)
            || string.IsNullOrWhiteSpace(receptionTimestamp)
            || string.IsNullOrWhiteSpace(signingTimestamp))
        {
            return Invalid("fiscal.envelope.ack.caratula_invalid");
        }

        if (!TryRequiredLong(caratula, "IDRespuesta", out var responseId)
            || !TryRequiredLong(caratula, "IdEmisor", out var senderEnvelopeId)
            || !TryRequiredLong(caratula, "IDReceptor", out var receiverId)
            || !TryRequiredInt(caratula, "CantidadCFE", out var cfeCount)
            || responseId is < 0 or > 9_999_999_999L
            || senderEnvelopeId is < 0 or > 9_999_999_999L
            || receiverId is < 0 or > 9_999_999_999L
            || cfeCount is < 1 or > 250)
        {
            return Invalid("fiscal.envelope.ack.caratula_numeric_invalid");
        }

        var stateText = RequiredText(detalle, "Estado");
        var state = stateText switch
        {
            "AS" => FiscalCfeEnvelopeAckState.Received,
            "BS" => FiscalCfeEnvelopeAckState.Rejected,
            _ => (FiscalCfeEnvelopeAckState?)null
        };
        if (state is null)
            return Invalid("fiscal.envelope.ack.state_invalid");

        string? consultationToken = null;
        string? consultationAvailableAt = null;
        var parameterNodes = DirectChildren(detalle, "ParamConsulta", DgiNamespace);
        if (parameterNodes.Count > 1)
            return Invalid("fiscal.envelope.ack.consultation_structure_invalid");
        if (parameterNodes.Count == 1)
        {
            consultationToken = RequiredText(parameterNodes[0], "Token");
            consultationAvailableAt = RequiredText(parameterNodes[0], "FechaHora");
            if (string.IsNullOrWhiteSpace(consultationToken) || string.IsNullOrWhiteSpace(consultationAvailableAt))
                return Invalid("fiscal.envelope.ack.consultation_invalid");
        }

        var reasonNodes = DirectChildren(detalle, "MotivosRechazo", DgiNamespace);
        if (reasonNodes.Count > 30)
            return Invalid("fiscal.envelope.ack.reasons_too_many");

        var reasons = new List<FiscalCfeEnvelopeAckRejectionReason>(reasonNodes.Count);
        foreach (var node in reasonNodes)
        {
            var code = RequiredText(node, "Motivo");
            var glosa = RequiredText(node, "Glosa");
            if (!TryOptionalText(node, "Detalle", out var rawDetail))
                return Invalid("fiscal.envelope.ack.reason_structure_invalid");
            reasons.Add(new(
                code ?? string.Empty,
                glosa ?? string.Empty,
                string.IsNullOrWhiteSpace(rawDetail) ? null : rawDetail));
        }

        if (!FiscalCfeEnvelopeAckRejectionReasonEvidence.TryValidate(
                reasons,
                state == FiscalCfeEnvelopeAckState.Rejected,
                out _))
        {
            return Invalid("fiscal.envelope.ack.reasons_invalid");
        }

        return new FiscalCfeEnvelopeAckParseResult(
            true,
            state,
            receiverRut!,
            issuerRuc!,
            responseId,
            senderEnvelopeId,
            receiverId,
            cfeCount,
            receptionTimestamp!,
            signingTimestamp!,
            consultationToken,
            consultationAvailableAt,
            reasons,
            null);
    }

    private static List<XmlElement> DirectChildren(XmlElement parent, string localName, string namespaceUri) =>
        parent.ChildNodes
            .OfType<XmlElement>()
            .Where(x => string.Equals(x.LocalName, localName, StringComparison.Ordinal)
                && string.Equals(x.NamespaceURI, namespaceUri, StringComparison.Ordinal))
            .ToList();

    private static string? RequiredText(XmlElement parent, string localName)
    {
        var nodes = DirectChildren(parent, localName, DgiNamespace);
        return nodes.Count == 1 ? nodes[0].InnerText.Trim() : null;
    }

    private static bool TryOptionalText(XmlElement parent, string localName, out string? value)
    {
        var nodes = DirectChildren(parent, localName, DgiNamespace);
        if (nodes.Count > 1)
        {
            value = null;
            return false;
        }
        value = nodes.Count == 0 ? null : nodes[0].InnerText.Trim();
        return true;
    }

    private static bool TryRequiredLong(XmlElement parent, string localName, out long value)
    {
        var text = RequiredText(parent, localName);
        return long.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool TryRequiredInt(XmlElement parent, string localName, out int value)
    {
        var text = RequiredText(parent, localName);
        return int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool TwelveDigits(string? value) =>
        value is not null && value.Length == 12 && value.All(char.IsDigit);

    private static FiscalCfeEnvelopeAckParseResult Invalid(string code) =>
        new(
            false,
            null,
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            null,
            string.Empty,
            string.Empty,
            null,
            null,
            Array.Empty<FiscalCfeEnvelopeAckRejectionReason>(),
            code);
}
