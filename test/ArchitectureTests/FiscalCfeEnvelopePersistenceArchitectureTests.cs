using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopePersistenceArchitectureTests
{
    [Fact]
    public void Application_persists_explicit_Idemisor_without_transport_or_allocator_dependency()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopePersistence.cs");

        Assert.Contains("PackageFiscalCfeEnvelopeUseCase", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeRepository", source, StringComparison.Ordinal);
        Assert.Contains("ITransactionManager", source, StringComparison.Ordinal);
        Assert.Contains("IUnitOfWork", source, StringComparison.Ordinal);
        Assert.Contains("SenderEnvelopeId remains explicit caller input", source, StringComparison.Ordinal);
        Assert.Contains("identity_payload_conflict", source, StringComparison.Ordinal);
        Assert.Contains("persisted_evidence_invalid", source, StringComparison.Ordinal);
        Assert.Contains("CreatedAt.Offset != command.CreatedAt.Offset", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeEnvelopeTransport", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeEnvelopeIdAllocator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DgiWs", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Repository_is_read_add_only_and_preserves_offset_evidence()
    {
        var source = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeRepository.cs");

        Assert.Contains("AsNoTracking()", source, StringComparison.Ordinal);
        Assert.Contains("CreatedAtUtc = envelope.CreatedAt.ToUniversalTime()", source, StringComparison.Ordinal);
        Assert.Contains("CreatedAtOffsetMinutes", source, StringComparison.Ordinal);
        Assert.Contains("ToOffset(TimeSpan.FromMinutes(record.CreatedAtOffsetMinutes))", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSql", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Ef_model_chains_prior_baseline_and_enforces_operation_and_identity_uniqueness()
    {
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceEnvelopeModelCustomizer.cs");
        var configurator = Read("src/Infrastructure/Persistence/V1/V1PersistenceDatabaseConfigurator.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260913050000_V1FiscalCfeEnvelopePersistence.cs");

        Assert.Contains("V1PersistenceLaterStateModelCustomizer", customizer, StringComparison.Ordinal);
        Assert.Contains("_baseline.Customize(modelBuilder, context)", customizer, StringComparison.Ordinal);
        Assert.Contains("v1_fiscal_cfe_envelopes", customizer, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fce_operation", customizer, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fce_identity", customizer, StringComparison.Ordinal);
        Assert.Contains("CreatedAtOffsetMinutes", customizer, StringComparison.Ordinal);
        Assert.Contains("V1PersistenceEnvelopeModelCustomizer", configurator, StringComparison.Ordinal);

        Assert.Contains("v1_fiscal_cfe_envelopes", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fce_operation", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fce_identity", migration, StringComparison.Ordinal);
        Assert.Contains("CreatedAtUtc", migration, StringComparison.Ordinal);
        Assert.Contains("CreatedAtOffsetMinutes", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_registers_repository_and_persistence_use_case()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalCfeEnvelopeRepository, EfFiscalCfeEnvelopeRepository", services, StringComparison.Ordinal);
        Assert.Contains("PersistFiscalCfeEnvelopeUseCase", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_keeps_Idemisor_allocation_and_transport_outside_candidate()
    {
        var docs = Read("documentation/blueprint-api-implementation/57_FISCAL_SOBRE_DURABLE_IDENTITY.md");
        var checkpoint = Read("documentation/BLUEPRINT_CURRENT_STATE.md");

        Assert.Contains("assigned by the issuer", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no authoritative allocation algorithm", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("local replay/correlation invariant", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not allocate", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not submit", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("S08", docs, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);

        Assert.Contains("main@b2ea590779d229d8a2eab51111562a0447c6b52e", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Current pending governed increment: PR #84", checkpoint, StringComparison.Ordinal);
        Assert.Contains("PR #84 remains pending until exact-head CI is green and human review is complete", checkpoint, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
