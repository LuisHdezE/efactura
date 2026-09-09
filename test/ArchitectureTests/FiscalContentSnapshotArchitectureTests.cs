using Xunit;

namespace ArchitectureTests;

public sealed class FiscalContentSnapshotArchitectureTests
{
    [Fact]
    public void Fiscal_snapshot_domain_is_framework_xml_signing_and_transport_free()
    {
        var source = Read("src/Domain/Fiscal/FiscalContentSnapshot.cs");

        Assert.Contains("FiscalConfirmationEvidence", source, StringComparison.Ordinal);
        Assert.Contains("FiscalContentSnapshot", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.AspNetCore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XDocument", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlDocument", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalSigner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalTransportGateway", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_snapshot_is_a_post_identity_application_step_not_an_identity_side_effect()
    {
        var workflow = Read("src/Application/Fiscal/FiscalContentSnapshotWorkflow.cs");
        var identity = Read("src/Application/Fiscal/FiscalDocumentIdentity.cs");

        Assert.Contains("CreateFiscalContentSnapshotUseCase", workflow, StringComparison.Ordinal);
        Assert.Contains("FiscalDocumentStatus.IdentityCreated", workflow, StringComparison.Ordinal);
        Assert.Contains("IFiscalContentSnapshotRepository", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateFiscalContentSnapshotUseCase", identity, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalContentSnapshotFactory", identity, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalXmlBuilder", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalSigner", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalTransportGateway", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirmation_freezes_fiscal_calculation_before_inventory_can_drift()
    {
        var confirmation = Read("src/Application/Sales/SaleConfirmationTransaction.cs");
        var request = Read("src/Domain/Fiscal/FiscalizationRequest.cs");

        Assert.Contains("FiscalConfirmationEvidence.Capture", confirmation, StringComparison.Ordinal);
        Assert.Contains("confirmation.FiscalCalculation", confirmation, StringComparison.Ordinal);
        Assert.Contains("confirmation.Selection.RuleEvidence", confirmation, StringComparison.Ordinal);
        Assert.Contains("FiscalConfirmationEvidence? ConfirmationEvidence", request, StringComparison.Ordinal);
        Assert.DoesNotContain("PrepareAsync(sale", Read("src/Application/Fiscal/FiscalContentSnapshotWorkflow.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_persistence_is_append_only_one_per_fiscal_document_and_provider_neutral()
    {
        var model = Read("src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260908233000_V1FiscalContentSnapshot.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalContentSnapshotRepository.cs");

        Assert.Contains("v1_fiscal_content_snapshots", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fcs_document", model, StringComparison.Ordinal);
        Assert.Contains("v1_fiscal_content_snapshots", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fcs_document", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("jsonb", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("json_extract", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "test")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
