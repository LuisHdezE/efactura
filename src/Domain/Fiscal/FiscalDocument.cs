using EFactura.Domain.Common;

namespace EFactura.Domain.Fiscal;

public enum FiscalDocumentStatus
{
    IdentityCreated = 1
}

/// <summary>
/// Owns the immutable server-assigned fiscal identity and the authoritative confirmation snapshot
/// needed by later artifact-generation steps. XML, signature, transport and DGI result state are
/// deliberately outside this foundation.
/// </summary>
public sealed class FiscalDocument
{
    private FiscalDocument(
        Guid id,
        string organizationId,
        Guid fiscalizationRequestId,
        Guid saleId,
        Guid fiscalNumberReservationId,
        Guid caeAuthorizationId,
        Guid? caeAllocationId,
        CfeFamily cfeType,
        string series,
        long number,
        string caeAuthorizationNumber,
        long caeRangeFrom,
        long caeRangeTo,
        DateOnly caeValidFrom,
        DateOnly caeValidTo,
        DateOnly fiscalDate,
        string? locationId,
        string? terminalId,
        ReceiverIdentificationRequirement? receiverIdentification,
        string formatVersion,
        string confirmationFingerprint,
        string settlementFingerprint,
        string currencyCode,
        decimal netAmount,
        decimal vatAmount,
        decimal totalAmount,
        FiscalDocumentStatus status,
        DateTimeOffset identityCreatedAtUtc)
    {
        if (id == Guid.Empty)
            throw Rule("fiscal.document_id_required", "Fiscal document id is required.");
        if (fiscalizationRequestId == Guid.Empty)
            throw Rule("fiscal.fiscalization_request_id_required", "Fiscalization request id is required.");
        if (saleId == Guid.Empty)
            throw Rule("fiscal.sale_id_required", "Source sale id is required.");
        if (fiscalNumberReservationId == Guid.Empty)
            throw Rule("fiscal.number_reservation_id_required", "Fiscal number reservation id is required.");
        if (caeAuthorizationId == Guid.Empty)
            throw Rule("fiscal.cae_authorization_id_required", "CAE authorization id is required.");
        if (!Enum.IsDefined(cfeType))
            throw Rule("fiscal.cfe_type_invalid", "Fiscal document requires a supported CFE type.");
        if (!Enum.IsDefined(status))
            throw Rule("fiscal.document_status_invalid", "Fiscal document status is invalid.");
        if (number <= 0)
            throw Rule("fiscal.number_invalid", "Fiscal document number must be positive.");
        if (caeRangeFrom <= 0 || caeRangeTo < caeRangeFrom || number < caeRangeFrom || number > caeRangeTo)
            throw Rule("fiscal.cae_range_invalid", "Fiscal number must belong to the snapshotted CAE range.");
        if (caeValidTo < caeValidFrom || fiscalDate < caeValidFrom || fiscalDate > caeValidTo)
            throw Rule("fiscal.cae_validity_invalid", "Fiscal date must belong to the snapshotted CAE validity window.");
        if (netAmount < 0m || vatAmount < 0m || totalAmount < 0m)
            throw Rule("fiscal.amount_invalid", "Fiscal snapshot amounts cannot be negative.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "fiscal.organization_required");
        FiscalizationRequestId = fiscalizationRequestId;
        SaleId = saleId;
        FiscalNumberReservationId = fiscalNumberReservationId;
        CaeAuthorizationId = caeAuthorizationId;
        CaeAllocationId = caeAllocationId;
        CfeType = cfeType;
        Series = Required(series, 20, "fiscal.series_required").ToUpperInvariant();
        Number = number;
        CaeAuthorizationNumber = Required(caeAuthorizationNumber, 80, "fiscal.cae_number_required");
        CaeRangeFrom = caeRangeFrom;
        CaeRangeTo = caeRangeTo;
        CaeValidFrom = caeValidFrom;
        CaeValidTo = caeValidTo;
        FiscalDate = fiscalDate;
        LocationId = Optional(locationId, 200);
        TerminalId = Optional(terminalId, 200);
        ReceiverIdentification = receiverIdentification;
        FormatVersion = Required(formatVersion, 40, "fiscal.format_version_required");
        ConfirmationFingerprint = Fingerprint(confirmationFingerprint, "fiscal.confirmation_fingerprint_invalid");
        SettlementFingerprint = Fingerprint(settlementFingerprint, "fiscal.settlement_fingerprint_invalid");
        CurrencyCode = Currency(currencyCode);
        NetAmount = netAmount;
        VatAmount = vatAmount;
        TotalAmount = totalAmount;
        Status = status;
        IdentityCreatedAtUtc = identityCreatedAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid FiscalizationRequestId { get; }
    public Guid SaleId { get; }
    public Guid FiscalNumberReservationId { get; }
    public Guid CaeAuthorizationId { get; }
    public Guid? CaeAllocationId { get; }
    public CfeFamily CfeType { get; }
    public string Series { get; }
    public long Number { get; }
    public string CaeAuthorizationNumber { get; }
    public long CaeRangeFrom { get; }
    public long CaeRangeTo { get; }
    public DateOnly CaeValidFrom { get; }
    public DateOnly CaeValidTo { get; }
    public DateOnly FiscalDate { get; }
    public string? LocationId { get; }
    public string? TerminalId { get; }
    public ReceiverIdentificationRequirement? ReceiverIdentification { get; }
    public string FormatVersion { get; }
    public string ConfirmationFingerprint { get; }
    public string SettlementFingerprint { get; }
    public string CurrencyCode { get; }
    public decimal NetAmount { get; }
    public decimal VatAmount { get; }
    public decimal TotalAmount { get; }
    public FiscalDocumentStatus Status { get; }
    public DateTimeOffset IdentityCreatedAtUtc { get; }

    public static FiscalDocument CreateIdentity(
        Guid id,
        string organizationId,
        Guid fiscalizationRequestId,
        Guid saleId,
        Guid fiscalNumberReservationId,
        Guid caeAuthorizationId,
        Guid? caeAllocationId,
        CfeFamily cfeType,
        string series,
        long number,
        string caeAuthorizationNumber,
        long caeRangeFrom,
        long caeRangeTo,
        DateOnly caeValidFrom,
        DateOnly caeValidTo,
        DateOnly fiscalDate,
        string? locationId,
        string? terminalId,
        ReceiverIdentificationRequirement? receiverIdentification,
        string formatVersion,
        string confirmationFingerprint,
        string settlementFingerprint,
        string currencyCode,
        decimal netAmount,
        decimal vatAmount,
        decimal totalAmount,
        DateTimeOffset identityCreatedAtUtc) =>
        new(
            id, organizationId, fiscalizationRequestId, saleId, fiscalNumberReservationId,
            caeAuthorizationId, caeAllocationId, cfeType, series, number, caeAuthorizationNumber,
            caeRangeFrom, caeRangeTo, caeValidFrom, caeValidTo, fiscalDate, locationId, terminalId,
            receiverIdentification, formatVersion, confirmationFingerprint, settlementFingerprint,
            currencyCode, netAmount, vatAmount, totalAmount, FiscalDocumentStatus.IdentityCreated,
            identityCreatedAtUtc);

    public static FiscalDocument Rehydrate(
        Guid id,
        string organizationId,
        Guid fiscalizationRequestId,
        Guid saleId,
        Guid fiscalNumberReservationId,
        Guid caeAuthorizationId,
        Guid? caeAllocationId,
        CfeFamily cfeType,
        string series,
        long number,
        string caeAuthorizationNumber,
        long caeRangeFrom,
        long caeRangeTo,
        DateOnly caeValidFrom,
        DateOnly caeValidTo,
        DateOnly fiscalDate,
        string? locationId,
        string? terminalId,
        ReceiverIdentificationRequirement? receiverIdentification,
        string formatVersion,
        string confirmationFingerprint,
        string settlementFingerprint,
        string currencyCode,
        decimal netAmount,
        decimal vatAmount,
        decimal totalAmount,
        FiscalDocumentStatus status,
        DateTimeOffset identityCreatedAtUtc) =>
        new(
            id, organizationId, fiscalizationRequestId, saleId, fiscalNumberReservationId,
            caeAuthorizationId, caeAllocationId, cfeType, series, number, caeAuthorizationNumber,
            caeRangeFrom, caeRangeTo, caeValidFrom, caeValidTo, fiscalDate, locationId, terminalId,
            receiverIdentification, formatVersion, confirmationFingerprint, settlementFingerprint,
            currencyCode, netAmount, vatAmount, totalAmount, status, identityCreatedAtUtc);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required fiscal-document value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Fiscal-document value cannot exceed {max} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule("fiscal.value_too_long", $"Fiscal-document value cannot exceed {max} characters.");
        return normalized;
    }

    private static string Currency(string value)
    {
        var normalized = Required(value, 3, "fiscal.currency_required").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("fiscal.currency_invalid", "Fiscal currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static string Fingerprint(string value, string code)
    {
        var normalized = Required(value, 64, code).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Rule(code, "Fiscal evidence fingerprint must be a SHA-256 hexadecimal value.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
