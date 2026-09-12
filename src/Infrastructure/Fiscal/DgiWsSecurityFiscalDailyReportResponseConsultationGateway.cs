using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using EFactura.Application.Fiscal;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Fiscal;

/// <summary>
/// Infrastructure-only adapter for DGI ws_consultas / EFACCONSULTARRESPUESTAREPORTE.
/// The published consultation contract accepts IdReceptor and returns the original ACKRepDiario.
/// Endpoint and SOAPAction remain external configuration and are never inferred from examples.
/// </summary>
public sealed class DgiWsSecurityFiscalDailyReportResponseConsultationGateway :
    IFiscalDailyReportResponseConsultationGateway,
    IDisposable
{
    public const string ConfigurationSection = "FiscalTransport:DailyReportConsultation";

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

    public DgiWsSecurityFiscalDailyReportResponseConsultationGateway(
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
                "fiscal.daily_report.consultation.endpoint_invalid",
                "FiscalTransport:DailyReportConsultation:Endpoint must be an absolute HTTPS URI.");
        }

        var soapAction = section["SoapAction"]?.Trim();
        if (string.IsNullOrWhiteSpace(soapAction))
        {
            throw ConfigurationError(
                "fiscal.daily_report.consultation.soap_action_required",
                "FiscalTransport:DailyReportConsultation:SoapAction is required.");
        }

        var timeoutSeconds = 60;
        var timeoutText = section["TimeoutSeconds"]?.Trim();
        if (!string.IsNullOrEmpty(timeoutText)
            && (!int.TryParse(timeoutText, out timeoutSeconds) || timeoutSeconds is < 5 or > 180))
        {
            throw ConfigurationError(
                "fiscal.daily_report.consultation.timeout_invalid",
                "FiscalTransport:DailyReportConsultation:TimeoutSeconds must be between 5 and 180 seconds.");
        }

        _endpoint = endpoint;
        _soapAction = soapAction;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public async Task<FiscalDailyReportResponseConsultationResponse> QueryAsync(
        FiscalDailyReportResponseConsultationRequest request,
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
            envelope = BuildSoapEnvelope(request.DgiReceiverId, certificate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FiscalDailyReportResponseConsultationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FiscalDailyReportResponseConsultationException(
                "fiscal.daily_report.consultation.request_build_failed",
                "DGI Reporte Diario consultation request could not be built.",
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
            throw new FiscalDailyReportResponseConsultationException(
                "fiscal.daily_report.consultation.transport_failed",
                "DGI Reporte Diario consultation failed before a trustworthy response was obtained.",
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
                throw new FiscalDailyReportResponseConsultationException(
                    "fiscal.daily_report.consultation.response_read_failed",
                    "DGI consultation response could not be read completely.",
                    ex);
            }

            if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
            {
                throw new FiscalDailyReportResponseConsultationException(
                    "fiscal.daily_report.consultation.http_failure",
                    $"DGI Reporte Diario consultation returned HTTP {(int)response.StatusCode}.");
            }

            return ParseResponse(responseXml, request.DgiReceiverId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _httpClient.Dispose();
        _disposed = true;
    }

    internal static XmlDocument BuildSoapEnvelope(string dgiReceiverId, X509Certificate2 certificate)
    {
        if (string.IsNullOrWhiteSpace(dgiReceiverId))
            throw new InvalidOperationException("DGI IdReceptor is required for Reporte Diario consultation.");
        ArgumentNullException.ThrowIfNull(certificate);
        using var privateKey = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("DGI consultation certificate requires an RSA private key.");

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
            "WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTE",
            DgiServiceNamespace);
        body.AppendChild(operation);
        var query = document.CreateElement("dgi", "Consultarrespuestareporte", DgiServiceNamespace);
        operation.AppendChild(query);
        var receiver = document.CreateElement("dgi", "IdReceptor", DgiServiceNamespace);
        receiver.InnerText = dgiReceiverId.Trim();
        query.AppendChild(receiver);

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

    internal static FiscalDailyReportResponseConsultationResponse ParseResponse(
        string soapResponseXml,
        string expectedReceiverId)
    {
        if (string.IsNullOrWhiteSpace(soapResponseXml))
            throw InvalidResponse("DGI returned an empty consultation SOAP response.");

        var soap = LoadXml(soapResponseXml, "DGI consultation SOAP response");
        var response = soap.SelectSingleNode(
            "//*[local-name()='WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTEResponse']") as XmlElement
            ?? throw InvalidResponse("DGI SOAP response does not contain EFACCONSULTARRESPUESTAREPORTE response payload.");

        string? ackXml = null;
        foreach (var child in response.ChildNodes.OfType<XmlElement>())
        {
            var text = child.InnerText.Trim();
            if (text.Contains("<ACKRepDiario", StringComparison.Ordinal))
            {
                ackXml = text;
                break;
            }

            var embedded = child.ChildNodes.OfType<XmlElement>()
                .FirstOrDefault(x => string.Equals(x.LocalName, "ACKRepDiario", StringComparison.Ordinal));
            if (embedded is not null)
            {
                ackXml = embedded.OuterXml;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(ackXml))
            throw InvalidResponse("DGI consultation response does not contain ACKRepDiario evidence.");

        var ack = LoadXml(ackXml, "ACKRepDiario");
        var root = ack.DocumentElement;
        if (root is null
            || !string.Equals(root.LocalName, "ACKRepDiario", StringComparison.Ordinal)
            || !string.Equals(root.NamespaceURI, DgiCfeNamespace, StringComparison.Ordinal))
        {
            throw InvalidResponse("DGI consultation payload is not ACKRepDiario in the expected namespace.");
        }

        var state = root.SelectSingleNode("./*[local-name()='Detalle']/*[local-name()='Estado']")?.InnerText.Trim();
        if (state is not ("AR" or "BR"))
            throw InvalidResponse("Original ACKRepDiario Estado must be AR or BR.");

        var receiverId = root.SelectSingleNode("./*[local-name()='Caratula']/*[local-name()='IDReceptor']")?.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(receiverId))
            throw InvalidResponse("ACKRepDiario does not contain IDReceptor.");
        if (!string.Equals(receiverId, expectedReceiverId.Trim(), StringComparison.Ordinal))
            throw InvalidResponse("ACKRepDiario IDReceptor does not match the requested report receiver id.");

        return new FiscalDailyReportResponseConsultationResponse(receiverId, state, ack.OuterXml);
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

    private static void ValidateRequest(FiscalDailyReportResponseConsultationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationId)
            || request.OrganizationId.Trim().Length > 200
            || string.IsNullOrWhiteSpace(request.DgiReceiverId)
            || request.DgiReceiverId.Trim().Length > 120)
        {
            throw new FiscalDailyReportResponseConsultationException(
                "fiscal.daily_report.consultation.request_invalid",
                "Reporte Diario consultation request is invalid.");
        }
    }

    private static FiscalDailyReportResponseConsultationException InvalidResponse(
        string message,
        Exception? inner = null) =>
        new("fiscal.daily_report.consultation.response_invalid", message, inner);

    private static FiscalDailyReportResponseConsultationException ConfigurationError(
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

public sealed class FiscalDailyReportResponseConsultationException : Exception
{
    public FiscalDailyReportResponseConsultationException(
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
