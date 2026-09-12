using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportSequenceLifecycleArchitectureTests
{
    [Fact]
    public void Pinned_DGI_schema_states_first_send_is_one_and_correction_is_previous_plus_one()
    {
        var schema = Read("src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2/ReporteDiarioCFE.xsd");
        Assert.Contains("El primer envío del día trae el", schema, StringComparison.Ordinal);
        Assert.Contains("secuencia anterior +1", schema, StringComparison.Ordinal);
        Assert.Contains("<xs:totalDigits value=\"2\"/>", schema, StringComparison.Ordinal);
    }

    [Fact]
    public void Lifecycle_is_complete_replacement_idempotent_and_transport_free()
    {
        var source = Read("src/Application/Fiscal/FiscalDailyReportVersionLifecycle.cs");
        var docs = Read("documentation/blueprint-api-implementation/49_FISCAL_DAILY_REPORT_SEQUENCE_LIFECYCLE.md");
        Assert.Contains("CompleteDocuments", source, StringComparison.Ordinal);
        Assert.Contains("CompleteAnnulments", source, StringComparison.Ordinal);
        Assert.Contains("OperationId", source, StringComparison.Ordinal);
        Assert.Contains("previous SecEnvio + 1", docs, StringComparison.Ordinal);
        Assert.Contains("EFACRECEPCIONREPORTE", docs, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_has_linear_identity_and_operation_uniqueness()
    {
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260912043000_V1FiscalDailyReportSequenceLifecycle.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportRepositories.cs");
        Assert.Contains("UX_v1_fdr_version_identity", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_version_operation", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fdr_version_previous", migration, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", repository, StringComparison.Ordinal);
        Assert.Contains("CurrentTransaction", repository, StringComparison.Ordinal);
    }

    private static string Read(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return File.ReadAllText(Path.GetFullPath(full));
    }
}
