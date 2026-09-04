using EFactura.Application.Fiscal;
using EFactura.Domain.Common;
using EFactura.Domain.Sales;
using Xunit;

namespace CrossCuttingTests;

public sealed class SaleConfirmationTransactionTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Validated_sale_becomes_confirmed_with_immutable_evidence_and_version_increment()
    {
        var sale = Draft();
        sale.MarkValidated("validated-evidence", DateTimeOffset.UtcNow, 1);

        sale.MarkConfirmed(Confirmation, Settlement, DateTimeOffset.UtcNow, 2);

        Assert.Equal(SaleStatus.Confirmed, sale.Status);
        Assert.Equal(3, sale.Version);
        Assert.Equal(Confirmation, sale.ConfirmationFingerprint);
        Assert.Equal(Settlement, sale.SettlementFingerprint);
        Assert.NotNull(sale.ConfirmedAtUtc);
    }

    [Fact]
    public void Confirmed_sale_cannot_be_edited_or_revalidated()
    {
        var sale = Draft();
        sale.MarkValidated("validated-evidence", DateTimeOffset.UtcNow, 1);
        sale.MarkConfirmed(Confirmation, Settlement, DateTimeOffset.UtcNow, 2);

        var edit = Assert.Throws<DomainRuleException>(() => sale.ReplaceDraft(
            null,
            SaleCommercialIntent.TaxpayerInvoice,
            "UYU",
            new DateOnly(2026, 9, 4),
            "UY",
            false,
            sale.Lines,
            3));
        Assert.Equal("sales.confirmed_immutable", edit.Code);

        var revalidate = Assert.Throws<DomainRuleException>(() =>
            sale.MarkValidated("new-validation", DateTimeOffset.UtcNow, 3));
        Assert.Equal("sales.confirmed_immutable", revalidate.Code);
    }

    [Fact]
    public void Draft_sale_and_invalid_fingerprints_fail_closed_before_confirmed_state()
    {
        var draft = Draft();
        var notValidated = Assert.Throws<DomainRuleException>(() =>
            draft.MarkConfirmed(Confirmation, Settlement, DateTimeOffset.UtcNow, 1));
        Assert.Equal("sales.confirmation.validation_required", notValidated.Code);

        var validated = Draft();
        validated.MarkValidated("validated-evidence", DateTimeOffset.UtcNow, 1);
        var badFingerprint = Assert.Throws<DomainRuleException>(() =>
            validated.MarkConfirmed("not-a-sha256", Settlement, DateTimeOffset.UtcNow, 2));
        Assert.Equal("sales.confirmation.confirmation_fingerprint_invalid", badFingerprint.Code);
        Assert.Equal(SaleStatus.Validated, validated.Status);
    }

    [Fact]
    public void Eligibility_and_authoritative_arithmetic_share_the_same_CFE_25_2_format_version()
    {
        Assert.Equal(
            UruguayCfe25_2ArithmeticCatalog.FormatVersion,
            UruguayCfe25_2EligibilityRulePackProvider.FormatVersion);
        Assert.Equal("25.2", UruguayCfe25_2EligibilityRulePackProvider.FormatVersion);
    }

    private static Sale Draft() => Sale.Create(
        Guid.NewGuid(),
        "company-1",
        "loc-1",
        "term-1",
        null,
        SaleCommercialIntent.TaxpayerInvoice,
        "UYU",
        new DateOnly(2026, 9, 4),
        "UY",
        false,
        new[]
        {
            SaleLine.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ITEM-1",
                "Item 1",
                SaleLineKind.Product,
                1m,
                100m,
                null)
        });
}
