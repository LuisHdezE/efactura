using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeDocumentResponseEvidenceCycleArchitectureTests
{
    [Fact]
    public void Evidence_cycle_composes_exact_consult_signature_trust_and_coverage_boundaries()
    {
        var application = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseEvidenceCycle.cs");

        Assert.Contains("ConsultFiscalCfeEnvelopeDocumentResponseUseCase", application, StringComparison.Ordinal);
        Assert.Contains("VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase", application, StringComparison.Ordinal);
        Assert.Contains("ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase", application, StringComparison.Ordinal);
        Assert.Contains("AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase", application, StringComparison.Ordinal);
        Assert.Contains("ValidateTrustAsync(", application, StringComparison.Ordinal);
        Assert.Contains("normalized.OperationId", application, StringComparison.Ordinal);
        Assert.Contains("EnsureExactLineage", application, StringComparison.Ordinal);
        Assert.Contains("SignatureValid", application, StringComparison.Ordinal);
        Assert.Contains("PkiUruguayTrustValidated", application, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated: false", application, StringComparison.Ordinal);
        Assert.Contains("ProtocolFinalityProven: false", application, StringComparison.Ordinal);
        Assert.Contains("TokenExhaustionProven: false", application, StringComparison.Ordinal);
        Assert.Contains("AutomaticReconsultationAuthorized: false", application, StringComparison.Ordinal);

        var consultIndex = application.IndexOf("new ConsultFiscalCfeEnvelopeDocumentResponseCommand", StringComparison.Ordinal);
        var signatureIndex = application.IndexOf("new VerifyFiscalCfeEnvelopeDocumentResponseSignatureCommand", StringComparison.Ordinal);
        var trustIndex = application.IndexOf("new ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustCommand", StringComparison.Ordinal);
        var coverageIndex = application.IndexOf("new AssessFiscalCfeEnvelopeDocumentResponseCoverageCommand", StringComparison.Ordinal);
        Assert.True(consultIndex >= 0 && consultIndex < signatureIndex);
        Assert.True(signatureIndex < trustIndex);
        Assert.True(trustIndex < coverageIndex);
    }

    [Fact]
    public void Evidence_cycle_contains_no_polling_scheduler_or_cross_boundary_transaction_claim()
    {
        var application = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseEvidenceCycle.cs");

        Assert.DoesNotContain("while (", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Delay", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Timer", application, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", application, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_DI_exposes_PR106_history_readers_coverage_and_explicit_cycle()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("AddScoped<EfFiscalCfeEnvelopeDocumentResponseConsultationRepository>()", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationRepository", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationHistoryReader", services, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<EfFiscalCfeEnvelopeDocumentResponseConsultationRepository>()", services, StringComparison.Ordinal);
        Assert.Contains("AddScoped<EfFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository>()", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationHistoryReader", services, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<EfFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository>()", services, StringComparison.Ordinal);
        Assert.Contains("AddScoped<AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase>()", services, StringComparison.Ordinal);
        Assert.Contains("AddScoped<CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase>()", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Evidence_cycle_documentation_keeps_manual_reconsultation_and_finality_boundaries_explicit()
    {
        var documentation = Read("documentation/blueprint-api-implementation/69_FISCAL_CFE_DOCUMENT_RESPONSE_EXPLICIT_EVIDENCE_CYCLE.md");

        Assert.Contains("caller-triggered", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("multiple messages", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("new operation `Y`", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("contains no loop", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prove that DGI has emitted its last ACKCFE message", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DgiIdentityValidated = false", documentation, StringComparison.Ordinal);
        Assert.Contains("ProtocolFinalityProven = false", documentation, StringComparison.Ordinal);
        Assert.Contains("TokenExhaustionProven = false", documentation, StringComparison.Ordinal);
        Assert.Contains("AutomaticReconsultationAuthorized = false", documentation, StringComparison.Ordinal);
        Assert.Contains("add a public REST endpoint", documentation, StringComparison.OrdinalIgnoreCase);
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
