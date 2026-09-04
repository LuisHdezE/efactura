using EFactura.Domain.Common;
using EFactura.Domain.Payments;
using EFactura.Domain.Receivables;
using Xunit;

namespace CrossCuttingTests;

public sealed class FinancePersistenceFoundationTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Payment_method_carries_enabled_and_version_evidence()
    {
        var method = PaymentMethod.Create(Guid.NewGuid(), " company-1 ", " Card ");

        Assert.Equal("company-1", method.OrganizationId);
        Assert.Equal("Card", method.Name);
        Assert.True(method.Enabled);
        Assert.Equal(1, method.Version);

        method.SetEnabled(false, 1);
        Assert.False(method.Enabled);
        Assert.Equal(2, method.Version);
    }

    [Fact]
    public void Payment_method_rejects_stale_version()
    {
        var method = PaymentMethod.Create(Guid.NewGuid(), "company-1", "Transfer");
        var error = Assert.Throws<DomainRuleException>(() => method.SetEnabled(false, 99));
        Assert.Equal("concurrency.stale_version", error.Code);
    }

    [Fact]
    public void Sale_payment_preserves_authoritative_settlement_evidence()
    {
        var methodId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var payment = Payment.CreateFromSale(
            Guid.NewGuid(), "company-1", saleId, 1, methodId, 7, 125.50m, "uyu", " POS-42 ",
            Confirmation, Settlement, DateTimeOffset.UtcNow);

        Assert.Equal(saleId, payment.SaleId);
        Assert.Equal(methodId, payment.PaymentMethodId);
        Assert.Equal(7, payment.PaymentMethodVersion);
        Assert.Equal(125.50m, payment.Amount);
        Assert.Equal("UYU", payment.CurrencyCode);
        Assert.Equal("POS-42", payment.ExternalReference);
        Assert.Equal(Settlement, payment.SettlementFingerprint);
    }

    [Fact]
    public void Sale_payment_rejects_non_positive_amount()
    {
        var error = Assert.Throws<DomainRuleException>(() => Payment.CreateFromSale(
            Guid.NewGuid(), "company-1", Guid.NewGuid(), 1, Guid.NewGuid(), 1, 0m, "UYU", null,
            Confirmation, Settlement, DateTimeOffset.UtcNow));
        Assert.Equal("payments.amount_invalid", error.Code);
    }

    [Fact]
    public void Receivable_rejects_due_date_before_sale_date()
    {
        var error = Assert.Throws<DomainRuleException>(() => Receivable.CreateFromSale(
            Guid.NewGuid(), "company-1", Guid.NewGuid(), Guid.NewGuid(), 500m, "UYU",
            new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 3),
            Confirmation, Settlement, DateTimeOffset.UtcNow));
        Assert.Equal("receivables.due_date_invalid", error.Code);
    }

    [Fact]
    public void Receivable_preserves_original_obligation_without_client_balance_field()
    {
        var receivable = Receivable.CreateFromSale(
            Guid.NewGuid(), "company-1", Guid.NewGuid(), Guid.NewGuid(), 500m, "uyu",
            new DateOnly(2026, 9, 4), new DateOnly(2026, 10, 4),
            Confirmation, Settlement, DateTimeOffset.UtcNow);

        Assert.Equal(500m, receivable.OriginalAmount);
        Assert.Equal("UYU", receivable.CurrencyCode);
        Assert.Equal(1, receivable.Version);
        Assert.Equal(Settlement, receivable.SettlementFingerprint);
    }
}
