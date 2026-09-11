using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using EFactura.Application.Fiscal;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportUnsignedXmlTests
{
    private static readonly XNamespace Cfe = FiscalDailyReportV13_2WireContract.XmlNamespace;
    private static readonly XNamespace Dsig = FiscalDailyReportV13_2WireContract.XmlDigitalSignatureNamespace;
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 11, 18, 30, 45, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(101, "Rsmn_Tck", true)]
    [InlineData(102, "Rsmn_Tck_Nota_Credito", true)]
    [InlineData(103, "Rsmn_Tck_Nota_Debito", true)]
    [InlineData(111, "Rsmn_Fac", false)]
    [InlineData(112, "Rsmn_Fac_Nota_Credito", false)]
    [InlineData(113, "Rsmn_Fac_Nota_Debito", false)]
    public void Builder_maps_supported_release1_families_and_unsigned_payload_validates_structurally(
        int familyCode,
        string summaryElement,
        bool expectsHighValueCounter)
    {
        var family = (CfeFamily)familyCode;
        var projection = Projection(family);
        var builder = new DeterministicUnsignedDailyReportXmlBuilder();

        var artifact = builder.Build(projection, SigningTimestamp);
        var document = XDocument.Parse(artifact.Xml);
        var root = document.Root;
        Assert.NotNull(root);

        Assert.Equal(Cfe + "Reporte", root.Name);
        Assert.Empty(root.Descendants(Dsig + "Signature"));
        var caratula = root!.Element(Cfe + "Caratula");
        Assert.NotNull(caratula);
        Assert.Equal("1.0", caratula.Attribute("version")?.Value);
        Assert.Equal("214748364700", caratula.Element(Cfe + "RUCEmisor")?.Value);
        Assert.Equal("2026-09-11", caratula.Element(Cfe + "FechaResumen")?.Value);
        Assert.Equal("1", caratula.Element(Cfe + "SecEnvio")?.Value);
        Assert.Equal("2026-09-11T18:30:45-03:00", caratula.Element(Cfe + "TmstFirmaEnv")?.Value);
        Assert.Equal("1", caratula.Element(Cfe + "CantComprobantes")?.Value);

        var summary = root.Element(Cfe + summaryElement);
        Assert.NotNull(summary);
        Assert.Equal(familyCode.ToString(), summary.Element(Cfe + "TipoComp")?.Value);
        var data = summary!.Element(Cfe + "RsmnData");
        Assert.NotNull(data);
        Assert.Equal("1", data.Element(Cfe + "CantDocsUtil")?.Value);
        Assert.Equal("0", data.Element(Cfe + "CantDocsAnulados")?.Value);
        Assert.Equal("1", data.Element(Cfe + "CantDocsEmi")?.Value);
        if (expectsHighValueCounter)
            Assert.Equal("0", data.Element(Cfe + "CantDocsMay_topeUI")?.Value);
        else
            Assert.Null(data.Element(Cfe + "CantDocsMay_topeUI"));

        var amount = data!.Descendants(Cfe + "Mnts_FyT_Item").SingleOrDefault();
        Assert.NotNull(amount);
        Assert.Equal("100.00", amount.Element(Cfe + "TotMntIVATasaBas")?.Value);
        Assert.Equal("22.00", amount.Element(Cfe + "MntIVATasaBas")?.Value);
        Assert.Equal("22", amount.Element(Cfe + "IVATasaBas")?.Value);
        Assert.Equal("122.00", amount.Element(Cfe + "TotMntTotal")?.Value);
        Assert.Equal("1", amount.Element(Cfe + "CodSuc")?.Value);

        Assert.Equal(Sha256(artifact.Xml), artifact.ContentHash);
        Assert.Equal(projection.ProjectionFingerprint, artifact.ProjectionFingerprint);

        var validation = new DgiFeV1_44_2UnsignedDailyReportSchemaValidator().Validate(artifact.Xml);
        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors.Select(error => error.Message)));
        Assert.True(validation.SignatureRequirementRelaxedForUnsignedValidation);
        Assert.Equal("13.2", validation.FunctionalFormatVersion);
        Assert.Equal("1.44.2", validation.SchemaArchiveVersion);
        Assert.NotEmpty(validation.SchemaSetFingerprint);
    }

    [Fact]
    public void Builder_emits_third_party_indicator_only_for_explicit_true_source_fact()
    {
        var builder = new DeterministicUnsignedDailyReportXmlBuilder();
        var trueXml = XDocument.Parse(builder.Build(Projection(CfeFamily.ETicket, thirdParty: true), SigningTimestamp).Xml);
        var falseXml = XDocument.Parse(builder.Build(Projection(CfeFamily.ETicket, thirdParty: false), SigningTimestamp).Xml);

        Assert.Equal("1", trueXml.Descendants(Cfe + "IndPagCta3ros").Single().Value);
        Assert.Empty(falseXml.Descendants(Cfe + "IndPagCta3ros"));
    }

    [Fact]
    public void Zero_count_report_omits_resumen_and_is_structurally_valid_before_signature()
    {
        var projection = new FiscalDailyReportWireProjection(
            "company-1",
            "214748364700",
            new DateOnly(2026, 9, 11),
            1,
            0,
            Array.Empty<FiscalDailyReportWireAmountRow>(),
            Array.Empty<FiscalDailyReportWireTypeCounters>(),
            Fingerprint('a'),
            Fingerprint('b'),
            Fingerprint('c'));

        var artifact = new DeterministicUnsignedDailyReportXmlBuilder().Build(projection, SigningTimestamp);
        var document = XDocument.Parse(artifact.Xml);
        Assert.Single(document.Root!.Elements());
        Assert.Equal(Cfe + "Caratula", document.Root.Elements().Single().Name);

        var validation = new DgiFeV1_44_2UnsignedDailyReportSchemaValidator().Validate(artifact.Xml);
        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors.Select(error => error.Message)));
    }

    [Fact]
    public void Builder_is_deterministic_for_same_projection_and_signing_timestamp()
    {
        var projection = Projection(CfeFamily.EFactura);
        var builder = new DeterministicUnsignedDailyReportXmlBuilder();

        var first = builder.Build(projection, SigningTimestamp);
        var second = builder.Build(projection, SigningTimestamp);

        Assert.Equal(first.Xml, second.Xml);
        Assert.Equal(first.ContentHash, second.ContentHash);
    }

    [Fact]
    public void Builder_rejects_subsecond_signing_timestamp_instead_of_silently_rounding()
    {
        var error = Assert.Throws<DomainRuleException>(() =>
            new DeterministicUnsignedDailyReportXmlBuilder().Build(
                Projection(CfeFamily.EFactura),
                SigningTimestamp.AddTicks(1)));

        Assert.Equal("fiscal.daily_report.xml.signing_timestamp_precision_invalid", error.Code);
    }

    [Fact]
    public void Unsigned_validator_rejects_signature_presence_and_does_not_pretend_to_be_full_signed_validation()
    {
        var artifact = new DeterministicUnsignedDailyReportXmlBuilder().Build(
            Projection(CfeFamily.EFactura),
            SigningTimestamp);
        var document = XDocument.Parse(artifact.Xml);
        document.Root!.Add(new XElement(Dsig + "Signature"));

        var validation = new DgiFeV1_44_2UnsignedDailyReportSchemaValidator()
            .Validate(document.ToString(SaveOptions.DisableFormatting));

        Assert.Equal(FiscalDailyReportUnsignedSchemaValidationStatus.DocumentInvalid, validation.Status);
        Assert.Contains(validation.Errors, error => error.Code == "fiscal.daily_report.xsd.signature_forbidden");
        Assert.True(validation.SignatureRequirementRelaxedForUnsignedValidation);
    }

    [Fact]
    public void Unsigned_validator_rejects_wrong_summary_element_for_type()
    {
        var artifact = new DeterministicUnsignedDailyReportXmlBuilder().Build(
            Projection(CfeFamily.ETicket),
            SigningTimestamp);
        var tampered = artifact.Xml.Replace("Rsmn_Tck", "Rsmn_Fac", StringComparison.Ordinal);

        var validation = new DgiFeV1_44_2UnsignedDailyReportSchemaValidator().Validate(tampered);

        Assert.Equal(FiscalDailyReportUnsignedSchemaValidationStatus.DocumentInvalid, validation.Status);
        Assert.NotEmpty(validation.Errors);
    }

    private static FiscalDailyReportWireProjection Projection(CfeFamily family, bool thirdParty = false)
    {
        var row = new FiscalDailyReportWireAmountRow(
            family,
            new DateOnly(2026, 9, 11),
            "0001",
            thirdParty,
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

    private static string Fingerprint(char value) => new(value, 64);
    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
