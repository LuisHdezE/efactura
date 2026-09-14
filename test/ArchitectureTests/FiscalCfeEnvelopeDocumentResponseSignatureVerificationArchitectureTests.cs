using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeDocumentResponseSignatureVerificationArchitectureTests
{
    [Fact]
    public void Application_owns_ACKCFE_signature_policy_without_XMLDSig_X509_HTTP_or_infrastructure_dependencies()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseSignatureVerification.cs");

        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseSignatureVerifier", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository", source, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", source, StringComparison.Ordinal);
        Assert.Contains("CertificateTrustValidated: false", source, StringComparison.Ordinal);
        Assert.Contains("invalid_external_signature", source, StringComparison.Ordinal);
        Assert.Contains("concurrent_verification_unresolved", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SignedXml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Chain", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_verifier_checks_whole_ACKCFE_signature_math_without_claiming_trust()
    {
        var verifier = Read("src/Infrastructure/Fiscal/DgiFiscalCfeEnvelopeDocumentResponseSignatureVerifier.cs");

        Assert.Contains("ACKCFE", verifier, StringComparison.Ordinal);
        Assert.Contains("DtdProcessing.Prohibit", verifier, StringComparison.Ordinal);
        Assert.Contains("XmlResolver = null", verifier, StringComparison.Ordinal);
        Assert.Contains("SignedXml", verifier, StringComparison.Ordinal);
        Assert.Contains("signedXml.CheckSignature(certificate, verifySignatureOnly: true)", verifier, StringComparison.Ordinal);
        Assert.Contains("reference.Uri, string.Empty", verifier, StringComparison.Ordinal);
        Assert.Contains("XmlDsigEnvelopedSignatureTransformUrl", verifier, StringComparison.Ordinal);
        Assert.Contains("X509CertificateLoader.LoadCertificate", verifier, StringComparison.Ordinal);
        Assert.Contains("XmlDsigRSASHA1Url", verifier, StringComparison.Ordinal);
        Assert.Contains("CertificateTrustValidated: false", verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Chain", verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("ChainPolicy", verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRSAPrivateKey", verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", verifier, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_is_append_only_unique_per_consultation_and_referentially_guarded()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository.cs");
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeDocumentResponseSignatureVerificationRecord.cs");
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceCfeDocumentResponseSignatureModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260914011000_V1FiscalCfeDocumentResponseSignatureVerification.cs");

        Assert.Contains("AsNoTracking()", repository, StringComparison.Ordinal);
        Assert.Contains(".Add(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Update(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", repository, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_document_response_signature_verifications", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fcdrsv_consultation", model, StringComparison.Ordinal);
        Assert.Contains("CertificateTrustValidated", model, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", model, StringComparison.Ordinal);

        Assert.Contains("FK_v1_fcdrsv_consultation", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrsv_ack_observation", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrsv_submission", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrsv_envelope", customizer, StringComparison.Ordinal);
        Assert.Contains("DeleteBehavior.Restrict", customizer, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fcdrsv_consultation", customizer, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_document_response_signature_verifications", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrsv_consultation", migration, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Restrict", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fcdrsv_consultation", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_registers_ACKCFE_verifier_repository_and_use_case()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseSignatureVerifier, DgiFiscalCfeEnvelopeDocumentResponseSignatureVerifier", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository, EfFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository", services, StringComparison.Ordinal);
        Assert.Contains("VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_keeps_ACKCFE_signature_math_separate_from_trust_identity_and_state_semantics()
    {
        var docs = Read("documentation/blueprint-api-implementation/65_FISCAL_CFE_DOCUMENT_RESPONSE_SIGNATURE_VERIFICATION.md");
        var checkpoint = Read("documentation/BLUEPRINT_CURRENT_STATE.md");

        Assert.Contains("Status: GOVERNED IMPLEMENTATION CANDIDATE", docs, StringComparison.Ordinal);
        Assert.Contains("CertificateTrustValidated = false", docs, StringComparison.Ordinal);
        Assert.Contains("whole durable `ACKCFE`", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does **not**", docs, StringComparison.Ordinal);
        Assert.Contains("ACKCFE_det/Estado", docs, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);

        Assert.Contains("main@8a70631cc5e2d723f88209459e5b77e487b66c20", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Accepted PR #95 ACKCFE document-response consultation boundary", checkpoint, StringComparison.Ordinal);
        Assert.Contains("cryptographic XMLDSig verification of returned `ACKCFE`", checkpoint, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", checkpoint, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
