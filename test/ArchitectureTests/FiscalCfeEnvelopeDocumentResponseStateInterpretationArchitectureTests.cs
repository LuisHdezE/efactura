using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeDocumentResponseStateInterpretationArchitectureTests
{
    [Fact]
    public void Application_semantics_requires_verified_trusted_evidence_and_exact_published_taxonomy()
    {
        var application = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseStateInterpretation.cs");

        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationRepository", application, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository", application, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository", application, StringComparison.Ordinal);
        Assert.Contains("EnsureVerificationIntegrity", application, StringComparison.Ordinal);
        Assert.Contains("EnsureValidationIntegrity", application, StringComparison.Ordinal);
        Assert.Contains("\"AE\" => FiscalCfeEnvelopeDocumentResponseSemanticState.Received", application, StringComparison.Ordinal);
        Assert.Contains("\"BE\" => FiscalCfeEnvelopeDocumentResponseSemanticState.Rejected", application, StringComparison.Ordinal);
        Assert.Contains("\"CE\" => FiscalCfeEnvelopeDocumentResponseSemanticState.ObservedContingency", application, StringComparison.Ordinal);
        Assert.Contains("count_mismatch", application, StringComparison.Ordinal);
        Assert.Contains("state_code_unsupported", application, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated", application, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", application, StringComparison.Ordinal);
        Assert.DoesNotContain(".AddAsync(", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantics_boundary_explicitly_forbids_finality_and_local_lifecycle_mutation()
    {
        var application = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseStateInterpretation.cs");
        var documentation = Read("documentation/blueprint-api-implementation/67_FISCAL_CFE_DOCUMENT_RESPONSE_STATE_SEMANTICS.md");

        Assert.Contains("one or multiple response messages", application, StringComparison.Ordinal);
        Assert.Contains("never claims complete Sobre resolution", application, StringComparison.Ordinal);
        Assert.Contains("never changes FiscalDocument state", application, StringComparison.Ordinal);
        Assert.Contains("AE", documentation, StringComparison.Ordinal);
        Assert.Contains("BE", documentation, StringComparison.Ordinal);
        Assert.Contains("CE", documentation, StringComparison.Ordinal);
        Assert.Contains("one or multiple response messages", documentation, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated = false", documentation, StringComparison.Ordinal);
        Assert.Contains("does **not** mutate", documentation, StringComparison.Ordinal);
        Assert.Contains("**BLOCKED BY MISSING PRODUCT CAPABILITIES**", documentation, StringComparison.Ordinal);
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
