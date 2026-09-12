using Xunit;

namespace ArchitectureTests;

public sealed class DgiTestingReadinessDocumentationTests
{
    [Fact]
    public void Current_checkpoint_and_testing_reconciliation_keep_the_formal_DGI_gate_fail_closed()
    {
        var root = FindRepositoryRoot();
        var currentState = File.ReadAllText(
            Path.Combine(root, "documentation", "BLUEPRINT_CURRENT_STATE.md"));
        var reconciliation = File.ReadAllText(
            Path.Combine(
                root,
                "documentation",
                "blueprint-api-implementation",
                "36_DGI_TESTING_READINESS_RECONCILIATION.md"));

        Assert.Contains(
            "main@44478944bff3dbfee1e269347b48954015d8b3b9",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "merge of PR #77",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "Current pending governed increment: PR #78",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "PR #78 is not part of the accepted baseline until its exact final head is green and the human explicitly approves merge.",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "Durable DGI `EFACRECEPCIONREPORTE` transport with immediate `ACKRepDiario` AR/BR persistence.",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "The accepted PR #77 baseline intentionally stops at `BR`; it does not contain accepted same-`SecEnvio` corrected resubmission.",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "**BLOCKED BY MISSING PRODUCT CAPABILITIES**",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "PR #78 remains pending until exact-head CI is green and human review is complete",
            currentState,
            StringComparison.Ordinal);

        foreach (var required in new[]
        {
            "101 e-Ticket",
            "102 Nota de Crédito de e-Ticket",
            "103 Nota de Débito de e-Ticket",
            "111 e-Factura",
            "112 Nota de Crédito de e-Factura",
            "113 Nota de Débito de e-Factura",
            "50 distinct documents",
            "Reporte Procesado",
            "Formato_Sobre_v05",
            "Formato Reporte CFE v13 2",
            "Formato Mensajes Respuesta v19",
            "XSDs_FE_V1.44.2"
        })
        {
            Assert.Contains(required, reconciliation, StringComparison.Ordinal);
        }

        Assert.Contains(
            "Domestic credit/debit note foundation",
            reconciliation,
            StringComparison.Ordinal);
        Assert.Contains(
            "Daily Report foundation",
            reconciliation,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "formal traditional `Prueba de Testing`: READY",
            currentState,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root containing api-accounting.sln was not found.");
    }
}
