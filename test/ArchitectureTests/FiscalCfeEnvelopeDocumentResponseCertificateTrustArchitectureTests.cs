using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeDocumentResponseCertificateTrustArchitectureTests
{
    [Fact]
    public void Application_owns_ACKCFE_trust_policy_without_X509_or_infrastructure_dependencies()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseCertificateTrustValidation.cs");

        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository", source, StringComparison.Ordinal);
        Assert.Contains("PkiUruguayTrustValidated: true", source, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated: false", source, StringComparison.Ordinal);
        Assert.Contains("RecoverConcurrentValidationAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Chain", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate2", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ACKCFE_adapter_reuses_governed_PKI_policy_but_maps_evidence_into_separate_boundary()
    {
        var adapter = Read("src/Infrastructure/Fiscal/DgiFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator.cs");
        var shared = Read("src/Infrastructure/Fiscal/DgiFiscalCfeEnvelopeAckCertificateTrustValidator.cs");

        Assert.Contains("IFiscalCfeEnvelopeAckCertificateTrustValidator", adapter, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator", adapter, StringComparison.Ordinal);
        Assert.Contains("fiscal.envelope.document_response.trust.", adapter, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated: false", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("DgiIdentityValidated: true", adapter, StringComparison.Ordinal);

        Assert.Contains("X509ChainTrustMode.CustomRootTrust", shared, StringComparison.Ordinal);
        Assert.Contains("X509RevocationMode.Online", shared, StringComparison.Ordinal);
        Assert.Contains("X509RevocationFlag.EntireChain", shared, StringComparison.Ordinal);
        Assert.Contains("X509VerificationFlags.NoFlag", shared, StringComparison.Ordinal);
        Assert.Contains("ExpectedSha256", shared, StringComparison.Ordinal);
    }

    [Fact]
    public void Trust_persistence_is_append_only_operation_guarded_and_restrict_linked_to_all_sources()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository.cs");
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeDocumentResponseCertificateTrustValidationRecord.cs");
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceCfeDocumentResponseCertificateTrustModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260914123000_V1FiscalCfeDocumentResponseCertificateTrust.cs");

        Assert.Contains("AsNoTracking()", repository, StringComparison.Ordinal);
        Assert.Contains(".Add(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Update(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_document_response_certificate_trust_validations", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fcdrctv_operation", model, StringComparison.Ordinal);
        Assert.Contains("PkiUruguayTrustValidated", model, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated", model, StringComparison.Ordinal);

        Assert.Contains("FK_v1_fcdrctv_signature_verification", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrctv_consultation", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrctv_ack_observation", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrctv_submission", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrctv_envelope", customizer, StringComparison.Ordinal);
        Assert.Contains("DeleteBehavior.Restrict", customizer, StringComparison.Ordinal);

        Assert.Contains("ReferentialAction.Restrict", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fcdrctv_operation", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_and_model_chain_register_the_new_ACKCFE_trust_slice()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");
        var database = Read("src/Infrastructure/Persistence/V1/V1PersistenceDatabaseConfigurator.cs");

        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator, DgiFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator", services, StringComparison.Ordinal);
        Assert.Contains("AddScoped<EfFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository>()", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationHistoryReader", services, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<EfFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository>()", services, StringComparison.Ordinal);
        Assert.Contains("ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase", services, StringComparison.Ordinal);
        Assert.Contains("V1PersistenceCfeDocumentResponseCertificateTrustModelCustomizer", database, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_keeps_PKI_trust_separate_from_DGI_identity_state_semantics_and_testing_readiness()
    {
        var docs = Read("documentation/blueprint-api-implementation/66_FISCAL_CFE_DOCUMENT_RESPONSE_CERTIFICATE_TRUST.md");

        Assert.Contains("PKI Uruguay", docs, StringComparison.Ordinal);
        Assert.Contains("CustomRootTrust", docs, StringComparison.Ordinal);
        Assert.Contains("online revocation", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DgiIdentityValidated = false", docs, StringComparison.Ordinal);
        Assert.Contains("does **not** prove", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ACKCFE_det/Estado", docs, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
