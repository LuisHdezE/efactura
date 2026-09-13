using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using EFactura.Application.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class DgiFiscalCfeEnvelopeWsSecurityTests
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    [Fact]
    public void SOAP_envelope_carries_exact_EnvioCFE_as_CDATA_and_signs_body()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Sobre Transport Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        const string envio = "<EnvioCFE xmlns=\"http://cfe.dgi.gub.uy\" version=\"1.0\"><Caratula version=\"1.0\"/></EnvioCFE>";

        var method = typeof(DgiWsSecurityFiscalCfeEnvelopeTransportGateway).GetMethod(
            "BuildSoapEnvelope",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSoapEnvelope was not found.");
        var document = (XmlDocument)(method.Invoke(null, new object[] { envio, certificate })
            ?? throw new InvalidOperationException("SOAP envelope was not returned."));

        var namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("soap", SoapNamespace);
        namespaces.AddNamespace("wsse", WsseNamespace);
        namespaces.AddNamespace("wsu", WsuNamespace);
        namespaces.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

        var body = Assert.IsType<XmlElement>(document.SelectSingleNode("/soap:Envelope/soap:Body", namespaces));
        var bodyId = body.GetAttribute("Id", WsuNamespace);
        Assert.False(string.IsNullOrWhiteSpace(bodyId));
        Assert.Contains("WS_eFactura.EFACRECEPCIONSOBRE", body.InnerXml, StringComparison.Ordinal);

        var xmlData = Assert.IsType<XmlElement>(document.SelectSingleNode("//*[local-name()='Datain']/*[local-name()='xmlData']"));
        var cdata = Assert.Single(xmlData.ChildNodes.OfType<XmlCDataSection>());
        Assert.Equal(envio, cdata.Value);

        var token = Assert.IsType<XmlElement>(document.SelectSingleNode(
            "/soap:Envelope/soap:Header/wsse:Security/wsse:BinarySecurityToken",
            namespaces));
        Assert.Equal(Convert.ToBase64String(certificate.RawData), token.InnerText);

        var signature = Assert.IsType<XmlElement>(document.SelectSingleNode(
            "/soap:Envelope/soap:Header/wsse:Security/ds:Signature",
            namespaces));
        var signatureMethod = Assert.IsType<XmlElement>(signature.SelectSingleNode("ds:SignedInfo/ds:SignatureMethod", namespaces));
        var digestMethod = Assert.IsType<XmlElement>(signature.SelectSingleNode("ds:SignedInfo/ds:Reference/ds:DigestMethod", namespaces));
        var reference = Assert.IsType<XmlElement>(signature.SelectSingleNode("ds:SignedInfo/ds:Reference", namespaces));
        Assert.Equal(SignedXml.XmlDsigRSASHA1Url, signatureMethod.GetAttribute("Algorithm"));
        Assert.Equal(SignedXml.XmlDsigSHA1Url, digestMethod.GetAttribute("Algorithm"));
        Assert.Equal("#" + bodyId, reference.GetAttribute("URI"));

        var verifier = new WsuSignedXml(document);
        verifier.LoadXml(signature);
        Assert.True(verifier.CheckSignature(certificate, verifySignatureOnly: true));
    }

    [Fact]
    public void Dataout_payload_is_returned_opaque_without_ACKSobre_state_interpretation()
    {
        const string responsePayload = "<ACKSobre xmlns=\"http://cfe.dgi.gub.uy\"><Caratula/><Detalle><Estado>ANY</Estado><Motivos><Motivo>S08</Motivo></Motivos></Detalle></ACKSobre>";
        var soap = $"<soapenv:Envelope xmlns:soapenv=\"{SoapNamespace}\" xmlns:dgi=\"http://dgi.gub.uy\"><soapenv:Body><dgi:WS_eFactura.EFACRECEPCIONSOBREResponse><dgi:Dataout><dgi:xmlData><![CDATA[{responsePayload}]]></dgi:xmlData></dgi:Dataout></dgi:WS_eFactura.EFACRECEPCIONSOBREResponse></soapenv:Body></soapenv:Envelope>";

        var method = typeof(DgiWsSecurityFiscalCfeEnvelopeTransportGateway).GetMethod(
            "ParseResponse",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseResponse was not found.");
        var response = (FiscalCfeEnvelopeTransportResponse)(method.Invoke(null, new object[] { soap })
            ?? throw new InvalidOperationException("Transport response was not returned."));

        Assert.Equal(responsePayload, response.ResponseXml);
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