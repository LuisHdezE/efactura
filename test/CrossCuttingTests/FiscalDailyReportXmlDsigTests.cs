using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EFactura.Application.Fiscal;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportXmlDsigTests
{
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 11, 21, 15, 30, TimeSpan.FromHours(-3));
    private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    [Fact]
    public async Task Signer_appends_final_verifiable_signature_and_complete_report_validates_against_untouched_XSD()
    {
        var artifact = new DeterministicUnsignedDailyReportXmlBuilder()
            .Build(Projection(CfeFamily.EFactura), SigningTimestamp);
        var unsignedValidation = new DgiFeV1_44_2UnsignedDailyReportSchemaValidator().Validate(artifact.Xml);
        Assert.True(unsignedValidation.IsValid, Errors(unsignedValidation.Errors));

        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var source = new StaticCertificateSource(certificate);
        var signer = new XmlDsigFiscalDailyReportSignatureProvider(source);

        var signed = await signer.SignAsync(Request(artifact));

        Assert.Equal(XmlDsigFiscalDailyReportSignatureProvider.DailyReportProfileId, signed.SignatureProfileId);
        Assert.Equal(Sha256(signed.SignedXml), signed.SignedContentHash);
        Assert.Equal(1, source.Calls);

        var document = Load(signed.SignedXml);
        var signatures = document.GetElementsByTagName("Signature", XmlDsigNamespace);
        Assert.Equal(1, signatures.Count);
        var signatureElement = Assert.IsType<XmlElement>(signatures[0]);
        Assert.Same(signatureElement, document.DocumentElement!.LastChild);

        var signedInfo = signatureElement["SignedInfo", XmlDsigNamespace]!;
        Assert.Equal(
            FiscalXmlSignatureProfile.InclusiveC14N,
            signedInfo["CanonicalizationMethod", XmlDsigNamespace]!.GetAttribute("Algorithm"));
        Assert.Equal(
            FiscalXmlSignatureProfile.RsaSha256,
            signedInfo["SignatureMethod", XmlDsigNamespace]!.GetAttribute("Algorithm"));
        var reference = signedInfo["Reference", XmlDsigNamespace]!;
        Assert.Equal(string.Empty, reference.GetAttribute("URI"));
        Assert.Equal(
            FiscalXmlSignatureProfile.Sha256,
            reference["DigestMethod", XmlDsigNamespace]!.GetAttribute("Algorithm"));
        Assert.Equal(
            FiscalXmlSignatureProfile.EnvelopedSignature,
            reference["Transforms", XmlDsigNamespace]!["Transform", XmlDsigNamespace]!.GetAttribute("Algorithm"));

        var verifier = new SignedXml(document);
        verifier.LoadXml(signatureElement);
        Assert.True(verifier.CheckSignature(certificate, verifySignatureOnly: true));

        var signedValidation = new DgiFeV1_44_2SignedDailyReportSchemaValidator().Validate(signed.SignedXml);
        Assert.True(signedValidation.IsValid, Errors(signedValidation.Errors));
        Assert.Equal("13.2", signedValidation.FunctionalFormatVersion);
        Assert.Equal("1.44.2", signedValidation.SchemaArchiveVersion);
        Assert.Equal(DgiFeV1_44_2SignedDailyReportSchemaValidator.SchemaSetId, signedValidation.SchemaSetId);
        Assert.NotEmpty(signedValidation.SchemaSetFingerprint);

        var preSignatureValidation = new DgiFeV1_44_2UnsignedDailyReportSchemaValidator().Validate(signed.SignedXml);
        Assert.Equal(FiscalDailyReportUnsignedSchemaValidationStatus.DocumentInvalid, preSignatureValidation.Status);
        Assert.Contains(preSignatureValidation.Errors, error => error.Code == "fiscal.daily_report.xsd.signature_forbidden");
    }

    [Fact]
    public async Task Signer_fails_closed_before_certificate_access_when_unsigned_hash_changed()
    {
        var artifact = new DeterministicUnsignedDailyReportXmlBuilder()
            .Build(Projection(CfeFamily.EFactura), SigningTimestamp);
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var source = new StaticCertificateSource(certificate);
        var signer = new XmlDsigFiscalDailyReportSignatureProvider(source);
        var request = Request(artifact) with { UnsignedContentHash = new string('0', 64) };

        var error = await Assert.ThrowsAsync<FiscalSignatureProviderException>(() => signer.SignAsync(request));

        Assert.Equal("fiscal.daily_report.signature.unsigned_hash_mismatch", error.Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task Signer_fails_closed_before_certificate_access_when_TmstFirmaEnv_does_not_match_frozen_timestamp()
    {
        var artifact = new DeterministicUnsignedDailyReportXmlBuilder()
            .Build(Projection(CfeFamily.EFactura), SigningTimestamp);
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var source = new StaticCertificateSource(certificate);
        var signer = new XmlDsigFiscalDailyReportSignatureProvider(source);
        var request = Request(artifact) with { SigningTimestamp = SigningTimestamp.AddSeconds(1) };

        var error = await Assert.ThrowsAsync<FiscalSignatureProviderException>(() => signer.SignAsync(request));

        Assert.Equal("fiscal.daily_report.signature.tmstfirmaenv_mismatch", error.Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public void Signed_validator_rejects_unsigned_report_instead_of_relaxing_signature_requirement()
    {
        var artifact = new DeterministicUnsignedDailyReportXmlBuilder()
            .Build(Projection(CfeFamily.ETicket), SigningTimestamp);

        var validation = new DgiFeV1_44_2SignedDailyReportSchemaValidator().Validate(artifact.Xml);

        Assert.Equal(FiscalDailyReportSignedSchemaValidationStatus.DocumentInvalid, validation.Status);
        Assert.Contains(validation.Errors, error => error.Code == "fiscal.daily_report.xsd.signature_required");
    }

    [Fact]
    public async Task Signed_validator_rejects_signature_that_is_not_final_child()
    {
        var artifact = new DeterministicUnsignedDailyReportXmlBuilder()
            .Build(Projection(CfeFamily.EFactura), SigningTimestamp);
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var signed = await new XmlDsigFiscalDailyReportSignatureProvider(new StaticCertificateSource(certificate))
            .SignAsync(Request(artifact));
        var document = XDocument.Parse(signed.SignedXml, LoadOptions.PreserveWhitespace);
        document.Root!.Add(new XElement(XName.Get("Unexpected", FiscalDailyReportV13_2WireContract.XmlNamespace)));

        var validation = new DgiFeV1_44_2SignedDailyReportSchemaValidator()
            .Validate(document.ToString(SaveOptions.DisableFormatting));

        Assert.Equal(FiscalDailyReportSignedSchemaValidationStatus.DocumentInvalid, validation.Status);
        Assert.Contains(validation.Errors, error => error.Code == "fiscal.daily_report.xsd.signature_position_invalid");
    }

    [Fact]
    public void Constructor_rejects_historical_SHA1_profile()
    {
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var source = new StaticCertificateSource(certificate);
        var sha1 = XmlDsigFiscalDailyReportSignatureProvider.DailyReportSha256EvidenceBackedV1 with
        {
            ProfileId = "historical-report-sha1-not-enabled",
            SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
            DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1"
        };

        var error = Assert.Throws<FiscalSignatureProviderException>(
            () => new XmlDsigFiscalDailyReportSignatureProvider(source, sha1));

        Assert.Equal("fiscal.daily_report.signature.profile_sha1_forbidden", error.Code);
    }

    private static FiscalDailyReportSignatureRequest Request(UnsignedDailyReportArtifact artifact) => new(
        "company-1",
        artifact.FormatVersion,
        artifact.ProjectionFingerprint,
        artifact.Xml,
        artifact.ContentHash,
        artifact.SigningTimestamp);

    private static FiscalDailyReportWireProjection Projection(CfeFamily family)
    {
        var row = new FiscalDailyReportWireAmountRow(
            family,
            new DateOnly(2026, 9, 11),
            "0001",
            false,
            NonTaxedAmount: 0m,
            ExportAmount: 0m,
            PerceivedTaxAmount: 0m,
            VatInSuspenseAmount: 0m,
            MinimumTaxableAmount: 0m,
            BasicTaxableAmount: 100m,
            OtherVatTaxableAmount: 0m,
            MinimumVatAmount: 0m,
            BasicVatAmount: 22m,
            OtherVatAmount: 0m,
            MinimumVatRatePercent: null,
            BasicVatRatePercent: 22m,
            TotalAmount: 122m,
            RetainedOrPerceivedAmount: 0m,
            FiscalCreditAmount: 0m);
        var counter = new FiscalDailyReportWireTypeCounters(
            family,
            UsedCount: 1,
            HighValueCount: 0,
            AnnulledCount: 0,
            EmittedCount: 1,
            UsedRanges: [new FiscalDailyReportNumberRange("A", 1, 1)],
            AnnulledRanges: Array.Empty<FiscalDailyReportNumberRange>());

        return new FiscalDailyReportWireProjection(
            "company-1",
            "214748364700",
            new DateOnly(2026, 9, 11),
            1,
            1,
            [row],
            [counter],
            Fingerprint('a'),
            Fingerprint('b'),
            Fingerprint('c'));
    }

    private static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=eFactura Daily Report XMLDSig Test, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
            critical: true));
        return request.CreateSelfSigned(notBefore, notAfter);
    }

    private static XmlDocument Load(string xml)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(xml);
        return document;
    }

    private static string Errors(IEnumerable<FiscalDailyReportUnsignedSchemaValidationError> errors) =>
        string.Join(Environment.NewLine, errors.Select(error => error.Message));

    private static string Errors(IEnumerable<FiscalDailyReportSignedSchemaValidationError> errors) =>
        string.Join(Environment.NewLine, errors.Select(error => error.Message));

    private static string Fingerprint(char value) => new(value, 64);
    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class StaticCertificateSource(X509Certificate2 certificate) : IFiscalDailyReportSigningCertificateSource
    {
        public int Calls { get; private set; }

        public ValueTask<X509Certificate2> GetSigningCertificateAsync(
            FiscalDailyReportSignatureRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(certificate);
        }
    }
}
