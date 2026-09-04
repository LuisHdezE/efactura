using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EFactura.Domain.Common;
using EFactura.Domain.Sales;

namespace EFactura.Application.Sales;

public enum SaleSettlementKind
{
    NoCharge = 0,
    ImmediatePayment = 1,
    CreditReceivable = 2,
    Mixed = 3
}

public sealed record SalePaymentMethodEvidence(
    Guid PaymentMethodId,
    string OrganizationId,
    long Version,
    bool Enabled);

public sealed record SaleImmediatePaymentIntent(
    Guid PaymentMethodId,
    decimal Amount,
    string CurrencyCode,
    string? ExternalReference = null);

public sealed record SaleCreditTerms(DateOnly DueDate);

public sealed record PlannedSalePayment(
    Guid PaymentMethodId,
    long PaymentMethodVersion,
    decimal Amount,
    string CurrencyCode,
    string? ExternalReference);

public sealed record PlannedSaleReceivable(
    Guid CustomerPartyId,
    decimal OriginalAmount,
    string CurrencyCode,
    DateOnly DueDate);

public sealed record SaleSettlementPlanningRequest(
    Sale Sale,
    SaleConfirmationPlan Confirmation,
    IReadOnlyCollection<SaleImmediatePaymentIntent> PaymentIntents,
    IReadOnlyCollection<SalePaymentMethodEvidence> PaymentMethods,
    SaleCreditTerms? CreditTerms);

public sealed record SaleSettlementPlan(
    Guid SaleId,
    long SaleVersion,
    string ConfirmationFingerprint,
    string SettlementFingerprint,
    string CurrencyCode,
    decimal SaleTotal,
    SaleSettlementKind Kind,
    IReadOnlyCollection<PlannedSalePayment> ImmediatePayments,
    PlannedSaleReceivable? Receivable);

/// <summary>
/// Creates the deterministic financial coverage plan that a future confirmSale transaction must consume.
/// It never persists a payment/receivable, changes Sale, touches CashManagement, mutates inventory,
/// allocates CAE or owns a transaction. The authoritative sale total comes only from the accepted
/// SaleConfirmationPlan fiscal calculation.
/// </summary>
public sealed class SaleSettlementPlanner
{
    public SaleSettlementPlan Prepare(SaleSettlementPlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Sale);
        ArgumentNullException.ThrowIfNull(request.Confirmation);

        ValidateConfirmationLink(request.Sale, request.Confirmation);

        if (request.Confirmation.FiscalCalculation is null)
            throw Rule("sales.settlement.fiscal_calculation_required", "Authoritative fiscal calculation is required before settlement planning.");

        var currencyCode = NormalizeCurrency(request.Confirmation.FiscalCalculation.CurrencyCode);
        if (!string.Equals(currencyCode, request.Sale.CurrencyCode, StringComparison.Ordinal))
            throw Rule("sales.settlement.currency_mismatch", "Sale and authoritative fiscal calculation currencies must match.");

        var totalAmount = request.Confirmation.FiscalCalculation.Totals.TotalAmount;
        if (totalAmount < 0m)
            throw Rule("sales.settlement.total_negative", "Sale settlement cannot plan a negative authoritative total.");

        if (request.PaymentIntents is null)
            throw Rule("sales.settlement.payment_intents_required", "Payment intents collection is required, even when empty.");
        if (request.PaymentMethods is null)
            throw Rule("sales.settlement.payment_method_evidence_required", "Payment-method evidence collection is required, even when empty.");
        if (request.PaymentIntents.Any(intent => intent is null))
            throw Rule("sales.settlement.payment_intent_required", "Payment intents cannot contain null entries.");
        if (request.PaymentMethods.Any(method => method is null))
            throw Rule("sales.settlement.payment_method_evidence_invalid", "Payment-method evidence cannot contain null entries.");
        if (request.PaymentMethods.Select(method => method.PaymentMethodId).Distinct().Count() != request.PaymentMethods.Count)
            throw Rule("sales.settlement.payment_method_evidence_duplicate", "Payment-method evidence must contain at most one row per payment method.");

        var payments = new List<PlannedSalePayment>(request.PaymentIntents.Count);
        foreach (var intent in request.PaymentIntents)
        {
            if (intent.PaymentMethodId == Guid.Empty)
                throw Rule("sales.settlement.payment_method_required", "Every immediate payment intent requires a payment method.");
            if (intent.Amount <= 0m)
                throw Rule("sales.settlement.payment_amount_invalid", "Immediate payment amounts must be greater than zero.");

            var paymentCurrency = NormalizeCurrency(intent.CurrencyCode);
            if (!string.Equals(paymentCurrency, currencyCode, StringComparison.Ordinal))
            {
                throw Rule(
                    "sales.settlement.cross_currency_not_supported",
                    "Release 1 sale settlement requires immediate payments in the same currency as the sale until an explicit FX settlement policy is accepted.");
            }

            var method = request.PaymentMethods.SingleOrDefault(x => x.PaymentMethodId == intent.PaymentMethodId)
                ?? throw Rule(
                    "sales.settlement.payment_method_evidence_missing",
                    "Authoritative payment-method evidence is missing for an immediate payment intent.");
            if (!string.Equals(method.OrganizationId?.Trim(), request.Sale.OrganizationId, StringComparison.Ordinal))
                throw Rule("sales.settlement.payment_method_scope_mismatch", "Payment method belongs to a different organization.");
            if (method.Version <= 0)
                throw Rule("sales.settlement.payment_method_version_invalid", "Payment-method evidence requires a positive version.");
            if (!method.Enabled)
                throw Rule("sales.settlement.payment_method_disabled", "Disabled payment methods cannot be used to settle a sale.");

            payments.Add(new PlannedSalePayment(
                intent.PaymentMethodId,
                method.Version,
                intent.Amount,
                paymentCurrency,
                Optional(intent.ExternalReference, 200)));
        }

        var immediateTotal = payments.Sum(payment => payment.Amount);
        if (immediateTotal > totalAmount)
        {
            throw Rule(
                "sales.settlement.overpayment_not_supported",
                "Overpayment or customer advance requires an explicit accepted policy and cannot be inferred during sale confirmation.");
        }

        PlannedSaleReceivable? receivable = null;
        SaleSettlementKind kind;

        if (totalAmount == 0m)
        {
            if (payments.Count != 0 || request.CreditTerms is not null)
                throw Rule("sales.settlement.zero_total_effect_forbidden", "A zero-total sale cannot create payment or receivable effects.");
            kind = SaleSettlementKind.NoCharge;
        }
        else if (request.CreditTerms is null)
        {
            if (immediateTotal != totalAmount)
            {
                throw Rule(
                    "sales.settlement.uncovered_balance",
                    "Immediate payments must cover the authoritative total exactly when no credit terms are supplied.");
            }
            kind = SaleSettlementKind.ImmediatePayment;
        }
        else
        {
            if (!request.Sale.CustomerPartyId.HasValue || request.Sale.CustomerPartyId.Value == Guid.Empty)
                throw Rule("sales.settlement.credit_customer_required", "Credit settlement requires an identified customer party.");
            if (request.CreditTerms.DueDate < request.Sale.EffectiveOn)
                throw Rule("sales.settlement.credit_due_date_invalid", "Receivable due date cannot precede the sale business date.");

            var residual = totalAmount - immediateTotal;
            if (residual <= 0m)
                throw Rule("sales.settlement.credit_terms_without_balance", "Credit terms require a positive residual balance after immediate payments.");

            receivable = new PlannedSaleReceivable(
                request.Sale.CustomerPartyId.Value,
                residual,
                currencyCode,
                request.CreditTerms.DueDate);
            kind = payments.Count == 0
                ? SaleSettlementKind.CreditReceivable
                : SaleSettlementKind.Mixed;
        }

        var confirmationFingerprint = NormalizeFingerprint(request.Confirmation.ConfirmationFingerprint);
        var settlementFingerprint = BuildSettlementFingerprint(
            request.Sale,
            confirmationFingerprint,
            currencyCode,
            totalAmount,
            kind,
            payments,
            receivable);

        return new SaleSettlementPlan(
            request.Sale.Id,
            request.Sale.Version,
            confirmationFingerprint,
            settlementFingerprint,
            currencyCode,
            totalAmount,
            kind,
            payments.ToArray(),
            receivable);
    }

    private static void ValidateConfirmationLink(Sale sale, SaleConfirmationPlan confirmation)
    {
        if (sale.Status != SaleStatus.Validated)
            throw Rule("sales.settlement.validation_required", "Only a validated sale can produce a settlement plan.");
        if (confirmation.SaleId != sale.Id || confirmation.SaleVersion != sale.Version)
            throw Rule("sales.settlement.confirmation_stale", "Confirmation plan no longer matches the authoritative sale identity/version.");
        if (string.IsNullOrWhiteSpace(sale.ValidationFingerprint)
            || !string.Equals(
                sale.ValidationFingerprint.Trim(),
                confirmation.ValidationFingerprint?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw Rule("sales.settlement.validation_fingerprint_mismatch", "Confirmation plan no longer matches the sale validation evidence.");
        }

        _ = NormalizeFingerprint(confirmation.ConfirmationFingerprint);
    }

    private static string BuildSettlementFingerprint(
        Sale sale,
        string confirmationFingerprint,
        string currencyCode,
        decimal totalAmount,
        SaleSettlementKind kind,
        IReadOnlyCollection<PlannedSalePayment> payments,
        PlannedSaleReceivable? receivable)
    {
        var material = new StringBuilder()
            .Append(sale.OrganizationId).Append('|')
            .Append(sale.Id.ToString("N")).Append('|')
            .Append(sale.Version).Append('|')
            .Append(confirmationFingerprint).Append('|')
            .Append(currencyCode).Append('|')
            .Append(Decimal(totalAmount)).Append('|')
            .Append((int)kind);

        foreach (var payment in payments
                     .OrderBy(x => x.PaymentMethodId)
                     .ThenBy(x => x.Amount)
                     .ThenBy(x => x.ExternalReference, StringComparer.Ordinal))
        {
            material.Append('|')
                .Append(payment.PaymentMethodId.ToString("N")).Append(':')
                .Append(payment.PaymentMethodVersion).Append(':')
                .Append(Decimal(payment.Amount)).Append(':')
                .Append(payment.CurrencyCode).Append(':')
                .Append(payment.ExternalReference ?? "-");
        }

        if (receivable is not null)
        {
            material.Append('|')
                .Append("AR:")
                .Append(receivable.CustomerPartyId.ToString("N")).Append(':')
                .Append(Decimal(receivable.OriginalAmount)).Append(':')
                .Append(receivable.CurrencyCode).Append(':')
                .Append(receivable.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material.ToString())))
            .ToLowerInvariant();
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule("sales.settlement.currency_required", "Settlement currency is required.");
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("sales.settlement.currency_invalid", "Settlement currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static string NormalizeFingerprint(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule("sales.settlement.confirmation_fingerprint_required", "Confirmation fingerprint is required before settlement planning.");
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Rule("sales.settlement.confirmation_fingerprint_invalid", "Confirmation fingerprint must be a SHA-256 hexadecimal value.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule("sales.settlement.value_too_long", $"Settlement value cannot exceed {max} characters.");
        return normalized;
    }

    private static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
