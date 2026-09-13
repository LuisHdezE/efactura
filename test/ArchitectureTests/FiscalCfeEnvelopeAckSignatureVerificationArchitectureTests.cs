using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeAckSignatureVerificationArchitectureTests
{
    [Fact]
    public void Application_owns_append_only_verification_policy_without_XMLDSig_X509_HTTP_or_infrastructure_dependencies()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopeAckSignatureVerification.cs");

        Assert.Contains("IFiscalCfeEnvelopeAckSignatureVerifier", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeAckSignatureVerificationRepository", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeAckObservationRepository", source, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", source, StringComparison.Ordinal);
        Assert.Contains("CertificateTrustValidated: false", source, StringComparison.Ordinal);
        Assert.Contains("invalid_external_signature", source, StringComparison.Ordinal);
        Assert.Contains("RecoverConcurrentVerificationAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SignedXml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate2", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Chain", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRSAPrivateKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_verifier_checks_whole_document_signature_math_without_claiming_certificate_trust()
    {
        var verifier = Read("src/Infrastructure/Fiscal/DgiFiscalCfeEnvelopeAckSignatureVerifier.cs");

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
    public void Verification_persistence_is_append_only_unique_and_referentially_guarded()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeAckSignatureVerificationRepository.cs");
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeEnvelopeRecords.cs");
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceEnvelopeModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260913121000_V1FiscalCfeEnvelopeAckSignatureVerification.cs");

        Assert.Contains("AsNoTracking()", repository, StringComparison.Ordinal);
        Assert.Contains(".Add(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Update(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", repository, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelope_ack_signature_verifications", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceasv_ack_observation", model, StringComparison.Ordinal);
        Assert.Contains("CertificateTrustValidated", model, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", model, StringComparison.Ordinal);

        Assert.Contains("FK_v1_fceasv_ack_observation", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceasv_submission", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceasv_envelope", customizer, StringComparison.Ordinal);
        Assert.Contains("DeleteBehavior.Restrict", customizer, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceasv_ack_observation", customizer, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelope_ack_signature_verifications", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceasv_ack_observation", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceasv_submission", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceasv_envelope", migration, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Restrict", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceasv_ack_observation", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_registers_verifier_repository_and_use_case()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalCfeEnvelopeAckSignatureVerifier, DgiFiscalCfeEnvelopeAckSignatureVerifier", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeAckSignatureVerificationRepository, EfFiscalCfeEnvelopeAckSignatureVerificationRepository", services, StringComparison.Ordinal);
        Assert.Contains("VerifyFiscalCfeEnvelopeAckSignatureUseCase", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_keeps_signature_math_separate_from_certificate_trust_and_token_consultation()
    {
        var docs = Read("documentation/blueprint-api-implementation/60_FISCAL_SOBRE_ACK_SIGNATURE_VERIFICATION.md");
        var checkpoint = Read("documentation/BLUEPRINT_CURRENT_STATE.md");

        Assert.Contains("Status: GOVERNED IMPLEMENTATION CANDIDATE", docs, StringComparison.Ordinal);
        Assert.Contains("Candidate PR: #87", docs, StringComparison.Ordinal);
        Assert.Contains("CertificateTrustValidated = false", docs, StringComparison.Ordinal);
        Assert.Contains("does **not** claim", docs, StringComparison.Ordinal);
        Assert.Contains("token-input endpoint", docs, StringComparison.Ordinal);
        Assert.Contains("does not weaken the consumer's own fiscal signer", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);

        Assert.Contains("main@c5d0c1220ca1c707417092aa905ee8f279056cc6", checkpoint, StringComparison.Ordinal);
        Assert.Contains("merge of PR #91", checkpoint, StringComparison.Ordinal);
        Assert.Contains("There is no pending governed increment currently open", checkpoint, StringComparison.Ordinal);
        Assert.Contains("CertificateTrustValidated = false", checkpoint, StringComparison.Ordinal);
        Assert.Contains("PkiUruguayTrustValidated = true", checkpoint, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated = false", checkpoint, StringComparison.Ordinal);
        Assert.Contains("does not establish a web-service operation whose input is the ACKSobre `Token`", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Accepted PR #87 ACKSobre signature-verification boundary", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Accepted PR #91 ACKSobre PKI Uruguay certificate-trust boundary", checkpoint, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}