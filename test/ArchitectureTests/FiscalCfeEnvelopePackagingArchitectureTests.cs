using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopePackagingArchitectureTests
{
    [Fact]
    public void Application_packaging_is_read_only_and_transport_free()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopePackaging.cs");

        Assert.Contains("IFiscalSignedArtifactRepository", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalSignedCfeSchemaValidator", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeBuilder", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeSchemaValidator", source, StringComparison.Ordinal);
        Assert.Contains("MaxCfePerEnvelope = 250", source, StringComparison.Ordinal);
        Assert.Contains("fiscal.envelope.certificate_mismatch", source, StringComparison.Ordinal);
        Assert.Contains("source_hash_mismatch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRSAPrivateKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_builder_preserves_signed_fragment_and_never_uses_private_keys_or_network()
    {
        var source = Read("src/Infrastructure/Fiscal/DgiCfeEnvelopeBuilder.cs");

        Assert.Contains("WriteRaw(item.CfeRootFragment)", source, StringComparison.Ordinal);
        Assert.Contains("RootFragment(source.SignedXml)", source, StringComparison.Ordinal);
        Assert.Contains("X509Certificate", source, StringComparison.Ordinal);
        Assert.Contains("RUCEmisor", source, StringComparison.Ordinal);
        Assert.Contains("CantCFE", source, StringComparison.Ordinal);
        Assert.Contains("yyyy-MM-dd'T'HH:mm:sszzz", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRSAPrivateKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ComputeSignature", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Envelope_schema_is_byte_pinned_and_embedded_with_no_external_resolution()
    {
        var validator = Read("src/Infrastructure/Fiscal/DgiFeV1_44_2CfeEnvelopeSchemaValidator.cs");
        var manifest = Read("src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2/envelope-schema-manifest.json");
        var xsdPath = Full("src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2/EnvioCFE.xsd");
        var xsdBytes = File.ReadAllBytes(xsdPath);
        var xsdHash = Convert.ToHexString(SHA256.HashData(xsdBytes)).ToLowerInvariant();
        var csproj = Read("src/Infrastructure/Infrastructure.csproj");

        Assert.Equal("38b411942eda7c229cf4048965654245ba6218f6079a00acfcbe1804e8daa58f", xsdHash);
        Assert.Contains("3209b5b854bcbf23694c8ae2561509b568d5d263", manifest, StringComparison.Ordinal);
        Assert.Contains("EnvioCFE.xsd", validator, StringComparison.Ordinal);
        Assert.Contains("envelope-schema-manifest.json", validator, StringComparison.Ordinal);
        Assert.Contains("DtdProcessing = DtdProcessing.Prohibit", validator, StringComparison.Ordinal);
        Assert.Contains("External schema resolution is forbidden", validator, StringComparison.Ordinal);
        Assert.Contains("envelope-schema-manifest.json", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_registers_builder_validator_and_use_case()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalCfeEnvelopeBuilder, DgiCfeEnvelopeBuilder", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeSchemaValidator, DgiFeV1_44_2CfeEnvelopeSchemaValidator", services, StringComparison.Ordinal);
        Assert.Contains("PackageFiscalCfeEnvelopeUseCase", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_keeps_packaging_separate_from_transport_and_testing_readiness_blocked()
    {
        var docs = Read("documentation/blueprint-api-implementation/56_FISCAL_SOBRE_V05_PACKAGING.md");

        Assert.Contains("1..250", docs, StringComparison.Ordinal);
        Assert.Contains("same certificate", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not persist", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not submit", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
