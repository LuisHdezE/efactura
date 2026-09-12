using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using EFactura.Application.Fiscal;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Fiscal;

/// <summary>
/// Infrastructure-only certificate boundary for DGI Reporte Diario transport. Transport credentials
/// never cross into Application contracts.
/// </summary>
public interface IFiscalDailyReportTransportCertificateSource
{
    ValueTask<X509Certificate2> GetTransportCertificateAsync(
        string organizationId,
        CancellationToken cancellationToken = default);
}

public sealed class SystemFiscalDailyReportTransportClock : IFiscalDailyReportTransportClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Direct DGI Reporte Diario transport for EFACRECEPCIONREPORTE.
///
/// DGI's published external-web-service specification documents legacy WS-Security with an X509v3
/// BinarySecurityToken, exclusive canonicalization, RSA-SHA1 and SHA1 over the SOAP Body reference.
/// Those SHA1 algorithms are intentionally isolated to this SOAP transport adapter. They MUST NOT be
/// reused for the Reporte document XMLDSig, which is governed independently by
/// XmlDsigFiscalDailyReportSignatureProvider and rejects SHA1.
///
/// Endpoint and SOAPAction are external configuration because this adapter does not infer environment
/// URLs from unofficial examples. The immediate response is parsed only as ACKRepDiario AR/BR evidence.
/// Later DR/ER/FR reconciliation remains a separate capability.
/// </summary>
public sealed class DgiWsSecurityFiscalDailyReportTransportGateway :
    IFiscalDailyReportTransportGateway,
    IDisposable
{
    public const string ConfigurationSection = "FiscalTransport:DailyReport";

    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string DgiServiceNamespace = "http://dgi.gub.uy";
    private const string DgiCfeNamespace = "http://cfe.dgi.gub.uy";
    private const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string X509V3ValueType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3";
    private const string Base64BinaryEncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

    private readonly Uri _endpoint;
    private readonly string _soapAction;
    private readonly IFiscalDailyReportTransportCertificateSource _certificates;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public DgiWsSecurityFiscalDailyReportTransportGateway(
        IConfiguration configuration,
        IFiscalDailyReportTransportCertificateSource certificates)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));

        var section = configuration.GetSection(ConfigurationSection);
        var endpointText = section["Endpoint"]?.Trim();
        if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint)
            || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw ConfigurationError(
                "fiscal.daily_report.transport.endpoint_invalid",
                "FiscalTransport:DailyReport:Endpoint must be an absolute HTTPS URI.");
        }

        var soapAction = section["SoapAction"]?.Trim();
        if (string.IsNullOrWhiteSpace(soapAction))
        {
            throw ConfigurationError(
                "fiscal.daily_report.transport.soap_action_required",
                "FiscalTransport:DailyReport:SoapAction is required.");
        }

        var timeoutSeconds = 60;
        var timeoutText = section["TimeoutSeconds"]?.Trim();
        if (!string.IsNullOrEmpty(timeoutText)
            && (!int.TryParse(timeoutText, out timeoutSeconds) || timeoutSeconds is < 5 or > 180))
        {
            throw ConfigurationError(
                "fiscal.daily_report.transport.timeout_invalid",
                "FiscalTransport:DailyReport:TimeoutSeconds must be between 5 and 180 seconds.");
        }

        _endpoint = endpoint;
        _soapAction = soapAction;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public async Task<FiscalDailyReportTransportResponse> SendAsync(
        FiscalDailyReportTransportRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        X509Certificate2 certificate;
        XmlDocument envelope;
        try
        {
            certificate = await _certificates.GetTransportCertificateAsync(request.OrganizationId, cancellationToken);
            envelope = BuildSoapEnvelope(request.SignedXml, certificate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FiscalDailyReportTransportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FiscalDailyReportTransportException(
                "fiscal.daily_report.transport.request_build_failed",
                "DGI Reporte Diario SOAP request could not be built before network dispatch.",
                deliveryAmbiguous: false,
                ex);
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(envelope.OuterXml, Encoding.UTF8, "text/xml")
        };
        message.Headers.TryAddWithoutValidation(
            "SOAPAction",
            _soapAction.StartsWith('"') ? _soapAction : $"\"{_soapAction}\"");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new FiscalDailyReportTransportException(
                "fiscal.daily_report.transport.delivery_unknown",
                "DGI Reporte Diario transport ended without a trustworthy response; delivery must be reconciled before retry.",
                deliveryAmbiguous: true,
                ex);
        }

        using (response)
        {
            string responseXml;
            try
            {
                responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
            {
                throw new FiscalDailyReportTransportException(
                    "fiscal.daily_report.transport.response_read_failed",
                    "DGI responded but the Reporte Diario response could not be read completely.",
                    deliveryAmbiguous: true,
                    ex);
            }

            if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
            {
                throw new FiscalDailyReportTransportException(
                    "fiscal.daily_report.transport.http_failure",
                    $"DGI Reporte Diario transport returned HTTP {(int)response.StatusCode}.",
                    deliveryAmbiguous: true);
            }

            return ParseResponse(responseXml);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _httpClient.Dispose();
        _disposed = true;
    }

    internal static XmlDocument BuildSoapEnvelope(string signedReportXml, X509Certificate2 certificate)
    {
        if (string.IsNullOrWhiteSpace(signedReportXml))
            throw new InvalidOperationException("Signed Reporte Diario XML is required for DGI transport.");
        ArgumentNullException.ThrowIfNull(certificate);
        using var privateKey = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("DGI transport certificate requires an RSA private key.");

        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        var envelope = document.CreateElement("soapenv", "Envelope", SoapNamespace);
        envelope.SetAttribute("xmlns:dgi", DgiServiceNamespace);
        envelope.SetAttribute("xmlns:wsse", WsseNamespace);
        envelope.SetAttribute("xmlns:wsu", WsuNamespace);
        document.AppendChild(envelope);

        var header = document.CreateElement("soapenv", "Header", SoapNamespace);
        envelope.AppendChild(header);
        var security = document.CreateElement("wsse", "Security", WsseNamespace);
        security.SetAttribute("mustUnderstand", SoapNamespace, "1");
        header.AppendChild(security);

        var tokenId = "X509-" + Guid.NewGuid().ToString("N");
        var token = document.CreateElement("wsse", "BinarySecurityToken", WsseNamespace);
        token.SetAttribute("Id", WsuNamespace, tokenId);
        token.SetAttribute("ValueType", X509V3ValueType);
        token.SetAttribute("EncodingType", Base64BinaryEncodingType);
        token.InnerText = Convert.ToBase64String(certificate.RawData);
        security.AppendChild(token);

        var bodyId = "Body-" + Guid.NewGuid().ToString("N");
        var body = document.CreateElement("soapenv", "Body", SoapNamespace);
        body.SetAttribute("Id", WsuNamespace, bodyId);
        envelope.AppendChild(body);

        var operation = document.CreateElement("dgi", "WS_eFactura.EFACRECEPCIONREPORTE", DgiServiceNamespace);
        body.AppendChild(operation);
        var dataIn = document.CreateElement("dgi", "Datain", DgiServiceNamespace);
        operation.AppendChild(dataIn);
        var xmlData = document.CreateElement("dgi", "xmlData", DgiServiceNamespace);
        xmlData.AppendChild(document.CreateCDataSection(signedReportXml));
        dataIn.AppendChild(xmlData);

        var signedXml = new WsuSignedXml(document)
        {
            SigningKey = privateKey
        };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

        var reference = new Reference("#" + bodyId)
        {
            DigestMethod = SignedXml.XmlDsigSHA1Url
        };
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        var securityTokenReference = document.CreateElement("wsse", "SecurityTokenReference", WsseNamespace);
        var certificateReference = document.CreateElement("wsse", "Reference", WsseNamespace);
        certificateReference.SetAttribute("URI", "#" + tokenId);
        certificateReference.SetAttribute("ValueType", X509V3ValueType);
        securityTokenReference.AppendChild(certificateReference);
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoNode(securityTokenReference));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();
        security.AppendChild(document.ImportNode(signedXml.GetXml(), deep: true));
        return document;
    }

    internal static FiscalDailyReportTransportResponse ParseResponse(string soapResponseXml)
    {
        if (string.IsNullOrWhiteSpace(soapResponseXml))
        {
            throw InvalidResponse("DGI returned an empty SOAP response.");
        }

        var soap = LoadXml(soapResponseXml, "DGI SOAP response");
        var xmlData = soap.SelectSingleNode("//*[local-name()='Dataout']/*[local-name()='xmlData']") as XmlElement
            ?? throw InvalidResponse("DGI SOAP response does not contain Dataout/xmlData.");

        var embeddedElement = xmlData.ChildNodes.OfType<XmlElement>().FirstOrDefault();
        var ackXml = embeddedElement?.OuterXml ?? xmlData.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(ackXml))
            throw InvalidResponse("DGI Dataout/xmlData does not contain ACKRepDiario.");

        var ack = LoadXml(ackXml, "ACKRepDiario");
        var root = ack.DocumentElement;
        if (root is null
            || !string.Equals(root.LocalName, "ACKRepDiario", StringComparison.Ordinal)
            || !string.Equals(root.NamespaceURI, DgiCfeNamespace, StringComparison.Ordinal))
        {
            throw InvalidResponse("DGI xmlData is not an ACKRepDiario in the expected namespace.");
        }

        var state = root.SelectSingleNode("./*[local-name()='Detalle']/*[local-name()='Estado']")?.InnerText.Trim();
        if (state is not ("AR" or "BR"))
            throw InvalidResponse("Immediate ACKRepDiario Estado must be AR or BR.");

        var receiverId = root.SelectSingleNode("./*[local-name()='Caratula']/*[local-name()='IDReceptor']")?.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(receiverId))
            throw InvalidResponse("ACKRepDiario does not contain IDReceptor.");

        return new FiscalDailyReportTransportResponse(state, receiverId, ack.OuterXml);
    }

    private static XmlDocument LoadXml(string xml, string label)
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            document.Load(reader);
            return document;
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            throw InvalidResponse($"{label} is not safe, well-formed XML.", ex);
        }
    }

    private static void ValidateRequest(FiscalDailyReportTransportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationId)
            || string.IsNullOrWhiteSpace(request.IssuerRuc)
            || request.IssuerRuc.Length != 12
            || request.IssuerRuc.Any(c => !char.IsDigit(c))
            || request.SummaryDate == default
            || request.Sequence is < 1 or > 99
            || request.SignedContentHash is null
            || request.SignedContentHash.Length != 64
            || request.SignedContentHash.Any(c => !Uri.IsHexDigit(c))
            || string.IsNullOrWhiteSpace(request.SignedXml))
        {
            throw new FiscalDailyReportTransportException(
                "fiscal.daily_report.transport.request_invalid",
                "Reporte Diario transport request is invalid.",
                deliveryAmbiguous: false);
        }
    }

    private static FiscalDailyReportTransportException InvalidResponse(string message, Exception? inner = null) =>
        new(
            "fiscal.daily_report.transport.response_invalid",
            message,
            deliveryAmbiguous: true,
            inner);

    private static FiscalDailyReportTransportException ConfigurationError(string code, string message) =>
        new(code, message, deliveryAmbiguous: false);

    private sealed class WsuSignedXml(XmlDocument document) : SignedXml(document)
    {
        public override XmlElement? GetIdElement(XmlDocument doc, string idValue)
        {
            var standard = base.GetIdElement(doc, idValue);
            if (standard is not null)
                return standard;

            var namespaces = new XmlNamespaceManager(doc.NameTable);
            namespaces.AddNamespace("wsu", WsuNamespace);
            return doc.SelectSingleNode($"//*[@wsu:Id='{idValue}']", namespaces) as XmlElement;
        }
    }
}
