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
/// Infrastructure-only adapter for the DGI ws_efactura / EFACCONSULTARESTADOENVIO operation.
/// The published contract accepts ConsultaCFE(IdReceptor + Token) directly inside Datain/xmlData
/// CDATA and returns ACKCFE inside Dataout/xmlData. Endpoint and SOAPAction remain external
/// configuration. The adapter parses only bounded correlation/detail evidence and does not mutate
/// local fiscal state or infer completion from a single ACKCFE.
/// </summary>
public sealed class DgiWsSecurityFiscalCfeEnvelopeDocumentResponseConsultationGateway :
    IFiscalCfeEnvelopeDocumentResponseConsultationGateway,
    IDisposable
{
    public const string ConfigurationSection = "FiscalTransport:CfeDocumentResponseConsultation";

    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string DgiServiceNamespace = "http://dgi.gub.uy";
    private const string CfeNamespace = "http://cfe.dgi.gub.uy";
    private const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string X509V3ValueType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3";
    private const string Base64BinaryEncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

    private readonly Uri _endpoint;
    private readonly string _soapAction;
    private readonly IFiscalDailyReportTransportCertificateSource _certificates;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public DgiWsSecurityFiscalCfeEnvelopeDocumentResponseConsultationGateway(
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
                "fiscal.envelope.document_response.endpoint_invalid",
                "FiscalTransport:CfeDocumentResponseConsultation:Endpoint must be an absolute HTTPS URI.");
        }

        var soapAction = section["SoapAction"]?.Trim();
        if (string.IsNullOrWhiteSpace(soapAction))
        {
            throw ConfigurationError(
                "fiscal.envelope.document_response.soap_action_required",
                "FiscalTransport:CfeDocumentResponseConsultation:SoapAction is required.");
        }

        var timeoutSeconds = 60;
        var timeoutText = section["TimeoutSeconds"]?.Trim();
        if (!string.IsNullOrEmpty(timeoutText)
            && (!int.TryParse(timeoutText, out timeoutSeconds) || timeoutSeconds is < 5 or > 180))
        {
            throw ConfigurationError(
                "fiscal.envelope.document_response.timeout_invalid",
                "FiscalTransport:CfeDocumentResponseConsultation:TimeoutSeconds must be between 5 and 180 seconds.");
        }

        _endpoint = endpoint;
        _soapAction = soapAction;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public async Task<FiscalCfeEnvelopeDocumentResponseConsultationResponse> QueryAsync(
        FiscalCfeEnvelopeDocumentResponseConsultationRequest request,
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
        catch (FiscalCfeEnvelopeDocumentResponseConsultationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FiscalCfeEnvelopeDocumentResponseConsultationException(
                "fiscal.envelope.document_response.request_build_failed",
                "DGI document-response consultation request could not be built.",
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
            throw new FiscalCfeEnvelopeDocumentResponseConsultationException(
                "fiscal.envelope.document_response.transport_failed",
                "DGI document-response consultation failed before a trustworthy response was obtained.",
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
                throw new FiscalCfeEnvelopeDocumentResponseConsultationException(
                    "fiscal.envelope.document_response.response_read_failed",
                    "DGI document-response consultation response could not be read completely.",
                    ex);
            }

            if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
            {
                throw new FiscalCfeEnvelopeDocumentResponseConsultationException(
                    "fiscal.envelope.document_response.http_failure",
                    $"DGI document-response consultation returned HTTP {(int)response.StatusCode}.");
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
        FiscalCfeEnvelopeDocumentResponseConsultationRequest request,
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
        var binaryToken = document.CreateElement("wsse", "BinarySecurityToken", WsseNamespace);
        binaryToken.SetAttribute("Id", WsuNamespace, tokenId);
        binaryToken.SetAttribute("ValueType", X509V3ValueType);
        binaryToken.SetAttribute("EncodingType", Base64BinaryEncodingType);
        binaryToken.InnerText = Convert.ToBase64String(certificate.RawData);
        security.AppendChild(binaryToken);

        var bodyId = "Body-" + Guid.NewGuid().ToString("N");
        var body = document.CreateElement("soapenv", "Body", SoapNamespace);
        body.SetAttribute("Id", WsuNamespace, bodyId);
        envelope.AppendChild(body);

        var operation = document.CreateElement("dgi", "WS_eFactura.EFACCONSULTARESTADOENVIO", DgiServiceNamespace);
        body.AppendChild(operation);
        var dataIn = document.CreateElement("dgi", "Datain", DgiServiceNamespace);
        operation.AppendChild(dataIn);
        var xmlData = document.CreateElement("dgi", "xmlData", DgiServiceNamespace);
        xmlData.AppendChild(document.CreateCDataSection(BuildConsultaCfe(request)));
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

    internal static FiscalCfeEnvelopeDocumentResponseConsultationResponse ParseResponse(string soapResponseXml)
    {
        if (string.IsNullOrWhiteSpace(soapResponseXml))
            throw InvalidResponse("DGI returned an empty document-response consultation SOAP response.");

        var soap = LoadXml(soapResponseXml, "DGI document-response consultation SOAP response");
        var operationResponse = soap.SelectSingleNode(
            "//*[local-name()='WS_eFactura.EFACCONSULTARESTADOENVIOResponse']") as XmlElement
            ?? throw InvalidResponse("DGI SOAP response does not contain EFACCONSULTARESTADOENVIO response payload.");
        var xmlData = operationResponse.SelectSingleNode(
            ".//*[local-name()='DataOut' or local-name()='Dataout']/*[local-name()='xmlData']") as XmlElement
            ?? operationResponse.SelectSingleNode(".//*[local-name()='xmlData']") as XmlElement
            ?? throw InvalidResponse("DGI SOAP response does not contain DataOut/xmlData.");

        var embeddedElement = xmlData.ChildNodes.OfType<XmlElement>().FirstOrDefault();
        var ackXml = embeddedElement?.OuterXml ?? xmlData.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(ackXml))
            throw InvalidResponse("DGI DataOut/xmlData is empty.");

        var ackDocument = LoadXml(ackXml, "ACKCFE");
        var ack = ackDocument.DocumentElement;
        if (ack is null
            || !string.Equals(ack.LocalName, "ACKCFE", StringComparison.Ordinal)
            || !string.Equals(ack.NamespaceURI, CfeNamespace, StringComparison.Ordinal))
        {
            throw InvalidResponse("DGI consultation payload is not an ACKCFE in the governed CFE namespace.");
        }

        var caratula = ack.SelectSingleNode("./*[local-name()='Caratula']") as XmlElement
            ?? throw InvalidResponse("ACKCFE does not contain Caratula.");
        var receiverRut = RequiredText(caratula, "RUCReceptor");
        var issuerRuc = RequiredText(caratula, "RUCEmisor");
        var responseId = RequiredLong(caratula, "IDRespuesta");
        var senderEnvelopeId = RequiredLong(caratula, "IDEmisor");
        var dgiReceiverId = RequiredLong(caratula, "IDReceptor");
        var envelopeCount = RequiredInt(caratula, "CantenSobre");
        var respondedCount = RequiredInt(caratula, "CantResponden");
        var acceptedCount = RequiredInt(caratula, "CantCFEAceptados");
        var rejectedCount = RequiredInt(caratula, "CantCFERechazados");
        var observedCount = RequiredInt(caratula, "CantCFEObservados");
        var otherRejectedCount = RequiredInt(caratula, "CantOtrosRechazados");

        var details = new List<FiscalCfeEnvelopeDocumentResponseDetail>();
        foreach (var detail in ack.SelectNodes("./*[local-name()='ACKCFE_det']")!.OfType<XmlElement>())
        {
            if (!int.TryParse(detail.GetAttribute("ordinal"), NumberStyles.None, CultureInfo.InvariantCulture, out var ordinal))
                throw InvalidResponse("ACKCFE detail ordinal is missing or invalid.");
            details.Add(new FiscalCfeEnvelopeDocumentResponseDetail(
                ordinal,
                RequiredInt(detail, "TipoCFE"),
                RequiredText(detail, "Serie"),
                RequiredLong(detail, "NroCFE"),
                RequiredText(detail, "Estado")));
        }

        return new FiscalCfeEnvelopeDocumentResponseConsultationResponse(
            issuerRuc,
            receiverRut,
            responseId,
            senderEnvelopeId,
            dgiReceiverId,
            envelopeCount,
            respondedCount,
            acceptedCount,
            rejectedCount,
            observedCount,
            otherRejectedCount,
            details,
            ackXml);
    }

    private static string BuildConsultaCfe(FiscalCfeEnvelopeDocumentResponseConsultationRequest request)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        var root = document.CreateElement(string.Empty, "ConsultaCFE", DgiServiceNamespace);
        document.AppendChild(root);
        var receiverId = document.CreateElement(string.Empty, "IdReceptor", DgiServiceNamespace);
        receiverId.InnerText = request.DgiReceiverId.ToString(CultureInfo.InvariantCulture);
        root.AppendChild(receiverId);
        var token = document.CreateElement(string.Empty, "Token", DgiServiceNamespace);
        token.InnerText = request.ConsultationToken.Trim();
        root.AppendChild(token);
        return document.OuterXml;
    }

    private static string RequiredText(XmlElement parent, string localName)
    {
        var value = parent.SelectSingleNode($"./*[local-name()='{localName}']")?.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw InvalidResponse($"ACKCFE does not contain {localName}.");
        return value;
    }

    private static long RequiredLong(XmlElement parent, string localName)
    {
        var text = RequiredText(parent, localName);
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            throw InvalidResponse($"ACKCFE {localName} is not a valid integer.");
        return value;
    }

    private static int RequiredInt(XmlElement parent, string localName)
    {
        var text = RequiredText(parent, localName);
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            throw InvalidResponse($"ACKCFE {localName} is not a valid integer.");
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

    private static void ValidateRequest(FiscalCfeEnvelopeDocumentResponseConsultationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationId)
            || request.OrganizationId.Trim().Length > 200
            || request.DgiReceiverId is < 0 or > 9_999_999_999L
            || string.IsNullOrWhiteSpace(request.ConsultationToken)
            || request.ConsultationToken.Length > 8192)
        {
            throw new FiscalCfeEnvelopeDocumentResponseConsultationException(
                "fiscal.envelope.document_response.request_invalid",
                "Document-response consultation requires bounded OrganizationId, IdReceptor and Token evidence.");
        }
    }

    private static FiscalCfeEnvelopeDocumentResponseConsultationException InvalidResponse(
        string message,
        Exception? inner = null) =>
        new("fiscal.envelope.document_response.response_invalid", message, inner);

    private static FiscalCfeEnvelopeDocumentResponseConsultationException ConfigurationError(
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

public sealed class FiscalCfeEnvelopeDocumentResponseConsultationException : Exception
{
    public FiscalCfeEnvelopeDocumentResponseConsultationException(
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException) => Code = code;

    public string Code { get; }
}
