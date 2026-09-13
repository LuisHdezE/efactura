using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using EFactura.Application.Fiscal;

namespace Infrastructure.Fiscal;

/// <summary>
/// Builds the DGI EnvioCFE/Sobre XML from already-signed CFE artifacts. It never accesses a private
/// key or network service. Signed CFE roots are embedded as their original XML fragment after removing
/// only the standalone XML declaration, so packaging does not reserialize the signed subtree.
/// </summary>
public sealed class DgiCfeEnvelopeBuilder : IFiscalCfeEnvelopeBuilder
{
    private const string CfeNamespace = "http://cfe.dgi.gub.uy";
    private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    public FiscalCfeEnvelopeBuildArtifact Build(FiscalCfeEnvelopeBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sources is null || request.Sources.Count is < 1 or > 250)
            throw new InvalidDataException("DGI Sobre requires between 1 and 250 signed CFE sources.");
        if (request.SenderEnvelopeId is < 0 or > 9_999_999_999L)
            throw new InvalidDataException("DGI Idemisor is outside the ten-digit XSD range.");
        if (string.IsNullOrWhiteSpace(request.ReceiverRut) || string.IsNullOrWhiteSpace(request.IssuerRuc))
            throw new InvalidDataException("DGI Sobre receiver and issuer RUT values are required.");

        var inspected = request.Sources.Select(source => Inspect(source, request.IssuerRuc.Trim())).ToArray();
        var first = inspected[0];
        foreach (var item in inspected.Skip(1))
        {
            if (!first.CertificateBytes.AsSpan().SequenceEqual(item.CertificateBytes)
                || !string.Equals(first.CertificateThumbprint, item.CertificateThumbprint, StringComparison.Ordinal)
                || !string.Equals(first.CertificateSerialNumber, item.CertificateSerialNumber, StringComparison.Ordinal))
            {
                throw new InvalidDataException("All CFEs in one DGI Sobre must contain the same X509 certificate.");
            }
        }

        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
            Indent = false,
            NewLineHandling = NewLineHandling.None
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("EnvioCFE", CfeNamespace);
            writer.WriteAttributeString("version", "1.0");

            writer.WriteStartElement("Caratula", CfeNamespace);
            writer.WriteAttributeString("version", "1.0");
            Element(writer, "RutReceptor", request.ReceiverRut.Trim());
            Element(writer, "RUCEmisor", request.IssuerRuc.Trim());
            Element(writer, "Idemisor", request.SenderEnvelopeId.ToString(CultureInfo.InvariantCulture));
            Element(writer, "CantCFE", inspected.Length.ToString(CultureInfo.InvariantCulture));
            Element(writer, "Fecha", request.CreatedAt.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture));
            Element(writer, "X509Certificate", Convert.ToBase64String(first.CertificateBytes));
            writer.WriteEndElement();

            foreach (var item in inspected)
                writer.WriteRaw(item.CfeRootFragment);

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return new FiscalCfeEnvelopeBuildArtifact(
            builder.ToString(),
            first.CertificateThumbprint,
            first.CertificateSerialNumber);
    }

    private static InspectedCfe Inspect(FiscalCfeEnvelopeSource source, string issuerRuc)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.FiscalDocumentId == Guid.Empty || string.IsNullOrWhiteSpace(source.SignedXml))
            throw new InvalidDataException("Signed CFE source is incomplete.");

        var document = new XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = false,
                IgnoreWhitespace = false
            };
            using var text = new StringReader(source.SignedXml);
            using var reader = XmlReader.Create(text, settings);
            document.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("Signed CFE source is not safe well-formed XML.", ex);
        }

        var root = document.DocumentElement
            ?? throw new InvalidDataException("Signed CFE source has no root element.");
        if (!string.Equals(root.LocalName, "CFE", StringComparison.Ordinal)
            || !string.Equals(root.NamespaceURI, CfeNamespace, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Signed CFE source must use the official DGI CFE root namespace.");
        }

        var issuerNodes = document.GetElementsByTagName("RUCEmisor", CfeNamespace);
        if (issuerNodes.Count != 1
            || !string.Equals(issuerNodes[0]?.InnerText?.Trim(), issuerRuc, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Signed CFE issuer RUC does not match the Sobre Caratula issuer.");
        }

        var certificateNodes = document.GetElementsByTagName("X509Certificate", XmlDsigNamespace);
        if (certificateNodes.Count != 1 || string.IsNullOrWhiteSpace(certificateNodes[0]?.InnerText))
            throw new InvalidDataException("Signed CFE must contain exactly one ds:X509Certificate for Sobre packaging.");

        byte[] certificateBytes;
        try
        {
            certificateBytes = Convert.FromBase64String(certificateNodes[0]!.InnerText.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Signed CFE contains an invalid base64 X509 certificate.", ex);
        }

        using var certificate = new X509Certificate2(certificateBytes);
        var thumbprint = NormalizeIdentifier(certificate.Thumbprint);
        var serial = NormalizeIdentifier(certificate.SerialNumber);
        if (!string.Equals(thumbprint, source.CertificateThumbprint, StringComparison.Ordinal)
            || !string.Equals(serial, source.CertificateSerialNumber, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Signed CFE embedded certificate does not match durable certificate evidence.");
        }

        return new InspectedCfe(
            certificateBytes,
            thumbprint,
            serial,
            RootFragment(source.SignedXml));
    }

    private static string RootFragment(string signedXml)
    {
        var start = 0;
        while (start < signedXml.Length && char.IsWhiteSpace(signedXml[start]))
            start++;

        if (signedXml.AsSpan(start).StartsWith("<?xml", StringComparison.Ordinal))
        {
            var declarationEnd = signedXml.IndexOf("?>", start, StringComparison.Ordinal);
            if (declarationEnd < 0)
                throw new InvalidDataException("Signed CFE XML declaration is malformed.");
            start = declarationEnd + 2;
            while (start < signedXml.Length && char.IsWhiteSpace(signedXml[start]))
                start++;
        }

        if (start >= signedXml.Length || signedXml[start] != '<')
            throw new InvalidDataException("Signed CFE root fragment is missing.");

        return signedXml[start..];
    }

    private static void Element(XmlWriter writer, string name, string value)
    {
        writer.WriteStartElement(name, CfeNamespace);
        writer.WriteString(value);
        writer.WriteEndElement();
    }

    private static string NormalizeIdentifier(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();

    private sealed record InspectedCfe(
        byte[] CertificateBytes,
        string CertificateThumbprint,
        string CertificateSerialNumber,
        string CfeRootFragment);

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
