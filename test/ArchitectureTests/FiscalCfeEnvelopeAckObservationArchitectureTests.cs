using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeAckObservationArchitectureTests
{
    [Fact]
    public void Application_owns_typed_append_only_ACK_policy_without_XML_HTTP_or_infrastructure_dependencies()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopeAckObservation.cs");

        Assert.Contains("IFiscalCfeEnvelopeAckParser", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeAckObservationRepository", source, StringComparison.Ordinal);
        Assert.Contains("FiscalCfeEnvelopeSubmissionState.ResponseReceived", source, StringComparison.Ordinal);
        Assert.Contains("S01", source, StringComparison.Ordinal);
        Assert.Contains("S08", source, StringComparison.Ordinal);
        Assert.Contains("correlation_mismatch", source, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlDocument", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SignedXml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRSAPrivateKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_parser_is_safe_structural_and_does_not_claim_signature_crypto_validation()
    {
        var parser = Read("src/Infrastructure/Fiscal/DgiFiscalCfeEnvelopeAckParser.cs");

        Assert.Contains("DtdProcessing.Prohibit", parser, StringComparison.Ordinal);
        Assert.Contains("XmlResolver = null", parser, StringComparison.Ordinal);
        Assert.Contains("ACKSobre", parser, StringComparison.Ordinal);
        Assert.Contains("\"AS\"", parser, StringComparison.Ordinal);
        Assert.Contains("\"BS\"", parser, StringComparison.Ordinal);
        Assert.Contains("Signature", parser, StringComparison.Ordinal);
        Assert.Contains("TryOptionalText", parser, StringComparison.Ordinal);
        Assert.DoesNotContain("SignedXml", parser, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", parser, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRSAPrivateKey", parser, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_persistence_is_append_only_referentially_guarded_and_provider_neutral()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeAckObservationRepository.cs");
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeEnvelopeRecords.cs");
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceEnvelopeModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260913065000_V1FiscalCfeEnvelopeAckObservation.cs");

        Assert.Contains("AsNoTracking()", repository, StringComparison.Ordinal);
        Assert.Contains(".Add(", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateAsync", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", repository, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelope_ack_observations", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceao_submission", model, StringComparison.Ordinal);
        Assert.Contains("RejectionReasonsJson", model, StringComparison.Ordinal);
        Assert.Contains("ResponseSha256", model, StringComparison.Ordinal);

        Assert.Contains("FK_v1_fceao_submission", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceao_envelope", customizer, StringComparison.Ordinal);
        Assert.Contains("DeleteBehavior.Restrict", customizer, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceao_submission", customizer, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelope_ack_observations", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceao_submission", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fceao_envelope", migration, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Restrict", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fceao_submission", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_registers_ACK_parser_repository_and_use_case()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalCfeEnvelopeAckParser, DgiFiscalCfeEnvelopeAckParser", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeAckObservationRepository, EfFiscalCfeEnvelopeAckObservationRepository", services, StringComparison.Ordinal);
        Assert.Contains("ObserveFiscalCfeEnvelopeAckUseCase", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_keeps_ACK_observation_non_mutating_and_S08_fail_closed()
    {
        var docs = Read("documentation/blueprint-api-implementation/59_FISCAL_SOBRE_ACK_OBSERVATION.md");

        Assert.Contains("Status: GOVERNED IMPLEMENTATION CANDIDATE", docs, StringComparison.Ordinal);
        Assert.Contains("AS", docs, StringComparison.Ordinal);
        Assert.Contains("BS", docs, StringComparison.Ordinal);
        Assert.Contains("S01..S08", docs, StringComparison.Ordinal);
        Assert.Contains("S08", docs, StringComparison.Ordinal);
        Assert.Contains("no automatic recovery", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not mutate", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cryptographic", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
