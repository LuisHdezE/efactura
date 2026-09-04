using Xunit;

namespace ArchitectureTests;

public sealed class SalesConfirmationApiContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void API_SAL_007_requires_confirm_permission_idempotency_and_delegates_to_atomic_use_case()
    {
        var controller = Read("src/WebApi/Controllers/V1/SalesController.cs");

        Assert.Contains("[HttpPost(\"{saleId:guid}/confirm\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[RequirePermission(Permissions.SalesConfirm)]", controller, StringComparison.Ordinal);
        Assert.Contains("ConfirmSaleUseCase", controller, StringComparison.Ordinal);
        Assert.Contains("new ConfirmSaleCommand(", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.RequireIdempotencyKey(Request)", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.ComputeRequestHash(request)", controller, StringComparison.Ordinal);
        Assert.Contains("SetReplayHeader(result.Replayed)", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("V1PersistenceDbContext", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalNumberAllocator", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("ICaeRepository", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_request_accepts_settlement_intent_but_not_server_owned_authoritative_evidence()
    {
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/SalesContracts.cs");
        var requestSurface = Slice(
            contracts,
            "public sealed record SaleImmediatePaymentRequest",
            "public sealed record SaleConfirmationDto");

        Assert.Contains("PaymentMethodId", requestSurface, StringComparison.Ordinal);
        Assert.Contains("Amount", requestSurface, StringComparison.Ordinal);
        Assert.Contains("CurrencyCode", requestSurface, StringComparison.Ordinal);
        Assert.Contains("ExternalReference", requestSurface, StringComparison.Ordinal);
        Assert.Contains("ExpectedVersion", requestSurface, StringComparison.Ordinal);
        Assert.Contains("PaymentIntents", requestSurface, StringComparison.Ordinal);
        Assert.Contains("CreditTerms", requestSurface, StringComparison.Ordinal);
        Assert.Contains("DueDate", requestSurface, StringComparison.Ordinal);
        Assert.Contains("OperatorReason", requestSurface, StringComparison.Ordinal);
        Assert.Contains("OperatorContext", requestSurface, StringComparison.Ordinal);

        Assert.DoesNotContain("OrganizationId", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("LocationId", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("TerminalId", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("NetAmount", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("TaxAmount", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("TotalAmount", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("CfeFamily", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("ValidationFingerprint", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfirmationFingerprint", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("SettlementFingerprint", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("PositionVersion", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("ReceivableAmount", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentMethodVersion", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("Cae", requestSurface, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FiscalNumber", requestSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("Xml", requestSurface, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Confirm_response_is_a_local_transaction_receipt_not_a_fiscal_document_result()
    {
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/SalesContracts.cs");
        var responseSurface = Slice(
            contracts,
            "public sealed record SaleConfirmationDto",
            "public sealed record SaleLineDto");

        Assert.Contains("SaleId", responseSurface, StringComparison.Ordinal);
        Assert.Contains("Version", responseSurface, StringComparison.Ordinal);
        Assert.Contains("ConfirmationFingerprint", responseSurface, StringComparison.Ordinal);
        Assert.Contains("SettlementFingerprint", responseSurface, StringComparison.Ordinal);
        Assert.Contains("FiscalizationRequestId", responseSurface, StringComparison.Ordinal);
        Assert.Contains("PaymentCount", responseSurface, StringComparison.Ordinal);
        Assert.Contains("ReceivableId", responseSurface, StringComparison.Ordinal);
        Assert.Contains("ConfirmedAtUtc", responseSurface, StringComparison.Ordinal);
        Assert.Contains("Replayed", responseSurface, StringComparison.Ordinal);

        Assert.DoesNotContain("Cae", responseSurface, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FiscalNumber", responseSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("FiscalDocumentId", responseSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("Xml", responseSurface, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Signature", responseSurface, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dgi", responseSurface, StringComparison.OrdinalIgnoreCase);
    }

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = source.IndexOf(endMarker, start >= 0 ? start : 0, StringComparison.Ordinal);
        if (start < 0 || end <= start)
            throw new InvalidOperationException($"Could not isolate contract surface between '{startMarker}' and '{endMarker}'.");
        return source[start..end];
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
