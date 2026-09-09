using Xunit;

namespace ArchitectureTests;

public sealed class FiscalPaymentFormEvidenceArchitectureTests
{
    [Fact]
    public void Sale_confirmation_freezes_payment_form_before_fiscalization_request_is_persisted()
    {
        var confirmation = Read("src/Application/Sales/SaleConfirmationTransaction.cs");

        var settlementEvidence = confirmation.IndexOf("BuildFiscalSettlementEvidence(settlement)", StringComparison.Ordinal);
        var fiscalization = confirmation.IndexOf("FiscalizationRequest.CreateFromSale", StringComparison.Ordinal);

        Assert.True(settlementEvidence >= 0, "Sale confirmation must freeze authoritative settlement evidence.");
        Assert.True(fiscalization > settlementEvidence, "Settlement evidence must be frozen before the fiscalization request is created.");
        Assert.Contains("SaleSettlementKind.ImmediatePayment", confirmation, StringComparison.Ordinal);
        Assert.Contains("FiscalPaymentForm.Cash", confirmation, StringComparison.Ordinal);
        Assert.Contains("SaleSettlementKind.CreditReceivable", confirmation, StringComparison.Ordinal);
        Assert.Contains("FiscalPaymentForm.Credit", confirmation, StringComparison.Ordinal);
    }

    [Fact]
    public void Mixed_and_no_charge_paths_do_not_invent_a_DGI_payment_form()
    {
        var confirmation = Read("src/Application/Sales/SaleConfirmationTransaction.cs");

        Assert.Contains("SaleSettlementKind.Mixed => new(", confirmation, StringComparison.Ordinal);
        Assert.Contains("FiscalSettlementKind.Mixed,\n                null,", confirmation, StringComparison.Ordinal);
        Assert.Contains("SaleSettlementKind.NoCharge => new(", confirmation, StringComparison.Ordinal);
        Assert.Contains("FiscalSettlementKind.NoCharge,\n                null,", confirmation, StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_form_completion_does_not_cross_into_XML_signing_or_transport()
    {
        var domain = Read("src/Domain/Fiscal/FiscalContentSnapshot.cs");
        var confirmation = Read("src/Application/Sales/SaleConfirmationTransaction.cs");
        var combined = domain + confirmation;

        Assert.Contains("FiscalSettlementEvidence", domain, StringComparison.Ordinal);
        Assert.Contains("FiscalPaymentForm", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("XDocument", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlDocument", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalSigner", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalTransportGateway", combined, StringComparison.Ordinal);
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
