using EFactura.Domain.Common;

namespace EFactura.Domain.Receivables;

public sealed class Receivable
{
    private Receivable(
        Guid id,
        string organizationId,
        Guid customerPartyId,
        Guid saleId,
        decimal originalAmount,
        string currencyCode,
        DateOnly dueDate,
        string confirmationFingerprint,
        string settlementFingerprint,
        long version,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
            throw Rule("receivables.id_required", "Receivable id is required.");
        if (customerPartyId == Guid.Empty)
            throw Rule("receivables.customer_required", "Receivable customer party is required.");
        if (saleId == Guid.Empty)
            throw Rule("receivables.sale_required", "Receivable source sale is required.");
        if (originalAmount <= 0m)
            throw Rule("receivables.original_amount_invalid", "Receivable original amount must be greater than zero.");
        if (version <= 0)
            throw Rule("receivables.version_invalid", "Receivable version must be positive.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "receivables.organization_required");
        CustomerPartyId = customerPartyId;
        SaleId = saleId;
        OriginalAmount = originalAmount;
        CurrencyCode = Currency(currencyCode);
        DueDate = dueDate;
        ConfirmationFingerprint = Fingerprint(confirmationFingerprint, "receivables.confirmation_fingerprint_invalid");
        SettlementFingerprint = Fingerprint(settlementFingerprint, "receivables.settlement_fingerprint_invalid");
        Version = version;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid CustomerPartyId { get; }
    public Guid SaleId { get; }
    public decimal OriginalAmount { get; }
    public string CurrencyCode { get; }
    public DateOnly DueDate { get; }
    public string ConfirmationFingerprint { get; }
    public string SettlementFingerprint { get; }
    public long Version { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public static Receivable CreateFromSale(
        Guid id,
        string organizationId,
        Guid customerPartyId,
        Guid saleId,
        decimal originalAmount,
        string currencyCode,
        DateOnly saleEffectiveOn,
        DateOnly dueDate,
        string confirmationFingerprint,
        string settlementFingerprint,
        DateTimeOffset createdAtUtc)
    {
        if (dueDate < saleEffectiveOn)
            throw Rule("receivables.due_date_invalid", "Receivable due date cannot precede the source sale business date.");

        return new Receivable(
            id,
            organizationId,
            customerPartyId,
            saleId,
            originalAmount,
            currencyCode,
            dueDate,
            confirmationFingerprint,
            settlementFingerprint,
            1,
            createdAtUtc);
    }

    public static Receivable Rehydrate(
        Guid id,
        string organizationId,
        Guid customerPartyId,
        Guid saleId,
        decimal originalAmount,
        string currencyCode,
        DateOnly dueDate,
        string confirmationFingerprint,
        string settlementFingerprint,
        long version,
        DateTimeOffset createdAtUtc) =>
        new(id, organizationId, customerPartyId, saleId, originalAmount, currencyCode, dueDate,
            confirmationFingerprint, settlementFingerprint, version, createdAtUtc);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required receivable value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Receivable value cannot exceed {max} characters.");
        return normalized;
    }

    private static string Currency(string value)
    {
        var normalized = Required(value, 3, "receivables.currency_required").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("receivables.currency_invalid", "Receivable currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static string Fingerprint(string value, string code)
    {
        var normalized = Required(value, 64, code).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Rule(code, "Receivable evidence fingerprint must be a SHA-256 hexadecimal value.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
