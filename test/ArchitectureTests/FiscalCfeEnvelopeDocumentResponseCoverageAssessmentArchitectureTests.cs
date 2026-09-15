using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeDocumentResponseCoverageAssessmentArchitectureTests
{
    [Fact]
    public void Coverage_boundary_is_read_only_evidence_first_and_fail_closed()
    {
        var application = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseCoverageAssessment.cs");

        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationHistoryReader", application, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationHistoryReader", application, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository", application, StringComparison.Ordinal);
        Assert.Contains("EnsureStoredIntegrity", application, StringComparison.Ordinal);
        Assert.Contains("EnsureVerificationIntegrity", application, StringComparison.Ordinal);
        Assert.Contains("EnsureValidationIntegrity", application, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", application, StringComparison.Ordinal);
        Assert.Contains("document_state_contradiction", application, StringComparison.Ordinal);
        Assert.Contains("response_id_contradiction", application, StringComparison.Ordinal);
        Assert.Contains("count_mismatch", application, StringComparison.Ordinal);
        Assert.Contains("FullDocumentCoverage", application, StringComparison.Ordinal);
        Assert.Contains("PartialDocumentCoverage", application, StringComparison.Ordinal);
        Assert.Contains("NoDocumentCoverage", application, StringComparison.Ordinal);
        Assert.Contains("ProtocolFinalityProven: false", application, StringComparison.Ordinal);
        Assert.Contains("TokenExhaustionProven: false", application, StringComparison.Ordinal);
        Assert.Contains("AutomaticReconsultationAuthorized: false", application, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated: false", application, StringComparison.Ordinal);
        Assert.Contains("ordinal remains message-scoped", application, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IUnitOfWork", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", application, StringComparison.Ordinal);
        Assert.DoesNotContain(".AddAsync(", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Existing_persistence_adapters_expose_source_history_without_schema_redefinition()
    {
        var consultations = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeDocumentResponseConsultationRepository.cs");
        var trusts = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository.cs");
        var consultationModel = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeDocumentResponseConsultationRecord.cs");
        var trustModel = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeDocumentResponseCertificateTrustValidationRecord.cs");

        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationHistoryReader", consultations, StringComparison.Ordinal);
        Assert.Contains("ListByAckObservationIdAsync", consultations, StringComparison.Ordinal);
        Assert.Contains("x.AckObservationId == ackObservationId", consultations, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationHistoryReader", trusts, StringComparison.Ordinal);
        Assert.Contains("ListByConsultationIdAsync", trusts, StringComparison.Ordinal);
        Assert.Contains("x.ConsultationId == consultationId", trusts, StringComparison.Ordinal);
        Assert.Contains("IX_v1_fcdrc_ack_observation", consultationModel, StringComparison.Ordinal);
        Assert.Contains("IX_v1_fcdrctv_consultation", trustModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Coverage_documentation_must_not_equate_full_CFE_coverage_with_DGI_finality()
    {
        var documentation = Read("documentation/blueprint-api-implementation/68_FISCAL_CFE_DOCUMENT_RESPONSE_KNOWN_COVERAGE.md");

        Assert.Contains("multiple messages", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("en este mensaje", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FullDocumentCoverage", documentation, StringComparison.Ordinal);
        Assert.Contains("does not prove protocol finality", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("token exhaustion", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no automatic polling", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DgiIdentityValidated = false", documentation, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", documentation, StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root containing api-accounting.sln was not found.");
    }
}
