using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Inventory;
using Xunit;

namespace CrossCuttingTests;

public sealed class SaleLocalEffectsTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Sale_consumption_decrements_stock_and_preserves_structured_source_evidence()
    {
        var saleId = Guid.NewGuid();
        var position = InventoryPosition.Rehydrate(Guid.NewGuid(), "company-1", Guid.NewGuid(), "loc-1", 10m, 7);

        var movement = position.ConsumeForSale(
            saleId, 3m, Confirmation, Settlement, DateTimeOffset.UtcNow, 7);

        Assert.Equal(7m, position.Quantity);
        Assert.Equal(8, position.Version);
        Assert.Equal(StockMovementKind.SaleConsumption, movement.Kind);
        Assert.Equal(-3m, movement.QuantityDelta);
        Assert.Equal(saleId, movement.SourceSaleId);
        Assert.Equal(Confirmation, movement.ConfirmationFingerprint);
        Assert.Equal(Settlement, movement.SettlementFingerprint);
        Assert.Equal("SALE_CONFIRMATION", movement.ReasonCode);
    }

    [Fact]
    public void Sale_consumption_rejects_stale_position_without_mutating_stock()
    {
        var position = InventoryPosition.Rehydrate(Guid.NewGuid(), "company-1", Guid.NewGuid(), "loc-1", 10m, 7);

        var error = Assert.Throws<DomainRuleException>(() => position.ConsumeForSale(
            Guid.NewGuid(), 2m, Confirmation, Settlement, DateTimeOffset.UtcNow, 6));

        Assert.Equal("concurrency.stale_version", error.Code);
        Assert.Equal(10m, position.Quantity);
        Assert.Equal(7, position.Version);
    }

    [Fact]
    public void Sale_consumption_rejects_insufficient_stock_without_mutating_stock()
    {
        var position = InventoryPosition.Rehydrate(Guid.NewGuid(), "company-1", Guid.NewGuid(), "loc-1", 1m, 3);

        var error = Assert.Throws<DomainRuleException>(() => position.ConsumeForSale(
            Guid.NewGuid(), 2m, Confirmation, Settlement, DateTimeOffset.UtcNow, 3));

        Assert.Equal("inventory.insufficient_stock", error.Code);
        Assert.Equal(1m, position.Quantity);
        Assert.Equal(3, position.Version);
    }

    [Fact]
    public void Sale_consumption_rejects_invalid_fingerprint_without_mutating_stock()
    {
        var position = InventoryPosition.Rehydrate(Guid.NewGuid(), "company-1", Guid.NewGuid(), "loc-1", 5m, 2);

        var error = Assert.Throws<DomainRuleException>(() => position.ConsumeForSale(
            Guid.NewGuid(), 1m, "not-a-sha", Settlement, DateTimeOffset.UtcNow, 2));

        Assert.Equal("inventory.confirmation_fingerprint_invalid", error.Code);
        Assert.Equal(5m, position.Quantity);
        Assert.Equal(2, position.Version);
    }

    [Fact]
    public void Ordinary_adjustment_remains_unlinked_to_sale_confirmation()
    {
        var position = InventoryPosition.Rehydrate(Guid.NewGuid(), "company-1", Guid.NewGuid(), "loc-1", 5m, 2);

        var movement = position.ApplyAdjustment(2m, "COUNT", "cycle count", DateTimeOffset.UtcNow, 2);

        Assert.Equal(StockMovementKind.Adjustment, movement.Kind);
        Assert.Null(movement.SourceSaleId);
        Assert.Null(movement.ConfirmationFingerprint);
        Assert.Null(movement.SettlementFingerprint);
    }

    [Fact]
    public void Fiscalization_request_starts_pending_and_preserves_authoritative_sale_evidence()
    {
        var saleId = Guid.NewGuid();
        var requestedAt = DateTimeOffset.UtcNow;

        var request = FiscalizationRequest.CreateFromSale(
            Guid.NewGuid(), "company-1", saleId, "loc-1", "term-1",
            CfeFamily.EFactura, ReceiverIdentificationRequirement.Required, "25.2",
            Confirmation, Settlement, "uyu", 100m, 22m, 122m, requestedAt);

        Assert.Equal(saleId, request.SaleId);
        Assert.Equal(FiscalizationRequestStatus.Pending, request.Status);
        Assert.Equal(1, request.Version);
        Assert.Equal(CfeFamily.EFactura, request.CfeFamily);
        Assert.Equal(ReceiverIdentificationRequirement.Required, request.ReceiverIdentification);
        Assert.Equal("25.2", request.FormatVersion);
        Assert.Equal("UYU", request.CurrencyCode);
        Assert.Equal(100m, request.NetAmount);
        Assert.Equal(22m, request.VatAmount);
        Assert.Equal(122m, request.TotalAmount);
        Assert.Equal(Confirmation, request.ConfirmationFingerprint);
        Assert.Equal(Settlement, request.SettlementFingerprint);
        Assert.Equal(requestedAt, request.RequestedAtUtc);
    }

    [Fact]
    public void Fiscalization_request_rejects_unsupported_cfe_family()
    {
        var error = Assert.Throws<DomainRuleException>(() => FiscalizationRequest.CreateFromSale(
            Guid.NewGuid(), "company-1", Guid.NewGuid(), "loc-1", null,
            (CfeFamily)999, null, "25.2", Confirmation, Settlement, "UYU",
            100m, 22m, 122m, DateTimeOffset.UtcNow));

        Assert.Equal("fiscalization.cfe_family_invalid", error.Code);
    }

    [Fact]
    public void Fiscalization_request_rejects_negative_authoritative_amounts()
    {
        var error = Assert.Throws<DomainRuleException>(() => FiscalizationRequest.CreateFromSale(
            Guid.NewGuid(), "company-1", Guid.NewGuid(), "loc-1", null,
            CfeFamily.ETicket, ReceiverIdentificationRequirement.Optional, "25.2",
            Confirmation, Settlement, "UYU", -1m, 0m, 0m, DateTimeOffset.UtcNow));

        Assert.Equal("fiscalization.amount_invalid", error.Code);
    }
}
