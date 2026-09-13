using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Fiscal;

/// <summary>
/// Infrastructure-only adapter for DGI ws_consultas / EFACCONSULTARESTADOCFE.
/// The authoritative contract accepts TipoCFE + Serie + Nro and returns EstadoCFE plus the
/// consultation parameters of the first Sobre received by DGI for that CFE. Endpoint and
/// SOAPAction remain external configuration and are never inferred from examples.
/// </summary>
public sealed class DgiWsSecurityFiscalCfeStateConsultationGateway :
    IFiscalCfeStateConsultationGateway,
    IDisposable
{
    public const string ConfigurationSection = "FiscalTransport:CfeStateConsultation";

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

    public DgiWsSecurityFiscalCfeStateConsultationGateway(
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
                "fiscal.cfe_state_consultation.endpoint_invalid",
                "FiscalTransport:CfeStateConsultation:Endpoint must be an absolute HTTPS URI.");
        }

        var soapAction = section["SoapAction"]?.Trim();
        if (string.IsNullOrWhiteSpace(soapAction))
        {
            throw ConfigurationError(
                "fiscal.cfe_state_consultation.soap_action_required",
                "FiscalTransport:CfeStateConsultation:SoapAction is required.");
        }

        var timeoutSeconds = 60;
        var timeoutText = section["TimeoutSeconds"]?.Trim();
        if (!string.IsNullOrEmpty(timeoutText)
            && (!int.TryParse(timeoutText, out timeoutSeconds) || timeoutSeconds is < 5 or > 180))
        {
            throw ConfigurationError(
                "fiscal.cfe_state_consultation.timeout_invalid",
                "FiscalTransport:CfeStateConsultation:TimeoutSeconds must be between 5 and 180 seconds.");
        }

        _endpoint = endpoint;
        _soapAction = soapAction;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public async Task<FiscalCfeStateConsultationResponse> QueryAsync(
        FiscalCfeStateConsultationRequest request,
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
            envelope = BuildSoapEnvelope(request, certificate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FiscalCfeStateConsultationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FiscalCfeStateConsultationException(
                "fiscal.cfe_state_consultation.request_build_failed",
                "DGI CFE state consultation request could not be built.",
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
            throw new FiscalCfeStateConsultationException(
                "fiscal.cfe_state_consultation.transport_failed",
                "DGI CFE state consultation failed before a trustworthy response was obtained.",
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
                throw new FiscalCfeStateConsultationException(
                    "fiscal.cfe_state_consultation.response_read_failed",
                    "DGI CFE state consultation response could not be read completely.",
                    ex);
            }

            if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
            {
                throw new FiscalCfeStateConsultationException(
                    "fiscal.cfe_state_consultation.http_failure",
                    $"DGI CFE state consultation returned HTTP {(int)response.StatusCode}.");
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
        FiscalCfeStateConsultationRequest request,
        X509Certificate2 certificate)
    {
        ValidateRequest(request);
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
            "WS_eFactura_Consultas.EFACCONSULTARESTADOCFE",
            DgiServiceNamespace);
        body.AppendChild(operation);
        var cfeId = document.CreateElement("dgi", "Cfeid", DgiServiceNamespace);
        operation.AppendChild(cfeId);
        AppendText(document, cfeId, "TipoCFE", ((int)request.CfeType).ToString());
        AppendText(document, cfeId, "Serie", request.Series.Trim().ToUpperInvariant());
        AppendText(document, cfeId, "Nro", request.Number.ToString());

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

    internal static FiscalCfeStateConsultationResponse ParseResponse(string soapResponseXml)
    {
        if (string.IsNullOrWhiteSpace(soapResponseXml))
            throw InvalidResponse("DGI returned an empty CFE state consultation SOAP response.");

        var soap = LoadXml(soapResponseXml, "DGI CFE state consultation SOAP response");
        var response = soap.SelectSingleNode(
            "//*[local-name()='WS_eFactura_Consultas.EFACCONSULTARESTADOCFEResponse']") as XmlElement
            ?? throw InvalidResponse("DGI SOAP response does not contain EFACCONSULTARESTADOCFE response payload.");

        XmlElement? ack = response.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(x => string.Equals(x.LocalName, "Ackconsultaestadocfe", StringComparison.Ordinal));

        if (ack is null)
        {
            foreach (var child in response.ChildNodes.OfType<XmlElement>())
            {
                var text = child.InnerText.Trim();
                if (!text.Contains("<Ackconsultaestadocfe", StringComparison.Ordinal))
                    continue;
                var embedded = LoadXml(text, "Ackconsultaestadocfe").DocumentElement;
                if (embedded is not null)
                {
                    ack = embedded;
                    break;
                }
            }
        }

        if (ack is null
            || !string.Equals(ack.LocalName, "Ackconsultaestadocfe", StringComparison.Ordinal))
        {
            throw InvalidResponse("DGI consultation response does not contain Ackconsultaestadocfe evidence.");
        }

        var state = RequiredText(ack, "EstadoCFE");
        var senderId = RequiredText(ack, "IdEmisor");
        var receiverId = RequiredText(ack, "IdReceptor");

        string? token = null;
        string? availableAt = null;
        var parameter = ack.SelectSingleNode("./*[local-name()='ParamConsulta']") as XmlElement;
        if (parameter is not null)
        {
            token = RequiredText(parameter, "Token");
            availableAt = RequiredText(parameter, "Fechahora");
        }

        return new FiscalCfeStateConsultationResponse(
            state,
            senderId,
            receiverId,
            token,
            availableAt,
            soapResponseXml);
    }

    private static void AppendText(XmlDocument document, XmlElement parent, string localName, string value)
    {
        var element = document.CreateElement("dgi", localName, DgiServiceNamespace);
        element.InnerText = value;
        parent.AppendChild(element);
    }

    private static string RequiredText(XmlElement parent, string localName)
    {
        var value = parent.SelectSingleNode($"./*[local-name()='{localName}']")?.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw InvalidResponse($"Ackconsultaestadocfe does not contain {localName}.");
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

    private static void ValidateRequest(FiscalCfeStateConsultationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationId)
            || request.OrganizationId.Trim().Length > 200
            || !Enum.IsDefined(request.CfeType)
            || string.IsNullOrWhiteSpace(request.Series)
            || request.Series.Trim().Length > 20
            || request.Number <= 0)
        {
            throw new FiscalCfeStateConsultationException(
                "fiscal.cfe_state_consultation.request_invalid",
                "CFE state consultation request is invalid.");
        }
    }

    private static FiscalCfeStateConsultationException InvalidResponse(
        string message,
        Exception? inner = null) =>
        new("fiscal.cfe_state_consultation.response_invalid", message, inner);

    private static FiscalCfeStateConsultationException ConfigurationError(
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

public sealed class FiscalCfeStateConsultationException : Exception
{
    public FiscalCfeStateConsultationException(
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
