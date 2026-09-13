using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeTransportArchitectureTests
{
    [Fact]
    public void Application_owns_durable_transport_lifecycle_without_HTTP_or_ACK_business_semantics()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopeTransport.cs");

        Assert.Contains("Prepared", source, StringComparison.Ordinal);
        Assert.Contains("InFlight", source, StringComparison.Ordinal);
        Assert.Contains("ResponseReceived", source, StringComparison.Ordinal);
        Assert.Contains("Unknown", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeTransportGateway", source, StringComparison.Ordinal);
        Assert.Contains("reconciliation_required", source, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRSAPrivateKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GZipStream", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ACKSobre", source, StringComparison.Ordinal);
        Assert.DoesNotContain("S08", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DGI_gateway_uses_EFACRECEPCIONSOBRE_direct_CDATA_and_keeps_response_opaque()
    {
        var gateway = Read("src/Infrastructure/Fiscal/DgiWsSecurityFiscalCfeEnvelopeTransportGateway.cs");

        Assert.Contains("WS_eFactura.EFACRECEPCIONSOBRE", gateway, StringComparison.Ordinal);
        Assert.Contains("CreateCDataSection(envelopeXml)", gateway, StringComparison.Ordinal);
        Assert.Contains("BinarySecurityToken", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigRSASHA1Url", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigSHA1Url", gateway, StringComparison.Ordinal);
        Assert.Contains("Dataout", gateway, StringComparison.Ordinal);
        Assert.Contains("xmlData", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("GZipStream", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("CompressionLevel", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("IDReceptor", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("S08", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("AckStateCode", gateway, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_repository_serializes_dispatch_by_durable_envelope_identity()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeSubmissionRepository.cs");
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeEnvelopeRecords.cs");
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceEnvelopeModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260913052000_V1FiscalCfeEnvelopeTransport.cs");

        Assert.Contains("FOR UPDATE", repository, StringComparison.Ordinal);
        Assert.Contains("Npgsql", repository, StringComparison.Ordinal);
        Assert.Contains("MySql", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", repository, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelope_submissions", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fces_operation", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fces_envelope", model, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", model, StringComparison.Ordinal);

        Assert.Contains("FK_v1_fces_envelope", customizer, StringComparison.Ordinal);
        Assert.Contains("DeleteBehavior.Restrict", customizer, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelope_submissions", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fces_operation", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fces_envelope", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fces_envelope", migration, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Restrict", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_registers_transport_boundary_and_use_cases()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalCfeEnvelopeTransportClock, SystemFiscalCfeEnvelopeTransportClock", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeTransportGateway, DgiWsSecurityFiscalCfeEnvelopeTransportGateway", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeSubmissionRepository, EfFiscalCfeEnvelopeSubmissionRepository", services, StringComparison.Ordinal);
        Assert.Contains("PrepareFiscalCfeEnvelopeSubmissionUseCase", services, StringComparison.Ordinal);
        Assert.Contains("DispatchFiscalCfeEnvelopeSubmissionUseCase", services, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
