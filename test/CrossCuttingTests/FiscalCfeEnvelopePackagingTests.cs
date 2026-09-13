using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopePackagingTests
{
    private const string CfeSchemaFingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string EnvelopeSchemaFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 13, 0, 15, 30, TimeSpan.FromHours(-3));

    [Fact]
    public async Task Application_packages_durable_sources_without_write_or_transport_side_effects()
    {
        var first = DummyArtifact(Guid.Parse("81000000-0000-0000-0000-000000000001"), "thumb", "serial");
        var second = DummyArtifact(Guid.Parse("81000000-0000-0000-0000-000000000002"), "thumb", "serial");
        var repository = new FakeSignedArtifactRepository(first, second);
        var builder = new FakeEnvelopeBuilder("thumb", "serial", "<EnvioCFE />");
        var useCase = new PackageFiscalCfeEnvelopeUseCase(
            repository,
            new FakeSignedCfeValidator(),
            builder,
            new FakeEnvelopeValidator());

        var result = await useCase.ExecuteAsync(new(
            "company-envelope",
            "219999830019",
            "214748364700",
            2020,
            new DateTimeOffset(2026, 9, 13, 0, 20, 0, TimeSpan.FromHours(-3)),
            [first.FiscalDocumentId, second.FiscalDocumentId]));

        Assert.Equal(2, result.CfeCount);
        Assert.Equal("thumb", result.CertificateThumbprint);
        Assert.Equal("serial", result.CertificateSerialNumber);
        Assert.Equal(Sha256("<EnvioCFE />"), result.EnvelopeSha256);
        Assert.Equal(2, repository.ReadCount);
        Assert.Equal(1, builder.BuildCount);
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task Application_rejects_mixed_certificates_fail_closed()
    {
        var first = DummyArtifact(Guid.Parse("82000000-0000-0000-0000-000000000001"), "thumb-a", "serial-a");
        var second = DummyArtifact(Guid.Parse("82000000-0000-0000-0000-000000000002"), "thumb-b", "serial-b");
        var useCase = new PackageFiscalCfeEnvelopeUseCase(
            new FakeSignedArtifactRepository(first, second),
            new FakeSignedCfeValidator(),
            new FakeEnvelopeBuilder("thumb-a", "serial-a", "<EnvioCFE />"),
            new FakeEnvelopeValidator());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => useCase.ExecuteAsync(new(
            "company-envelope",
            "219999830019",
            "214748364700",
            2021,
            SigningTimestamp,
            [first.FiscalDocumentId, second.FiscalDocumentId])));

        Assert.Equal("fiscal.envelope.certificate_mismatch", error.Code);
    }

    [Fact]
    public async Task Application_rejects_more_than_250_cfe_before_repository_reads()
    {
        var repository = new FakeSignedArtifactRepository();
        var ids = Enumerable.Range(1, 251)
            .Select(_ => Guid.NewGuid())
            .ToArray();
        var useCase = new PackageFiscalCfeEnvelopeUseCase(
            repository,
            new FakeSignedCfeValidator(),
            new FakeEnvelopeBuilder("thumb", "serial", "<EnvioCFE />"),
            new FakeEnvelopeValidator());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => useCase.ExecuteAsync(new(
            "company-envelope",
            "219999830019",
            "214748364700",
            2022,
            SigningTimestamp,
            ids)));

        Assert.Equal("fiscal.envelope.cfe_count_invalid", error.Code);
        Assert.Equal(0, repository.ReadCount);
    }

    [Fact]
    public async Task Application_rejects_persisted_signed_xml_hash_drift()
    {
        var original = DummyArtifact(Guid.Parse("83000000-0000-0000-0000-000000000001"), "thumb", "serial");
        var corrupt = original with { SignedXml = "<changed />" };
        var useCase = new PackageFiscalCfeEnvelopeUseCase(
            new FakeSignedArtifactRepository(corrupt),
            new FakeSignedCfeValidator(),
            new FakeEnvelopeBuilder("thumb", "serial", "<EnvioCFE />"),
            new FakeEnvelopeValidator());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => useCase.ExecuteAsync(new(
            "company-envelope",
            "219999830019",
            "214748364700",
            2023,
            SigningTimestamp,
            [corrupt.FiscalDocumentId])));

        Assert.Equal("fiscal.envelope.source_hash_mismatch", error.Code);
    }

    [Fact]
    public async Task Real_builder_preserves_signed_root_and_generated_Sobre_validates_against_pinned_XSD()
    {
        var signed = await BuildSignedCfe();
        var source = new FiscalCfeEnvelopeSource(
            signed.FiscalDocumentId,
            signed.SignedContentHash,
            signed.CertificateThumbprint,
            signed.CertificateSerialNumber,
            signed.SignedXml);
        var builder = new DgiCfeEnvelopeBuilder();

        var envelope = builder.Build(new FiscalCfeEnvelopeBuildRequest(
            "219999830019",
            "214748364700",
            2024,
            new DateTimeOffset(2026, 9, 13, 0, 30, 0, TimeSpan.FromHours(-3)),
            [source]));

        var validation = new DgiFeV1_44_2CfeEnvelopeSchemaValidator().Validate(envelope.Xml);
        Assert.True(validation.IsValid, string.Join(" | ", validation.Errors.Select(x => x.Message)));
        Assert.Equal(FiscalCfeEnvelopeSchemaValidationStatus.Valid, validation.Status);
        Assert.Equal(DgiFeV1_44_2CfeEnvelopeSchemaValidator.SchemaSetId, validation.SchemaSetId);
        Assert.Equal(DgiFeV1_44_2CfeEnvelopeSchemaValidator.SchemaVersion, validation.SchemaVersion);
        Assert.Matches("^[0-9a-f]{64}$", validation.SchemaSetFingerprint);

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", envelope.Xml, StringComparison.Ordinal);
        Assert.Equal(1, Count(envelope.Xml, "<?xml"));
        Assert.Contains(RootFragment(signed.SignedXml), envelope.Xml, StringComparison.Ordinal);

        var parsed = XDocument.Parse(envelope.Xml, LoadOptions.PreserveWhitespace);
        XNamespace cfe = "http://cfe.dgi.gub.uy";
        var caratula = parsed.Root!.Element(cfe + "Caratula")!;
        Assert.Equal("219999830019", caratula.Element(cfe + "RutReceptor")!.Value);
        Assert.Equal("214748364700", caratula.Element(cfe + "RUCEmisor")!.Value);
        Assert.Equal("2024", caratula.Element(cfe + "Idemisor")!.Value);
        Assert.Equal("1", caratula.Element(cfe + "CantCFE")!.Value);
        Assert.Single(parsed.Root!.Elements(cfe + "CFE"));
        Assert.Equal(Convert.ToBase64String(signed.CertificateRawData), caratula.Element(cfe + "X509Certificate")!.Value);
    }

    private static StoredFiscalSignedArtifact DummyArtifact(Guid documentId, string thumbprint, string serial)
    {
        const string xml = "<signed />";
        return new StoredFiscalSignedArtifact(
            Guid.NewGuid(),
            "company-envelope",
            documentId,
            Guid.NewGuid(),
            new string('c', 64),
            new string('d', 64),
            new string('e', 64),
            Sha256(xml),
            SigningTimestamp,
            "test-profile",
            thumbprint,
            serial,
            "test-cfe-schema",
            "1.0",
            CfeSchemaFingerprint,
            xml);
    }

    private static async Task<RealSignedCfe> BuildSignedCfe()
    {
        var (document, snapshot) = Fixture();
        var unsigned = new DeterministicUnsignedCfeBuilder().Build(document, snapshot);
        var evidence = FiscalSigningEvidence.Establish(
            Guid.Parse("84000000-0000-0000-0000-000000000001"),
            document.OrganizationId,
            document.Id,
            unsigned.ContentFingerprint,
            Sha256(unsigned.Xml),
            SigningTimestamp);
        var payload = new DeterministicFiscalSigningPayloadBuilder().Build(document.Id, unsigned, evidence);

        using var certificate = CreateCertificate(SigningTimestamp.AddDays(-1), SigningTimestamp.AddDays(30));
        var signer = new XmlDsigFiscalSignatureProvider(new StaticCertificateSource(certificate));
        var result = await signer.SignAsync(new FiscalSignatureRequest(
            document.Id,
            payload.Family,
            payload.FormatVersion,
            payload.FiscalContentFingerprint,
            payload.UnsignedContentHash,
            payload.Xml,
            payload.ContentHash,
            payload.SigningTimestamp));

        return new RealSignedCfe(
            document.Id,
            result.SignedXml,
            result.SignedContentHash,
            result.CertificateThumbprint,
            result.CertificateSerialNumber,
            certificate.RawData.ToArray());
    }

    private static (FiscalDocument Document, FiscalContentSnapshot Snapshot) Fixture()
    {
        var saleId = Guid.Parse("85000000-0000-0000-0000-000000000001");
        var lineId = Guid.Parse("85000000-0000-0000-0000-000000000002");
        var rule = new RegulatoryRuleEvidence(
            "TEST-CFE-RULE", "DGI test evidence", "https://example.invalid/dgi-rule", "25.2-test",
            new DateOnly(2026, 1, 1), new DateOnly(2027, 12, 31), "test clause");
        var calculation = new CfeArithmeticResult(
            "UYU", "25.2", "UY-CFE-25.2-ARITH-R1",
            [new CfeArithmeticLineResult(lineId, 100m, VatLiabilityKind.VatDue, VatRateKind.Basic, 22m, [rule], "UY-VAT-RATE-R1")],
            new CfeArithmeticTotals(100m, 0m, 100m, 0m, 0m, 22m, 22m, 122m),
            [rule]);
        var settlement = new FiscalSettlementEvidence(FiscalSettlementKind.ImmediatePayment, FiscalPaymentForm.Cash, null);
        var fiscal = FiscalConfirmationEvidence.Capture(
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            new string('a', 64),
            calculation,
            [rule],
            settlement);
        var issuer = new FiscalIssuerContentSnapshot(
            "214748364700", "Empresa Snapshot S.A.", "Empresa Snapshot", 3, "loc-1", "0001",
            "Av. Italia 1234", "Montevideo", "Montevideo", 4);
        var receiver = new FiscalReceiverContentSnapshot(
            Guid.Parse("85000000-0000-0000-0000-000000000004"), "Cliente Snapshot S.A.", "UY", "UY", 5,
            new FiscalReceiverIdentitySnapshot("2", "219999990017", "UY"),
            new FiscalReceiverAddressSnapshot(
                Guid.Parse("85000000-0000-0000-0000-000000000005"), FiscalReceiverAddressKind.Fiscal,
                "18 de Julio 2000", "Montevideo", "Montevideo", "UY", "11200"));
        var snapshot = FiscalContentSnapshot.Create(
            "company-envelope", saleId, CfeFamily.EFactura, ReceiverIdentificationRequirement.Required,
            "25.2", new DateOnly(2026, 9, 13), new string('a', 64), new string('b', 64), issuer, receiver,
            [new FiscalContentLineSnapshot(
                1, lineId, Guid.Parse("85000000-0000-0000-0000-000000000003"), "ITEM-001",
                "Producto confirmado", FiscalContentLineKind.Goods, 1m, 100m, 0m, 0m,
                fiscal.Lines.Single(), "UNI")],
            fiscal);
        var document = FiscalDocument.CreateIdentity(
            Guid.Parse("86000000-0000-0000-0000-000000000001"), "company-envelope",
            Guid.Parse("86000000-0000-0000-0000-000000000002"), saleId,
            Guid.Parse("86000000-0000-0000-0000-000000000003"),
            Guid.Parse("86000000-0000-0000-0000-000000000004"), null,
            CfeFamily.EFactura, "A", 100, "90260000001", 1, 1000,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 12, 31), new DateOnly(2026, 9, 13),
            "loc-1", "terminal-1", ReceiverIdentificationRequirement.Required, "25.2",
            new string('a', 64), new string('b', 64),
            "UYU", 100m, 22m, 122m, new DateTimeOffset(2026, 9, 13, 0, 0, 0, TimeSpan.FromHours(-3)));
        return (document, snapshot);
    }

    private static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=eFactura Sobre Test, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
            critical: true));
        return request.CreateSelfSigned(notBefore, notAfter);
    }

    private static string RootFragment(string xml)
    {
        var start = xml.IndexOf("?>", StringComparison.Ordinal);
        return start >= 0 ? xml[(start + 2)..].TrimStart() : xml.TrimStart();
    }

    private static int Count(string value, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FakeSignedArtifactRepository(params StoredFiscalSignedArtifact[] artifacts) : IFiscalSignedArtifactRepository
    {
        private readonly Dictionary<Guid, StoredFiscalSignedArtifact> _artifacts = artifacts.ToDictionary(x => x.FiscalDocumentId);
        public int ReadCount { get; private set; }
        public int AddCount { get; private set; }

        public Task<StoredFiscalSignedArtifact?> GetByFiscalDocumentAsync(
            string organizationId,
            Guid fiscalDocumentId,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            _artifacts.TryGetValue(fiscalDocumentId, out var artifact);
            return Task.FromResult(artifact);
        }

        public Task AddAsync(StoredFiscalSignedArtifact artifact, CancellationToken cancellationToken = default)
        {
            AddCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSignedCfeValidator : IFiscalSignedCfeSchemaValidator
    {
        public FiscalSignedCfeSchemaValidationResult Validate(string signedXml) => new(
            FiscalSignedCfeSchemaValidationStatus.Valid,
            "test-cfe-schema",
            "1.0",
            CfeSchemaFingerprint,
            Array.Empty<FiscalSignedCfeSchemaValidationError>());
    }

    private sealed class FakeEnvelopeValidator : IFiscalCfeEnvelopeSchemaValidator
    {
        public FiscalCfeEnvelopeSchemaValidationResult Validate(string envelopeXml) => new(
            FiscalCfeEnvelopeSchemaValidationStatus.Valid,
            "test-envelope-schema",
            "1.44.2",
            EnvelopeSchemaFingerprint,
            Array.Empty<FiscalCfeEnvelopeSchemaValidationError>());
    }

    private sealed class FakeEnvelopeBuilder(string thumbprint, string serial, string xml) : IFiscalCfeEnvelopeBuilder
    {
        public int BuildCount { get; private set; }
        public FiscalCfeEnvelopeBuildArtifact Build(FiscalCfeEnvelopeBuildRequest request)
        {
            BuildCount++;
            return new(xml, thumbprint, serial);
        }
    }

    private sealed class StaticCertificateSource(X509Certificate2 certificate) : IFiscalSigningCertificateSource
    {
        public ValueTask<X509Certificate2> GetSigningCertificateAsync(
            FiscalSignatureRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(certificate);
    }

    private sealed record RealSignedCfe(
        Guid FiscalDocumentId,
        string SignedXml,
        string SignedContentHash,
        string CertificateThumbprint,
        string CertificateSerialNumber,
        byte[] CertificateRawData);
}
