using EFactura.Domain.Common;

namespace EFactura.Domain.Fiscal;

public enum FiscalizationRequestStatus
{
    Pending = 1
}

public sealed class FiscalizationRequest
{
    private FiscalizationRequest(
        Guid id,
        string organizationId,
        Guid saleId,
        string? locationId,
        string? terminalId,
        CfeFamily cfeFamily,
        ReceiverIdentificationRequirement? receiverIdentification,
        string formatVersion,
        string confirmationFingerprint,
        string settlementFingerprint,
        string currencyCode,
        decimal netAmount,
        decimal vatAmount,
        decimal totalAmount,
        FiscalizationRequestStatus status,
        long version,
        DateTimeOffset requestedAtUtc)
    {
        if (id == Guid.Empty)
            throw Rule("fiscalization.request_id_required", "Fiscalization request id is required.");
        if (saleId == Guid.Empty)
            throw Rule("fiscalization.sale_id_required", "Fiscalization request requires a source sale id.");
        if (!Enum.IsDefined(cfeFamily))
            throw Rule("fiscalization.cfe_family_invalid", "Fiscalization request requires a supported CFE family.");
        if (!Enum.IsDefined(status))
            throw Rule("fiscalization.status_invalid", "Fiscalization request status is invalid.");
        if (version <= 0)
            throw Rule("fiscalization.version_invalid", "Fiscalization request version must be positive.");
        if (netAmount < 0m || vatAmount < 0m || totalAmount < 0m)
            throw Rule("fiscalization.amount_invalid", "Fiscalization request amounts cannot be negative.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "fiscalization.organization_required");
        SaleId = saleId;
        LocationId = Optional(locationId, 200);
        TerminalId = Optional(terminalId, 200);
        CfeFamily = cfeFamily;
        ReceiverIdentification = receiverIdentification;
        FormatVersion = Required(formatVersion, 40, "fiscalization.format_version_required");
        ConfirmationFingerprint = Fingerprint(confirmationFingerprint, "fiscalization.confirmation_fingerprint_invalid");
        SettlementFingerprint = Fingerprint(settlementFingerprint, "fiscalization.settlement_fingerprint_invalid");
        CurrencyCode = Currency(currencyCode);
        NetAmount = netAmount;
        VatAmount = vatAmount;
        TotalAmount = totalAmount;
        Status = status;
        Version = version;
        RequestedAtUtc = requestedAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid SaleId { get; }
    public string? LocationId { get; }
    public string? TerminalId { get; }
    public CfeFamily CfeFamily { get; }
    public ReceiverIdentificationRequirement? ReceiverIdentification { get; }
    public string FormatVersion { get; }
    public string ConfirmationFingerprint { get; }
    public string SettlementFingerprint { get; }
    public string CurrencyCode { get; }
    public decimal NetAmount { get; }
    public decimal VatAmount { get; }
    public decimal TotalAmount { get; }
    public FiscalizationRequestStatus Status { get; }
    public long Version { get; }
    public DateTimeOffset RequestedAtUtc { get; }

    public static FiscalizationRequest CreateFromSale(
        Guid id,
        string organizationId,
        Guid saleId,
        string? locationId,
        string? terminalId,
        CfeFamily cfeFamily,
        ReceiverIdentificationRequirement? receiverIdentification,
        string formatVersion,
        string confirmationFingerprint,
        string settlementFingerprint,
        string currencyCode,
        decimal netAmount,
        decimal vatAmount,
        decimal totalAmount,
        DateTimeOffset requestedAtUtc) =>
        new(
            id,
            organizationId,
            saleId,
            locationId,
            terminalId,
            cfeFamily,
            receiverIdentification,
            formatVersion,
            confirmationFingerprint,
            settlementFingerprint,
            currencyCode,
            netAmount,
            vatAmount,
            totalAmount,
            FiscalizationRequestStatus.Pending,
            1,
            requestedAtUtc);

    public static FiscalizationRequest Rehydrate(
        Guid id,
        string organizationId,
        Guid saleId,
        string? locationId,
        string? terminalId,
        CfeFamily cfeFamily,
        ReceiverIdentificationRequirement? receiverIdentification,
        string formatVersion,
        string confirmationFingerprint,
        string settlementFingerprint,
        string currencyCode,
        decimal netAmount,
        decimal vatAmount,
        decimal totalAmount,
        FiscalizationRequestStatus status,
        long version,
        DateTimeOffset requestedAtUtc) =>
        new(
            id,
            organizationId,
            saleId,
            locationId,
            terminalId,
            cfeFamily,
            receiverIdentification,
            formatVersion,
            confirmationFingerprint,
            settlementFingerprint,
            currencyCode,
            netAmount,
            vatAmount,
            totalAmount,
            status,
            version,
            requestedAtUtc);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required fiscalization value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Fiscalization value cannot exceed {max} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule("fiscalization.value_too_long", $"Fiscalization value cannot exceed {max} characters.");
        return normalized;
    }

    private static string Currency(string value)
    {
        var normalized = Required(value, 3, "fiscalization.currency_required").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("fiscalization.currency_invalid", "Fiscalization currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static string Fingerprint(string value, string code)
    {
        var normalized = Required(value, 64, code).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Rule(code, "Fiscalization fingerprint must be a SHA-256 hexadecimal value.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
