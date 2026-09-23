using EFactura.Domain.Common;

namespace EFactura.Domain.Payables;

public enum PayableSourceKind
{
    PurchaseReceipt = 1,
    ReceivedFiscalDocument = 2
}

public sealed class Payable
{
    private Payable(
        Guid id,
        string organizationId,
        Guid supplierPartyId,
        PayableSourceKind sourceKind,
        string sourceId,
        decimal originalAmount,
        string currencyCode,
        DateOnly sourceEffectiveOn,
        DateOnly dueDate,
        long version,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
            throw Rule("payables.id_required", "Payable id is required.");
        if (supplierPartyId == Guid.Empty)
            throw Rule("payables.supplier_required", "Payable supplier Party is required.");
        if (!Enum.IsDefined(sourceKind))
            throw Rule("payables.source_kind_invalid", "Payable source kind is invalid.");
        if (originalAmount <= 0m)
            throw Rule("payables.original_amount_invalid", "Payable original amount must be greater than zero.");
        if (dueDate < sourceEffectiveOn)
            throw Rule("payables.due_date_invalid", "Payable due date cannot precede the source business date.");
        if (version <= 0)
            throw Rule("payables.version_invalid", "Payable version must be positive.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "payables.organization_required");
        SupplierPartyId = supplierPartyId;
        SourceKind = sourceKind;
        SourceId = Required(sourceId, 200, "payables.source_required");
        OriginalAmount = NormalizeAmount(originalAmount);
        CurrencyCode = Currency(currencyCode);
        SourceEffectiveOn = sourceEffectiveOn;
        DueDate = dueDate;
        Version = version;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid SupplierPartyId { get; }
    public PayableSourceKind SourceKind { get; }
    public string SourceId { get; }
    public decimal OriginalAmount { get; }
    public string CurrencyCode { get; }
    public DateOnly SourceEffectiveOn { get; }
    public DateOnly DueDate { get; }
    public long Version { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public static Payable CreateFromPurchaseReceipt(
        Guid id,
        string organizationId,
        Guid supplierPartyId,
        string purchaseReceiptId,
        decimal originalAmount,
        string currencyCode,
        DateOnly receiptEffectiveOn,
        DateOnly dueDate,
        DateTimeOffset createdAtUtc) =>
        new(
            id,
            organizationId,
            supplierPartyId,
            PayableSourceKind.PurchaseReceipt,
            purchaseReceiptId,
            originalAmount,
            currencyCode,
            receiptEffectiveOn,
            dueDate,
            1,
            createdAtUtc);

    public static Payable CreateFromReceivedFiscalDocument(
        Guid id,
        string organizationId,
        Guid supplierPartyId,
        string receivedFiscalDocumentId,
        decimal originalAmount,
        string currencyCode,
        DateOnly documentEffectiveOn,
        DateOnly dueDate,
        DateTimeOffset createdAtUtc) =>
        new(
            id,
            organizationId,
            supplierPartyId,
            PayableSourceKind.ReceivedFiscalDocument,
            receivedFiscalDocumentId,
            originalAmount,
            currencyCode,
            documentEffectiveOn,
            dueDate,
            1,
            createdAtUtc);

    public static Payable Rehydrate(
        Guid id,
        string organizationId,
        Guid supplierPartyId,
        PayableSourceKind sourceKind,
        string sourceId,
        decimal originalAmount,
        string currencyCode,
        DateOnly sourceEffectiveOn,
        DateOnly dueDate,
        long version,
        DateTimeOffset createdAtUtc) =>
        new(
            id,
            organizationId,
            supplierPartyId,
            sourceKind,
            sourceId,
            originalAmount,
            currencyCode,
            sourceEffectiveOn,
            dueDate,
            version,
            createdAtUtc);

    private static decimal NormalizeAmount(decimal value) =>
        decimal.Round(value, 6, MidpointRounding.ToEven);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required payable value is missing.");

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Payable value cannot exceed {max} characters.");
        return normalized;
    }

    private static string Currency(string value)
    {
        var normalized = Required(value, 3, "payables.currency_required").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("payables.currency_invalid", "Payable currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
