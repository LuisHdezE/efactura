using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportXmlDsigArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Daily_report_signing_contract_stays_in_Application_without_crypto_implementation_details()
    {
        var contracts = Read("src/Application/Fiscal/FiscalDailyReportSigning.cs");

        Assert.Contains("IFiscalDailyReportSignatureProvider", contracts, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportSignedSchemaValidator", contracts, StringComparison.Ordinal);
        Assert.Contains("FiscalDailyReportSignatureRequest", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("SignedXml", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", contracts, StringComparison.Ordinal);
    }

    [Fact]
    public void Daily_report_XMLDSig_adapter_is_report_specific_and_forbids_SHA1()
    {
        var provider = Read("src/Infrastructure/Fiscal/XmlDsigFiscalDailyReportSignatureProvider.cs");

        Assert.Contains("dgi-daily-report-sha256-evidence-backed-v1", provider, StringComparison.Ordinal);
        Assert.Contains("FiscalXmlSignatureProfile.RsaSha256", provider, StringComparison.Ordinal);
        Assert.Contains("FiscalXmlSignatureProfile.Sha256", provider, StringComparison.Ordinal);
        Assert.Contains("FiscalXmlSignatureProfile.EnvelopedSignature", provider, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportSigningCertificateSource", provider, StringComparison.Ordinal);
        Assert.Contains("TmstFirmaEnv", provider, StringComparison.Ordinal);
        Assert.Contains("profile_sha1_forbidden", provider, StringComparison.Ordinal);
        Assert.Contains("signature_position_invalid", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACRECEPCIONREPORTE", provider, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Signed_report_validator_compiles_untouched_pinned_root_and_never_relaxes_signature()
    {
        var validator = Read("src/Infrastructure/Fiscal/DgiFeV1_44_2SignedDailyReportSchemaValidator.cs");

        Assert.Contains("ReporteDiarioCFE.xsd", validator, StringComparison.Ordinal);
        Assert.Contains("daily-report-schema-manifest.json", validator, StringComparison.Ordinal);
        Assert.Contains("resources.Bytes[RootSchema]", validator, StringComparison.Ordinal);
        Assert.Contains("signatureIsRequiredFinalChild", validator, StringComparison.Ordinal);
        Assert.Contains("DtdProcessing = DtdProcessing.Prohibit", validator, StringComparison.Ordinal);
        Assert.Contains("External schema resolution is forbidden", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("UnsignedSignatureDeclaration", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("RelaxOnlySignatureRequirement", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("minOccurs=\"0\"", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", validator, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_composition_reuses_organization_scoped_PFX_source_without_collapsing_artifact_contracts()
    {
        var pfx = Read("src/Infrastructure/Fiscal/PfxFiscalSigningCertificateSource.cs");
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalSigningCertificateSource", pfx, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportSigningCertificateSource", pfx, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportSigningCertificateSource>(sp =>", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportSignatureProvider, XmlDsigFiscalDailyReportSignatureProvider", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportSignedSchemaValidator, DgiFeV1_44_2SignedDailyReportSchemaValidator", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportUnsignedSchemaValidator, DgiFeV1_44_2UnsignedDailyReportSchemaValidator", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Implementation_record_keeps_transport_and_formal_DGI_testing_as_later_gates()
    {
        var document = Read("documentation/blueprint-api-implementation/47_FISCAL_DAILY_REPORT_XMLDSIG_AND_SIGNED_XSD.md");

        Assert.Contains("RSA-SHA256", document, StringComparison.Ordinal);
        Assert.Contains("untouched", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EFACRECEPCIONREPORTE", document, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", document, StringComparison.Ordinal);
        Assert.DoesNotContain("READY FOR DGI TESTING", document, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing api-accounting.sln was not found.");
    }
}
