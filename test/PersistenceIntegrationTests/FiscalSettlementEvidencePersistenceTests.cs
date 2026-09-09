using System.Text.Json;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalSettlementEvidencePersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Fiscalization_request_round_trips_credit_payment_form_and_due_date(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var now = DateTimeOffset.UtcNow;
        var saleId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var dueDate = new DateOnly(2026, 10, 15);
        var evidence = Capture(
            Guid.NewGuid(),
            new FiscalSettlementEvidence(
                FiscalSettlementKind.CreditReceivable,
                FiscalPaymentForm.Credit,
                dueDate));

        await using (var context = database.CreateContext())
        {
            SeedSale(context, saleId, now);
            await new EfFiscalizationRequestRepository(context).AddAsync(
                FiscalizationRequest.CreateFromSale(
                    requestId,
                    "company-1",
                    saleId,
                    null,
                    null,
                    CfeFamily.EFactura,
                    ReceiverIdentificationRequirement.Required,
                    "25.2",
                    Confirmation,
                    Settlement,
                    "UYU",
                    100m,
                    22m,
                    122m,
                    now,
                    evidence));
            await context.SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        var restored = await new EfFiscalizationRequestRepository(verification)
            .GetAsync("company-1", requestId)
            ?? throw new InvalidOperationException("Fiscalization request was not persisted.");

        Assert.NotNull(restored.ConfirmationEvidence);
        Assert.NotNull(restored.ConfirmationEvidence!.Settlement);
        Assert.Equal(FiscalSettlementKind.CreditReceivable, restored.ConfirmationEvidence.Settlement!.Kind);
        Assert.Equal(FiscalPaymentForm.Credit, restored.ConfirmationEvidence.Settlement.PaymentForm);
        Assert.Equal(dueDate, restored.ConfirmationEvidence.Settlement.DueDate);
        Assert.Equal(evidence.EvidenceFingerprint, restored.ConfirmationEvidence.EvidenceFingerprint);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Historical_confirmation_JSON_without_settlement_property_remains_readable(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var now = DateTimeOffset.UtcNow;
        var saleId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var legacy = Capture(Guid.NewGuid(), null);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var currentJson = JsonSerializer.Serialize(legacy, options);
        var historicalJson = currentJson.Replace(",\"settlement\":null}", "}", StringComparison.Ordinal);

        await using (var context = database.CreateContext())
        {
            SeedSale(context, saleId, now);
            context.Set<V1FiscalizationRequestRecord>().Add(new V1FiscalizationRequestRecord
            {
                Id = requestId,
                OrganizationId = "company-1",
                SaleId = saleId,
                CfeFamily = (int)CfeFamily.EFactura,
                ReceiverIdentification = (int)ReceiverIdentificationRequirement.Required,
                FormatVersion = "25.2",
                ConfirmationFingerprint = Confirmation,
                SettlementFingerprint = Settlement,
                CurrencyCode = "UYU",
                NetAmount = 100m,
                VatAmount = 22m,
                TotalAmount = 122m,
                ConfirmationEvidenceFingerprint = legacy.EvidenceFingerprint,
                ConfirmationEvidenceJson = historicalJson,
                Status = (int)FiscalizationRequestStatus.Pending,
                Version = 1,
                RequestedAtUtc = now
            });
            await context.SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        var restored = await new EfFiscalizationRequestRepository(verification)
            .GetAsync("company-1", requestId)
            ?? throw new InvalidOperationException("Historical fiscalization request was not persisted.");

        Assert.NotNull(restored.ConfirmationEvidence);
        Assert.Null(restored.ConfirmationEvidence!.Settlement);
        Assert.Equal(legacy.EvidenceFingerprint, restored.ConfirmationEvidence.EvidenceFingerprint);
        restored.ConfirmationEvidence.EnsureIntegrity();
    }

    private static void SeedSale(
        Infrastructure.Persistence.V1.Write.V1PersistenceDbContext context,
        Guid saleId,
        DateTimeOffset now)
    {
        context.Sales.Add(new V1SaleRecord
        {
            Id = saleId,
            OrganizationId = "company-1",
            Intent = (int)SaleCommercialIntent.TaxpayerInvoice,
            CurrencyCode = "UYU",
            EffectiveOnUtc = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc),
            Status = (int)SaleStatus.Confirmed,
            ValidationFingerprint = "validated-settlement-evidence",
            ValidatedAtUtc = now,
            ConfirmationFingerprint = Confirmation,
            SettlementFingerprint = Settlement,
            ConfirmedAtUtc = now,
            Version = 3,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private static FiscalConfirmationEvidence Capture(
        Guid lineId,
        FiscalSettlementEvidence? settlement)
    {
        var rule = new RegulatoryRuleEvidence(
            "TEST-CFE-SETTLEMENT-RULE",
            "DGI test evidence",
            "https://example.invalid/dgi-rule",
            "25.2-test",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "test clause");
        var calculation = new CfeArithmeticResult(
            "UYU",
            "25.2",
            "UY-CFE-25.2-ARITH-R1",
            new[]
            {
                new CfeArithmeticLineResult(
                    lineId,
                    100m,
                    VatLiabilityKind.VatDue,
                    VatRateKind.Basic,
                    22m,
                    new[] { rule },
                    "UY-VAT-RATE-R1")
            },
            new CfeArithmeticTotals(100m, 0m, 100m, 0m, 0m, 22m, 22m, 122m),
            new[] { rule });

        return FiscalConfirmationEvidence.Capture(
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            calculation,
            new[] { rule },
            settlement);
    }
}
