using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeBatchPlanningArchitectureTests
{
    [Fact]
    public void Application_planner_is_local_product_policy_and_has_no_transport_or_persistence_side_effects()
    {
        var source = Read("src/Application/Fiscal/FiscalCfeEnvelopeBatchPlanning.cs");

        Assert.Contains("MaxCfePerEnvelope = 250", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalSignedArtifactRepository", source, StringComparison.Ordinal);
        Assert.Contains("first", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not discover pending documents", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("allocate Idemisor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeEnvelopeBuilder", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeEnvelopeTransportGateway", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SenderEnvelopeId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_injection_registers_batch_planner_use_case()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("PlanFiscalCfeEnvelopeBatchesUseCase", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_marks_grouping_as_product_policy_and_keeps_DGI_readiness_blocked()
    {
        var docs = Read("documentation/blueprint-api-implementation/63_FISCAL_SOBRE_BATCH_PLANNING.md");
        var checkpoint = Read("documentation/BLUEPRINT_CURRENT_STATE.md");

        Assert.Contains("product policy", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1..250", docs, StringComparison.Ordinal);
        Assert.Contains("same certificate", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not allocate `Idemisor`", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not discover pending", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
        Assert.Contains("business grouping/batching policy for CFE into Sobre", checkpoint, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
