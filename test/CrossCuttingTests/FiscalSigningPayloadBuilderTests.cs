using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using EFactura.Application.Fiscal;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalSigningPayloadBuilderTests
{
    private const string Fingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly Guid DocumentId = Guid.Parse("61000000-0000-0000-0000-000000000001");
    private static readonly XNamespace CfeNamespace = "http://cfe.dgi.gub.uy";

    [Fact]
    public void Build_inserts_durable_TmstFirma_before_Encabezado_with_preserved_offset()
    {
        var unsigned = Artifact(BaseXml());
        var evidence = Evidence(unsigned, new DateTimeOffset(2026, 9, 9, 13, 14, 15, 987, TimeSpan.FromHours(-3)));
        var sut = new DeterministicFiscalSigningPayloadBuilder();

        var result = sut.Build(DocumentId, unsigned, evidence);

        var document = XDocument.Parse(result.Xml);
        var family = document.Root!.Element(CfeNamespace + "eFact")!;
        var children = family.Elements().Select(element => element.Name.LocalName).ToArray();

        Assert.Equal("TmstFirma", children[0]);
        Assert.Equal("Encabezado", children[1]);
        Assert.Equal("2026-09-09T13:14:15-03:00", family.Element(CfeNamespace + "TmstFirma")!.Value);
        Assert.Equal(evidence.SigningTimestamp, result.SigningTimestamp);
        Assert.Equal(Sha256(unsigned.Xml), result.UnsignedContentHash);
        Assert.Equal(Sha256(result.Xml), result.ContentHash);
        Assert.DoesNotContain("TmstFirma", unsigned.Xml, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_is_deterministic_for_same_unsigned_content_and_durable_evidence()
    {
        var unsigned = Artifact(BaseXml());
        var evidence = Evidence(unsigned, new DateTimeOffset(2026, 9, 9, 13, 14, 15, TimeSpan.FromHours(-3)));
        var sut = new DeterministicFiscalSigningPayloadBuilder();

        var first = sut.Build(DocumentId, unsigned, evidence);
        var replay = sut.Build(DocumentId, unsigned, evidence);

        Assert.Equal(first.Xml, replay.Xml);
        Assert.Equal(first.ContentHash, replay.ContentHash);
        Assert.Equal(first.SigningTimestamp, replay.SigningTimestamp);
    }

    [Fact]
    public void Build_fails_closed_when_unsigned_hash_no_longer_matches_durable_evidence()
    {
        var original = Artifact(BaseXml());
        var evidence = Evidence(original, new DateTimeOffset(2026, 9, 9, 13, 14, 15, TimeSpan.FromHours(-3)));
        var changed = Artifact(BaseXml().Replace("<Detalle />", "<Detalle><Changed /></Detalle>", StringComparison.Ordinal));
        var sut = new DeterministicFiscalSigningPayloadBuilder();

        var error = Assert.Throws<DomainRuleException>(() => sut.Build(DocumentId, changed, evidence));

        Assert.Equal("fiscal.signing_payload.unsigned_hash_mismatch", error.Code);
    }

    [Fact]
    public void Build_fails_closed_when_TmstFirma_is_already_present()
    {
        var xml = BaseXml().Replace(
            "<Encabezado />",
            "<TmstFirma>2026-09-09T13:14:15-03:00</TmstFirma><Encabezado />",
            StringComparison.Ordinal);
        var unsigned = Artifact(xml);
        var evidence = Evidence(unsigned, new DateTimeOffset(2026, 9, 9, 13, 14, 15, TimeSpan.FromHours(-3)));
        var sut = new DeterministicFiscalSigningPayloadBuilder();

        var error = Assert.Throws<DomainRuleException>(() => sut.Build(DocumentId, unsigned, evidence));

        Assert.Equal("fiscal.signing_payload.tmstfirma_already_present", error.Code);
    }

    [Fact]
    public void Build_fails_closed_when_dsSignature_is_already_present()
    {
        var xml = BaseXml().Replace(
            "</CFE>",
            "<ds:Signature xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\" /></CFE>",
            StringComparison.Ordinal);
        var unsigned = Artifact(xml);
        var evidence = Evidence(unsigned, new DateTimeOffset(2026, 9, 9, 13, 14, 15, TimeSpan.FromHours(-3)));
        var sut = new DeterministicFiscalSigningPayloadBuilder();

        var error = Assert.Throws<DomainRuleException>(() => sut.Build(DocumentId, unsigned, evidence));

        Assert.Equal("fiscal.signing_payload.signature_already_present", error.Code);
    }

    private static UnsignedCfeArtifact Artifact(string xml) =>
        new(CfeFamily.EFactura, "25.2", Fingerprint, xml);

    private static FiscalSigningEvidence Evidence(UnsignedCfeArtifact unsigned, DateTimeOffset timestamp) =>
        FiscalSigningEvidence.Establish(
            Guid.Parse("61000000-0000-0000-0000-000000000002"),
            "company-1",
            DocumentId,
            Fingerprint,
            Sha256(unsigned.Xml),
            timestamp);

    private static string BaseXml() =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><CFE version=\"1.0\" xmlns=\"http://cfe.dgi.gub.uy\"><eFact><Encabezado /><Detalle /><CAEData /></eFact></CFE>";

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
