using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using EFactura.Application.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class DgiFiscalDailyReportWsSecurityTests
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    [Fact]
    public void SOAP_envelope_contains_X509_token_and_valid_legacy_transport_signature_over_body()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Reporte Transport Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        const string report = "<Reporte xmlns=\"http://cfe.dgi.gub.uy\"><Caratula/></Reporte>";

        var method = typeof(DgiWsSecurityFiscalDailyReportTransportGateway).GetMethod(
            "BuildSoapEnvelope",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSoapEnvelope was not found.");
        var document = (XmlDocument)(method.Invoke(null, new object[] { report, certificate })
            ?? throw new InvalidOperationException("SOAP envelope was not returned."));

        var namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("soap", SoapNamespace);
        namespaces.AddNamespace("wsse", WsseNamespace);
        namespaces.AddNamespace("wsu", WsuNamespace);
        namespaces.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

        var body = (XmlElement?)document.SelectSingleNode("/soap:Envelope/soap:Body", namespaces);
        Assert.NotNull(body);
        var bodyId = body!.GetAttribute("Id", WsuNamespace);
        Assert.False(string.IsNullOrWhiteSpace(bodyId));
        Assert.Contains("WS_eFactura.EFACRECEPCIONREPORTE", body.InnerXml, StringComparison.Ordinal);
        Assert.Contains("Datain", body.InnerXml, StringComparison.Ordinal);
        Assert.Contains(report, body.InnerText, StringComparison.Ordinal);

        var token = (XmlElement?)document.SelectSingleNode("/soap:Envelope/soap:Header/wsse:Security/wsse:BinarySecurityToken", namespaces);
        Assert.NotNull(token);
        Assert.Equal(Convert.ToBase64String(certificate.RawData), token!.InnerText);

        var signature = (XmlElement?)document.SelectSingleNode("/soap:Envelope/soap:Header/wsse:Security/ds:Signature", namespaces);
        Assert.NotNull(signature);
        var signatureMethod = (XmlElement?)signature!.SelectSingleNode("ds:SignedInfo/ds:SignatureMethod", namespaces);
        var digestMethod = (XmlElement?)signature.SelectSingleNode("ds:SignedInfo/ds:Reference/ds:DigestMethod", namespaces);
        var reference = (XmlElement?)signature.SelectSingleNode("ds:SignedInfo/ds:Reference", namespaces);
        Assert.Equal(SignedXml.XmlDsigRSASHA1Url, signatureMethod!.GetAttribute("Algorithm"));
        Assert.Equal(SignedXml.XmlDsigSHA1Url, digestMethod!.GetAttribute("Algorithm"));
        Assert.Equal("#" + bodyId, reference!.GetAttribute("URI"));

        var verifier = new WsuSignedXml(document);
        verifier.LoadXml(signature);
        Assert.True(verifier.CheckSignature(certificate, verifySignatureOnly: true));
    }

    [Theory]
    [InlineData("AR")]
    [InlineData("BR")]
    public void Immediate_ACKRepDiario_AR_or_BR_is_extracted_from_Dataout(string state)
    {
        var ack = $"<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>receiver-9</IDReceptor></Caratula><Detalle><Estado>{state}</Estado></Detalle></ACKRepDiario>";
        var soap = $"<soapenv:Envelope xmlns:soapenv=\"{SoapNamespace}\" xmlns:dgi=\"http://dgi.gub.uy\"><soapenv:Body><dgi:WS_eFactura.EFACRECEPCIONREPORTEResponse><dgi:Dataout><dgi:xmlData><![CDATA[{ack}]]></dgi:xmlData></dgi:Dataout></dgi:WS_eFactura.EFACRECEPCIONREPORTEResponse></soapenv:Body></soapenv:Envelope>";

        var method = typeof(DgiWsSecurityFiscalDailyReportTransportGateway).GetMethod(
            "ParseResponse",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseResponse was not found.");
        var response = (FiscalDailyReportTransportResponse)(method.Invoke(null, new object[] { soap })
            ?? throw new InvalidOperationException("Transport response was not returned."));

        Assert.Equal(state, response.AckStateCode);
        Assert.Equal("receiver-9", response.DgiReceiverId);
        Assert.Contains("ACKRepDiario", response.AckXml, StringComparison.Ordinal);
    }

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
