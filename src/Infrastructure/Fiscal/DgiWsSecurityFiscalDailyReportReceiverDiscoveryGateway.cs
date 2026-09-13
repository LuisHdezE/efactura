using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using EFactura.Application.Fiscal;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Fiscal;

/// <summary>
/// Infrastructure-only adapter for DGI ws_consultas / EFACCONSULTARENVIOSREPORTE.
/// The published contract requires FechaResumen and allows Secuencia and IdEmisor as optional filters.
/// This bounded adapter sends FechaResumen + Secuencia and deliberately omits IdEmisor because the
/// consumer does not yet hold authoritative durable DGI IdEmisor evidence. Endpoint and SOAPAction
/// remain external configuration and are never inferred from examples or WSDL locations.
/// </summary>
public sealed class DgiWsSecurityFiscalDailyReportReceiverDiscoveryGateway :
    IFiscalDailyReportReceiverDiscoveryGateway,
    IDisposable
{
    public const string ConfigurationSection = "FiscalTransport:DailyReportReceiverDiscovery";

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

    public DgiWsSecurityFiscalDailyReportReceiverDiscoveryGateway(
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
                "fiscal.daily_report.receiver_discovery.endpoint_invalid",
                "FiscalTransport:DailyReportReceiverDiscovery:Endpoint must be an absolute HTTPS URI.");
        }

        var soapAction = section["SoapAction"]?.Trim();
        if (string.IsNullOrWhiteSpace(soapAction))
        {
            throw ConfigurationError(
                "fiscal.daily_report.receiver_discovery.soap_action_required",
                "FiscalTransport:DailyReportReceiverDiscovery:SoapAction is required.");
        }

        var timeoutSeconds = 60;
        var timeoutText = section["TimeoutSeconds"]?.Trim();
        if (!string.IsNullOrEmpty(timeoutText)
            && (!int.TryParse(timeoutText, out timeoutSeconds) || timeoutSeconds is < 5 or > 180))
        {
            throw ConfigurationError(
                "fiscal.daily_report.receiver_discovery.timeout_invalid",
                "FiscalTransport:DailyReportReceiverDiscovery:TimeoutSeconds must be between 5 and 180 seconds.");
        }

        _endpoint = endpoint;
        _soapAction = soapAction;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public async Task<FiscalDailyReportReceiverDiscoveryResponse> QueryAsync(
        FiscalDailyReportReceiverDiscoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        XmlDocument envelope;
        try
        {
            var certificate = await _certificates.GetTransportCertificateAsync(
                request.OrganizationId,
                cancellationToken);
            envelope = BuildSoapEnvelope(request.SummaryDate, request.Sequence, certificate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FiscalDailyReportReceiverDiscoveryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FiscalDailyReportReceiverDiscoveryException(
                "fiscal.daily_report.receiver_discovery.request_build_failed",
                "DGI Reporte Diario receiver-discovery request could not be built.",
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
            throw new FiscalDailyReportReceiverDiscoveryException(
                "fiscal.daily_report.receiver_discovery.transport_failed",
                "DGI Reporte Diario receiver discovery failed before a trustworthy response was obtained.",
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
                throw new FiscalDailyReportReceiverDiscoveryException(
                    "fiscal.daily_report.receiver_discovery.response_read_failed",
                    "DGI receiver-discovery response could not be read completely.",
                    ex);
            }

            if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
            {
                throw new FiscalDailyReportReceiverDiscoveryException(
                    "fiscal.daily_report.receiver_discovery.http_failure",
                    $"DGI Reporte Diario receiver discovery returned HTTP {(int)response.StatusCode}.");
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

    internal static XmlDocument BuildSoapEnvelope(
        DateOnly summaryDate,
        int sequence,
        X509Certificate2 certificate)
    {
        if (summaryDate == default)
            throw new InvalidOperationException("FechaResumen is required for Reporte Diario receiver discovery.");
        if (sequence is < 1 or > 99)
            throw new InvalidOperationException("Secuencia must be between 1 and 99 for Reporte Diario receiver discovery.");
        ArgumentNullException.ThrowIfNull(certificate);
        using var privateKey = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("DGI receiver-discovery certificate requires an RSA private key.");

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

        var operation = document.CreateElement(
            "dgi",
            "WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTE",
            DgiServiceNamespace);
        body.AppendChild(operation);
        var query = document.CreateElement("dgi", "Consultaenviosreporte", DgiServiceNamespace);
        operation.AppendChild(query);

        var date = document.CreateElement("dgi", "FechaResumen", DgiServiceNamespace);
        date.InnerText = summaryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        query.AppendChild(date);

        var sequenceElement = document.CreateElement("dgi", "Secuencia", DgiServiceNamespace);
        sequenceElement.InnerText = sequence.ToString(CultureInfo.InvariantCulture);
        query.AppendChild(sequenceElement);

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

    internal static FiscalDailyReportReceiverDiscoveryResponse ParseResponse(string soapResponseXml)
    {
        if (string.IsNullOrWhiteSpace(soapResponseXml))
            throw InvalidResponse("DGI returned an empty receiver-discovery SOAP response.");

        var soap = LoadXml(soapResponseXml, "DGI receiver-discovery SOAP response");
        var response = soap.SelectSingleNode(
            "//*[local-name()='WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTEResponse']") as XmlElement
            ?? throw InvalidResponse("DGI SOAP response does not contain EFACCONSULTARENVIOSREPORTE response payload.");
        var ack = response.SelectSingleNode(".//*[local-name()='Ackconsultaenviosreporte']") as XmlElement
            ?? throw InvalidResponse("DGI receiver-discovery response does not contain Ackconsultaenviosreporte evidence.");

        var items = new List<FiscalDailyReportReceiverDiscoveryItem>();
        foreach (var report in ack.SelectNodes(".//*[local-name()='DatosReporte']")?.OfType<XmlElement>()
                 ?? Enumerable.Empty<XmlElement>())
        {
            var emitter = RequiredText(report, "IdEmisor", 120);
            var receiver = RequiredText(report, "IdReceptor", 120);
            var state = RequiredText(report, "Estado", 8);
            var receptionTimestamp = RequiredText(report, "FechaHoraRecepcion", 80);
            items.Add(new FiscalDailyReportReceiverDiscoveryItem(
                emitter,
                receiver,
                state,
                receptionTimestamp));
        }

        return new FiscalDailyReportReceiverDiscoveryResponse(ack.OuterXml, items);
    }

    private static string RequiredText(XmlElement parent, string localName, int maxLength)
    {
        var value = parent.SelectSingleNode($"./*[local-name()='{localName}']")?.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw InvalidResponse($"DGI receiver-discovery row contains missing or oversized {localName}.");
        return value;
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

    private static void ValidateRequest(FiscalDailyReportReceiverDiscoveryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationId)
            || request.OrganizationId.Trim().Length > 200
            || request.SummaryDate == default
            || request.Sequence is < 1 or > 99)
        {
            throw new FiscalDailyReportReceiverDiscoveryException(
                "fiscal.daily_report.receiver_discovery.request_invalid",
                "Reporte Diario receiver-discovery request is invalid.");
        }
    }

    private static FiscalDailyReportReceiverDiscoveryException InvalidResponse(
        string message,
        Exception? inner = null) =>
        new("fiscal.daily_report.receiver_discovery.response_invalid", message, inner);

    private static FiscalDailyReportReceiverDiscoveryException ConfigurationError(
        string code,
        string message) => new(code, message);

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

public sealed class FiscalDailyReportReceiverDiscoveryException : Exception
{
    public FiscalDailyReportReceiverDiscoveryException(
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
