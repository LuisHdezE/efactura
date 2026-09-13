using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using EFactura.Application.Fiscal;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Fiscal;

public sealed class SystemFiscalCfeEnvelopeTransportClock : IFiscalCfeEnvelopeTransportClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Infrastructure-only DGI Sobre transport for EFACRECEPCIONSOBRE. The published DGI reception
/// document frames EnvioCFE directly inside Datain/xmlData CDATA. This adapter therefore does not
/// gzip or Base64-wrap the fiscal envelope. WS-Security SHA1 is isolated to the SOAP transport
/// signature and must never be reused as fiscal-document signature policy.
///
/// The returned Dataout/xmlData payload is intentionally opaque at this boundary. ACKSobre business
/// semantics and state interpretation are a separately governed capability.
/// </summary>
public sealed class DgiWsSecurityFiscalCfeEnvelopeTransportGateway : IFiscalCfeEnvelopeTransportGateway, IDisposable
{
    public const string ConfigurationSection = "FiscalTransport:CfeEnvelope";

    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string DgiServiceNamespace = "http://dgi.gub.uy";
    private const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string X509V3ValueType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3";
    private const string Base64BinaryEncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

    private readonly Uri _endpoint;
    private readonly string _soapAction;
    private readonly IFiscalDailyReportTransportCertificateSource _certificates;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public DgiWsSecurityFiscalCfeEnvelopeTransportGateway(
        IConfiguration configuration,
        IFiscalDailyReportTransportCertificateSource certificates)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));

        var section = configuration.GetSection(ConfigurationSection);
        var endpointText = section["Endpoint"]?.Trim();
        if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint)
            || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw ConfigurationError("fiscal.envelope.transport.endpoint_invalid", "FiscalTransport:CfeEnvelope:Endpoint must be an absolute HTTPS URI.");

        var soapAction = section["SoapAction"]?.Trim();
        if (string.IsNullOrWhiteSpace(soapAction))
            throw ConfigurationError("fiscal.envelope.transport.soap_action_required", "FiscalTransport:CfeEnvelope:SoapAction is required.");

        var timeoutSeconds = 60;
        var timeoutText = section["TimeoutSeconds"]?.Trim();
        if (!string.IsNullOrEmpty(timeoutText)
            && (!int.TryParse(timeoutText, out timeoutSeconds) || timeoutSeconds is < 5 or > 180))
            throw ConfigurationError("fiscal.envelope.transport.timeout_invalid", "FiscalTransport:CfeEnvelope:TimeoutSeconds must be between 5 and 180 seconds.");

        _endpoint = endpoint;
        _soapAction = soapAction;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public async Task<FiscalCfeEnvelopeTransportResponse> SendAsync(
        FiscalCfeEnvelopeTransportRequest request,
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
            envelope = BuildSoapEnvelope(request.EnvelopeXml, certificate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FiscalCfeEnvelopeTransportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FiscalCfeEnvelopeTransportException(
                "fiscal.envelope.transport.request_build_failed",
                "DGI Sobre SOAP request could not be built before network dispatch.",
                deliveryAmbiguous: false,
                ex);
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(envelope.OuterXml, Encoding.UTF8, "text/xml")
        };
        message.Headers.TryAddWithoutValidation("SOAPAction", _soapAction.StartsWith('"') ? _soapAction : $"\"{_soapAction}\"");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new FiscalCfeEnvelopeTransportException(
                "fiscal.envelope.transport.delivery_unknown",
                "DGI Sobre transport ended without a trustworthy response; delivery must be reconciled before retry.",
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
                throw new FiscalCfeEnvelopeTransportException(
                    "fiscal.envelope.transport.response_read_failed",
                    "DGI responded but the Sobre response could not be read completely.",
                    deliveryAmbiguous: true,
                    ex);
            }

            if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
                throw new FiscalCfeEnvelopeTransportException(
                    "fiscal.envelope.transport.http_failure",
                    $"DGI Sobre transport returned HTTP {(int)response.StatusCode}.",
                    deliveryAmbiguous: true);

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

    internal static XmlDocument BuildSoapEnvelope(string envelopeXml, X509Certificate2 certificate)
    {
        if (string.IsNullOrWhiteSpace(envelopeXml))
            throw new InvalidOperationException("Durable EnvioCFE XML is required for DGI transport.");
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

        var operation = document.CreateElement("dgi", "WS_eFactura.EFACRECEPCIONSOBRE", DgiServiceNamespace);
        body.AppendChild(operation);
        var dataIn = document.CreateElement("dgi", "Datain", DgiServiceNamespace);
        operation.AppendChild(dataIn);
        var xmlData = document.CreateElement("dgi", "xmlData", DgiServiceNamespace);
        xmlData.AppendChild(document.CreateCDataSection(envelopeXml));
        dataIn.AppendChild(xmlData);

        var signedXml = new WsuSignedXml(document) { SigningKey = privateKey };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        var reference = new Reference("#" + bodyId) { DigestMethod = SignedXml.XmlDsigSHA1Url };
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

    internal static FiscalCfeEnvelopeTransportResponse ParseResponse(string soapResponseXml)
    {
        if (string.IsNullOrWhiteSpace(soapResponseXml))
            throw InvalidResponse("DGI returned an empty SOAP response.");

        var soap = LoadXml(soapResponseXml);
        var xmlData = soap.SelectSingleNode("//*[local-name()='Dataout']/*[local-name()='xmlData']") as XmlElement
            ?? throw InvalidResponse("DGI SOAP response does not contain Dataout/xmlData.");

        var embeddedElement = xmlData.ChildNodes.OfType<XmlElement>().FirstOrDefault();
        var responseXml = embeddedElement?.OuterXml ?? xmlData.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(responseXml))
            throw InvalidResponse("DGI Dataout/xmlData is empty.");

        return new FiscalCfeEnvelopeTransportResponse(responseXml);
    }

    private static XmlDocument LoadXml(string xml)
    {
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            document.Load(reader);
            return document;
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            throw InvalidResponse("DGI SOAP response is not safe, well-formed XML.", ex);
        }
    }

    private static void ValidateRequest(FiscalCfeEnvelopeTransportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationId)
            || request.IssuerRuc.Length != 12 || request.IssuerRuc.Any(c => !char.IsDigit(c))
            || request.ReceiverRut.Length != 12 || request.ReceiverRut.Any(c => !char.IsDigit(c))
            || request.SenderEnvelopeId is < 0 or > 9_999_999_999L
            || !PrepareFiscalCfeEnvelopeSubmissionUseCase.Sha256Value(request.EnvelopeSha256)
            || string.IsNullOrWhiteSpace(request.EnvelopeXml)
            || !string.Equals(request.EnvelopeSha256, PrepareFiscalCfeEnvelopeSubmissionUseCase.Sha256(request.EnvelopeXml), StringComparison.Ordinal))
            throw new FiscalCfeEnvelopeTransportException(
                "fiscal.envelope.transport.request_invalid",
                "Sobre transport request is invalid or no longer matches its durable SHA-256 identity.",
                deliveryAmbiguous: false);
    }

    private static FiscalCfeEnvelopeTransportException InvalidResponse(string message, Exception? inner = null) =>
        new("fiscal.envelope.transport.response_invalid", message, deliveryAmbiguous: true, inner);

    private static FiscalCfeEnvelopeTransportException ConfigurationError(string code, string message) =>
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
