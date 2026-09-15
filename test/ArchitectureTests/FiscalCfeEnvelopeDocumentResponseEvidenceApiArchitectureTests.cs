using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeDocumentResponseEvidenceApiArchitectureTests
{
    [Fact]
    public void API_FIS_010_is_explicit_idempotent_and_requires_regularization_permission()
    {
        var inventory = Read("documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md");
        var controller = Read("src/WebApi/Controllers/V1/FiscalCfeEnvelopesController.cs");

        Assert.Contains("API-FIS-010", inventory, StringComparison.Ordinal);
        Assert.Contains("collectFiscalEnvelopeDocumentResponseEvidence", inventory, StringComparison.Ordinal);
        Assert.Contains("POST `/api/v1/fiscal-envelopes/{envelopeId}/document-response-evidence`", inventory, StringComparison.Ordinal);
        Assert.Contains("`fiscal.regularization.manage` | REQUIRED", inventory, StringComparison.Ordinal);

        Assert.Contains("[HttpPost(\"{envelopeId:guid}/document-response-evidence\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[RequirePermission(Permissions.FiscalRegularizationManage)]", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.RequireIdempotencyKey(Request)", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.ComputeRequestHash", controller, StringComparison.Ordinal);
        Assert.Contains("CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdUseCase", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeEnvelopeDocumentResponseConsultationGateway", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("ConsultFiscalCfeEnvelopeDocumentResponseUseCase", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_contract_exposes_bounded_trusted_evidence_without_tokens_xml_or_certificate_material()
    {
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/FiscalCfeEnvelopeContracts.cs");

        Assert.Contains("CoverageStatus", contracts, StringComparison.Ordinal);
        Assert.Contains("CoveredDocuments", contracts, StringComparison.Ordinal);
        Assert.Contains("MissingDocuments", contracts, StringComparison.Ordinal);
        Assert.Contains("XmlSignatureValidated", contracts, StringComparison.Ordinal);
        Assert.Contains("PkiUruguayTrustValidated", contracts, StringComparison.Ordinal);
        Assert.Contains("FullyReplayed", contracts, StringComparison.Ordinal);
        Assert.Contains("DgiIdentityValidated", contracts, StringComparison.Ordinal);
        Assert.Contains("ProtocolFinalityProven", contracts, StringComparison.Ordinal);
        Assert.Contains("TokenExhaustionProven", contracts, StringComparison.Ordinal);
        Assert.Contains("AutomaticReconsultationAuthorized", contracts, StringComparison.Ordinal);

        Assert.DoesNotContain("ResponseXml", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("ConsultationToken", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("TokenSha", contracts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CertificateSubject", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("CertificateIssuer", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("CertificateThumbprint", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("CertificateSerial", contracts, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_api_wrapper_resolves_server_owned_submission_identity_and_delegates_only_to_full_cycle()
    {
        var application = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseEvidenceApi.cs");

        Assert.Contains("IFiscalCfeEnvelopeSubmissionRepository", application, StringComparison.Ordinal);
        Assert.Contains("GetByEnvelopeIdAsync(command.EnvelopeId", application, StringComparison.Ordinal);
        Assert.Contains("submission.IssuerRuc", application, StringComparison.Ordinal);
        Assert.Contains("submission.ReceiverRut", application, StringComparison.Ordinal);
        Assert.Contains("submission.SenderEnvelopeId", application, StringComparison.Ordinal);
        Assert.Contains("CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase", application, StringComparison.Ordinal);
        Assert.Contains("PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", application, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeEnvelopeDocumentResponseConsultationGateway", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Evidence_api_documentation_preserves_non_finality_no_polling_and_no_raw_evidence_leakage()
    {
        var docs = Read("documentation/blueprint-api-implementation/70_FISCAL_CFE_DOCUMENT_RESPONSE_EVIDENCE_API.md");

        Assert.Contains("Status: GOVERNED IMPLEMENTATION CANDIDATE", docs, StringComparison.Ordinal);
        Assert.Contains("fiscal.regularization.manage", docs, StringComparison.Ordinal);
        Assert.Contains("raw `ACKCFE` XML", docs, StringComparison.Ordinal);
        Assert.Contains("consultation `Token`", docs, StringComparison.Ordinal);
        Assert.Contains("FULL_DOCUMENT_COVERAGE", docs, StringComparison.Ordinal);
        Assert.Contains("does not prove that DGI emitted its last response message", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DgiIdentityValidated = false", docs, StringComparison.Ordinal);
        Assert.Contains("ProtocolFinalityProven = false", docs, StringComparison.Ordinal);
        Assert.Contains("TokenExhaustionProven = false", docs, StringComparison.Ordinal);
        Assert.Contains("AutomaticReconsultationAuthorized = false", docs, StringComparison.Ordinal);
        Assert.Contains("no timer, polling loop, background worker", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
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
