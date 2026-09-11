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
            "main@44225493c60ee313e849f0f0d7628f7415744e49",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "formal traditional `Prueba de Testing`: **BLOCKED BY MISSING PRODUCT CAPABILITIES**",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "Domestic 102/103/112/113 credit/debit-note foundation",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "Reporte Diario v13.2 internal reconciliation foundation",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "Typed immutable Reporte Diario source-fact evidence",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "Lossless foreign-currency integration",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "typed Reporte Diario source facts for signed identity, AE/BE, A-C19 and FX provenance: **IMPLEMENTED / LOCALLY VALIDATED**",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "lossless non-UYU -> UYU Daily Report evidence composition: **IMPLEMENTED / LOCALLY VALIDATED**",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "sendable/signed/persisted Reporte Diario v13.2 artifact: **NOT YET IMPLEMENTED**",
            currentState,
            StringComparison.Ordinal);
        Assert.Contains(
            "Pin the authoritative Reporte Diario v13.2 wire contract",
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
