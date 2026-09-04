using EFactura.Domain.Common;

namespace EFactura.Domain.Payments;

public sealed class PaymentMethod
{
    private PaymentMethod(Guid id, string organizationId, string name, bool enabled, long version)
    {
        if (id == Guid.Empty)
            throw Rule("payments.method_id_required", "Payment method id is required.");
        if (version <= 0)
            throw Rule("payments.method_version_invalid", "Payment method version must be positive.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "payments.organization_required");
        Name = Required(name, 120, "payments.method_name_required");
        Enabled = enabled;
        Version = version;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public string Name { get; private set; }
    public bool Enabled { get; private set; }
    public long Version { get; private set; }

    public static PaymentMethod Create(Guid id, string organizationId, string name, bool enabled = true) =>
        new(id, organizationId, name, enabled, 1);

    public static PaymentMethod Rehydrate(
        Guid id,
        string organizationId,
        string name,
        bool enabled,
        long version) =>
        new(id, organizationId, name, enabled, version);

    public void SetEnabled(bool enabled, long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        if (Enabled == enabled)
            return;

        Enabled = enabled;
        Version++;
    }

    public void Rename(string name, long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        var normalized = Required(name, 120, "payments.method_name_required");
        if (string.Equals(Name, normalized, StringComparison.Ordinal))
            return;

        Name = normalized;
        Version++;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw Rule("concurrency.stale_version", "Payment method changed before this operation was applied.");
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required payment value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Payment value cannot exceed {max} characters.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}

public sealed class Payment
{
    private Payment(
        Guid id,
        string organizationId,
        Guid saleId,
        int sequence,
        Guid paymentMethodId,
        long paymentMethodVersion,
        decimal amount,
        string currencyCode,
        string? externalReference,
        string confirmationFingerprint,
        string settlementFingerprint,
        DateTimeOffset recordedAtUtc)
    {
        if (id == Guid.Empty)
            throw Rule("payments.payment_id_required", "Payment id is required.");
        if (saleId == Guid.Empty)
            throw Rule("payments.sale_id_required", "Sale id is required for a sale payment.");
        if (sequence <= 0)
            throw Rule("payments.sequence_invalid", "Payment sequence must be positive.");
        if (paymentMethodId == Guid.Empty)
            throw Rule("payments.method_id_required", "Payment method id is required.");
        if (paymentMethodVersion <= 0)
            throw Rule("payments.method_version_invalid", "Payment method version must be positive.");
        if (amount <= 0m)
            throw Rule("payments.amount_invalid", "Payment amount must be greater than zero.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "payments.organization_required");
        SaleId = saleId;
        Sequence = sequence;
        PaymentMethodId = paymentMethodId;
        PaymentMethodVersion = paymentMethodVersion;
        Amount = amount;
        CurrencyCode = Currency(currencyCode);
        ExternalReference = Optional(externalReference, 200);
        ConfirmationFingerprint = Fingerprint(confirmationFingerprint, "payments.confirmation_fingerprint_invalid");
        SettlementFingerprint = Fingerprint(settlementFingerprint, "payments.settlement_fingerprint_invalid");
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid SaleId { get; }
    public int Sequence { get; }
    public Guid PaymentMethodId { get; }
    public long PaymentMethodVersion { get; }
    public decimal Amount { get; }
    public string CurrencyCode { get; }
    public string? ExternalReference { get; }
    public string ConfirmationFingerprint { get; }
    public string SettlementFingerprint { get; }
    public DateTimeOffset RecordedAtUtc { get; }

    public static Payment CreateFromSale(
        Guid id,
        string organizationId,
        Guid saleId,
        int sequence,
        Guid paymentMethodId,
        long paymentMethodVersion,
        decimal amount,
        string currencyCode,
        string? externalReference,
        string confirmationFingerprint,
        string settlementFingerprint,
        DateTimeOffset recordedAtUtc) =>
        new(id, organizationId, saleId, sequence, paymentMethodId, paymentMethodVersion, amount,
            currencyCode, externalReference, confirmationFingerprint, settlementFingerprint, recordedAtUtc);

    public static Payment Rehydrate(
        Guid id,
        string organizationId,
        Guid saleId,
        int sequence,
        Guid paymentMethodId,
        long paymentMethodVersion,
        decimal amount,
        string currencyCode,
        string? externalReference,
        string confirmationFingerprint,
        string settlementFingerprint,
        DateTimeOffset recordedAtUtc) =>
        new(id, organizationId, saleId, sequence, paymentMethodId, paymentMethodVersion, amount,
            currencyCode, externalReference, confirmationFingerprint, settlementFingerprint, recordedAtUtc);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required payment value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Payment value cannot exceed {max} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule("payments.value_too_long", $"Payment value cannot exceed {max} characters.");
        return normalized;
    }

    private static string Currency(string value)
    {
        var normalized = Required(value, 3, "payments.currency_required").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("payments.currency_invalid", "Payment currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static string Fingerprint(string value, string code)
    {
        var normalized = Required(value, 64, code).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Rule(code, "Payment evidence fingerprint must be a SHA-256 hexadecimal value.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
