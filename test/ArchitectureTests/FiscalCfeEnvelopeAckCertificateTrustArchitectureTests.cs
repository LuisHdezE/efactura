using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeAckCertificateTrustArchitectureTests
{
    [Fact]
    public void Application_owns_append_only_trust_policy_without_X509Chain_or_infrastructure_dependencies()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopeAckCertificateTrustValidation.cs");

        Assert.Contains("IFiscalCfeEnvelopeAckCertificateTrustValidator", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeAckCertificateTrustValidationRepository", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeAckSignatureVerificationRepository", source, StringComparison.Ordinal);
        Assert.Contains("PkiUruguayTrustValidated: true", source, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated: false", source, StringComparison.Ordinal);
        Assert.Contains("RevocationMode", source, StringComparison.Ordinal);
        Assert.Contains("RecoverConcurrentValidationAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Chain", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate2", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_uses_pinned_custom_root_and_online_entire_chain_revocation_without_claiming_DGI_identity()
    {
        var validator = Read("src/Infrastructure/Fiscal/DgiFiscalCfeEnvelopeAckCertificateTrustValidator.cs");

        Assert.Contains("X509ChainTrustMode.CustomRootTrust", validator, StringComparison.Ordinal);
        Assert.Contains("CustomTrustStore.AddRange", validator, StringComparison.Ordinal);
        Assert.Contains("X509RevocationMode.Online", validator, StringComparison.Ordinal);
        Assert.Contains("X509RevocationFlag.EntireChain", validator, StringComparison.Ordinal);
        Assert.Contains("X509VerificationFlags.NoFlag", validator, StringComparison.Ordinal);
        Assert.Contains("ExpectedSha256", validator, StringComparison.Ordinal);
        Assert.Contains("certificate_source_mismatch", validator, StringComparison.Ordinal);
        Assert.Contains("X509KeyUsageFlags.DigitalSignature", validator, StringComparison.Ordinal);
        Assert.Contains("DtdProcessing.Prohibit", validator, StringComparison.Ordinal);
        Assert.Contains("XmlResolver = null", validator, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated: false", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("DgiIdentityValidated: true", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowUnknownCertificateAuthority", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("NoCheck", validator, StringComparison.Ordinal);
    }

    [Fact]
    public void Trust_persistence_is_append_only_operation_replay_guarded_and_restrict_linked_to_source_evidence()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeAckCertificateTrustValidationRepository.cs");
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeEnvelopeRecords.cs");
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceEnvelopeModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260913173000_V1FiscalCfeEnvelopeAckCertificateTrust.cs");

        Assert.Contains("AsNoTracking()", repository, StringComparison.Ordinal);
        Assert.Contains(".Add(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Update(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelope_ack_certificate_trust_validations", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceactv_operation", model, StringComparison.Ordinal);
        Assert.Contains("PkiUruguayTrustValidated", model, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated", model, StringComparison.Ordinal);

        Assert.Contains("FK_v1_fceactv_signature_verification", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceactv_ack_observation", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceactv_submission", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceactv_envelope", customizer, StringComparison.Ordinal);
        Assert.Contains("DeleteBehavior.Restrict", customizer, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceactv_operation", customizer, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelope_ack_certificate_trust_validations", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceactv_signature_verification", migration, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Restrict", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceactv_operation", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_registers_validator_repository_and_use_case()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalCfeEnvelopeAckCertificateTrustValidator, DgiFiscalCfeEnvelopeAckCertificateTrustValidator", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeAckCertificateTrustValidationRepository, EfFiscalCfeEnvelopeAckCertificateTrustValidationRepository", services, StringComparison.Ordinal);
        Assert.Contains("ValidateFiscalCfeEnvelopeAckCertificateTrustUseCase", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_bounds_PKI_Uruguay_trust_without_overclaiming_DGI_identity_or_readiness()
    {
        var docs = Read("documentation/blueprint-api-implementation/62_FISCAL_SOBRE_ACK_CERTIFICATE_TRUST.md");
        var checkpoint = Read("documentation/BLUEPRINT_CURRENT_STATE.md");

        Assert.Contains("Status: GOVERNED IMPLEMENTATION CANDIDATE", docs, StringComparison.Ordinal);
        Assert.Contains("PKI Uruguay", docs, StringComparison.Ordinal);
        Assert.Contains("CustomRootTrust", docs, StringComparison.Ordinal);
        Assert.Contains("online revocation", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DgiIdentityValidated = false", docs, StringComparison.Ordinal);
        Assert.Contains("does not prove DGI legal identity", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
        Assert.Contains("main@bf7d97e350192302cd7cc681d6c7feecfa8589ab", docs, StringComparison.Ordinal);

        Assert.Contains("main@8a70631cc5e2d723f88209459e5b77e487b66c20", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Accepted PR #91 ACKSobre PKI Uruguay certificate-trust boundary", checkpoint, StringComparison.Ordinal);
        Assert.Contains("X509Chain CustomRootTrust", checkpoint, StringComparison.Ordinal);
        Assert.Contains("online revocation for EntireChain", checkpoint, StringComparison.Ordinal);
        Assert.Contains("PkiUruguayTrustValidated = true", checkpoint, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated = false", checkpoint, StringComparison.Ordinal);
        Assert.Contains("DGI-specific legal signer identity/certificate habilitation policy", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Accepted PR #93 deterministic Sobre batch-planning boundary", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Accepted PR #95 ACKCFE document-response consultation boundary", checkpoint, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", checkpoint, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}