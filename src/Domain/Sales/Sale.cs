using EFactura.Domain.Common;

namespace EFactura.Domain.Sales;

public enum SaleStatus
{
    Draft = 1,
    Validated = 2
}

public enum SaleCommercialIntent
{
    ConsumerFinal = 1,
    TaxpayerInvoice = 2,
    Export = 3
}

public enum SaleLineKind
{
    Product = 1,
    Service = 2
}

public enum SaleServicePerformanceScope
{
    UnknownOrMixed = 0,
    EntirelyInUruguay = 1,
    EntirelyOutsideUruguay = 2
}

public enum SaleExportServiceKind
{
    None = 0,
    AdvisoryOrTechnical = 1,
    CustomSoftware = 2,
    SoftwareLicense = 3,
    SoftwareRightsAssignment = 4
}

public enum SaleRegulatoryFactStatus
{
    Unknown = 0,
    Confirmed = 1,
    NotMet = 2
}

public sealed class SaleLine
{
    private SaleLine(
        Guid id,
        Guid itemId,
        string itemCode,
        string itemName,
        SaleLineKind kind,
        decimal quantity,
        decimal unitPrice,
        Guid? taxProfileId,
        SaleServicePerformanceScope servicePerformanceScope,
        string? serviceUseCountry,
        SaleExportServiceKind exportServiceKind,
        SaleRegulatoryFactStatus recipientIsPersonAbroad,
        SaleRegulatoryFactStatus exclusiveUseAbroad,
        SaleRegulatoryFactStatus foreignEconomicRelation,
        SaleRegulatoryFactStatus recipientInstalledInFreeZone,
        SaleRegulatoryFactStatus providerFromNonFreeNationalTerritory)
    {
        Id = id;
        ItemId = itemId;
        ItemCode = Required(itemCode, 80, "sales.line.item_code_required");
        ItemName = Required(itemName, 250, "sales.line.item_name_required");
        Kind = kind;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxProfileId = taxProfileId;
        ServicePerformanceScope = servicePerformanceScope;
        ServiceUseCountry = Country(serviceUseCountry, "sales.line.invalid_service_use_country");
        ExportServiceKind = exportServiceKind;
        RecipientIsPersonAbroad = recipientIsPersonAbroad;
        ExclusiveUseAbroad = exclusiveUseAbroad;
        ForeignEconomicRelation = foreignEconomicRelation;
        RecipientInstalledInFreeZone = recipientInstalledInFreeZone;
        ProviderFromNonFreeNationalTerritory = providerFromNonFreeNationalTerritory;
        Validate();
    }

    public Guid Id { get; }
    public Guid ItemId { get; }
    public string ItemCode { get; }
    public string ItemName { get; }
    public SaleLineKind Kind { get; }
    public decimal Quantity { get; }
    public decimal UnitPrice { get; }
    public Guid? TaxProfileId { get; }
    public SaleServicePerformanceScope ServicePerformanceScope { get; }
    public string? ServiceUseCountry { get; }
    public SaleExportServiceKind ExportServiceKind { get; }
    public SaleRegulatoryFactStatus RecipientIsPersonAbroad { get; }
    public SaleRegulatoryFactStatus ExclusiveUseAbroad { get; }
    public SaleRegulatoryFactStatus ForeignEconomicRelation { get; }
    public SaleRegulatoryFactStatus RecipientInstalledInFreeZone { get; }
    public SaleRegulatoryFactStatus ProviderFromNonFreeNationalTerritory { get; }
    public decimal NetAmount => Quantity * UnitPrice;

    public static SaleLine Create(
        Guid id,
        Guid itemId,
        string itemCode,
        string itemName,
        SaleLineKind kind,
        decimal quantity,
        decimal unitPrice,
        Guid? taxProfileId,
        SaleServicePerformanceScope servicePerformanceScope = SaleServicePerformanceScope.UnknownOrMixed,
        string? serviceUseCountry = null,
        SaleExportServiceKind exportServiceKind = SaleExportServiceKind.None,
        SaleRegulatoryFactStatus recipientIsPersonAbroad = SaleRegulatoryFactStatus.Unknown,
        SaleRegulatoryFactStatus exclusiveUseAbroad = SaleRegulatoryFactStatus.Unknown,
        SaleRegulatoryFactStatus foreignEconomicRelation = SaleRegulatoryFactStatus.Unknown,
        SaleRegulatoryFactStatus recipientInstalledInFreeZone = SaleRegulatoryFactStatus.Unknown,
        SaleRegulatoryFactStatus providerFromNonFreeNationalTerritory = SaleRegulatoryFactStatus.Unknown) =>
        new(id, itemId, itemCode, itemName, kind, quantity, unitPrice, taxProfileId,
            servicePerformanceScope, serviceUseCountry, exportServiceKind, recipientIsPersonAbroad,
            exclusiveUseAbroad, foreignEconomicRelation, recipientInstalledInFreeZone,
            providerFromNonFreeNationalTerritory);

    public static SaleLine Rehydrate(
        Guid id,
        Guid itemId,
        string itemCode,
        string itemName,
        SaleLineKind kind,
        decimal quantity,
        decimal unitPrice,
        Guid? taxProfileId,
        SaleServicePerformanceScope servicePerformanceScope,
        string? serviceUseCountry,
        SaleExportServiceKind exportServiceKind,
        SaleRegulatoryFactStatus recipientIsPersonAbroad,
        SaleRegulatoryFactStatus exclusiveUseAbroad,
        SaleRegulatoryFactStatus foreignEconomicRelation,
        SaleRegulatoryFactStatus recipientInstalledInFreeZone,
        SaleRegulatoryFactStatus providerFromNonFreeNationalTerritory) =>
        new(id, itemId, itemCode, itemName, kind, quantity, unitPrice, taxProfileId,
            servicePerformanceScope, serviceUseCountry, exportServiceKind, recipientIsPersonAbroad,
            exclusiveUseAbroad, foreignEconomicRelation, recipientInstalledInFreeZone,
            providerFromNonFreeNationalTerritory);

    private void Validate()
    {
        if (Quantity <= 0m)
        {
            throw new DomainRuleException("sales.line.quantity_invalid", "Sale line quantity must be greater than zero.");
        }

        if (UnitPrice < 0m)
        {
            throw new DomainRuleException("sales.line.unit_price_invalid", "Sale line unit price cannot be negative.");
        }

        if (Kind == SaleLineKind.Product)
        {
            if (ServicePerformanceScope != SaleServicePerformanceScope.UnknownOrMixed
                || ServiceUseCountry is not null
                || ExportServiceKind != SaleExportServiceKind.None)
            {
                throw new DomainRuleException(
                    "sales.line.product_service_context_forbidden",
                    "Product lines cannot carry service-export regulatory context.");
            }
        }
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required sale-line value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > max)
        {
            throw new DomainRuleException(code, $"Sale-line value cannot exceed {max} characters.");
        }

        return normalized;
    }

    private static string? Country(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException(code, "Country must be an ISO alpha-2 code.");
        }

        return normalized;
    }
}

public sealed class Sale
{
    private readonly List<SaleLine> _lines;

    private Sale(
        Guid id,
        string organizationId,
        string? locationId,
        string? terminalId,
        Guid? customerPartyId,
        SaleCommercialIntent intent,
        string currencyCode,
        DateOnly effectiveOn,
        string? deliveryCountry,
        bool goodsExportConfirmed,
        IEnumerable<SaleLine> lines,
        SaleStatus status,
        string? validationFingerprint,
        DateTimeOffset? validatedAtUtc,
        long version)
    {
        Id = id;
        OrganizationId = Required(organizationId, 200, "sales.organization_required");
        LocationId = Optional(locationId, 200);
        TerminalId = Optional(terminalId, 200);
        CustomerPartyId = customerPartyId;
        Intent = intent;
        CurrencyCode = Required(currencyCode, 3, "sales.currency_required").ToUpperInvariant();
        EffectiveOn = effectiveOn;
        DeliveryCountry = Country(deliveryCountry, "sales.invalid_delivery_country");
        GoodsExportConfirmed = goodsExportConfirmed;
        _lines = new List<SaleLine>(lines);
        Status = status;
        ValidationFingerprint = validationFingerprint;
        ValidatedAtUtc = validatedAtUtc;
        Version = version;
        Validate();
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public string? LocationId { get; private set; }
    public string? TerminalId { get; private set; }
    public Guid? CustomerPartyId { get; private set; }
    public SaleCommercialIntent Intent { get; private set; }
    public string CurrencyCode { get; private set; }
    public DateOnly EffectiveOn { get; private set; }
    public string? DeliveryCountry { get; private set; }
    public bool GoodsExportConfirmed { get; private set; }
    public SaleStatus Status { get; private set; }
    public string? ValidationFingerprint { get; private set; }
    public DateTimeOffset? ValidatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<SaleLine> Lines => _lines;
    public decimal NetAmount => _lines.Sum(x => x.NetAmount);

    public static Sale Create(
        Guid id,
        string organizationId,
        string? locationId,
        string? terminalId,
        Guid? customerPartyId,
        SaleCommercialIntent intent,
        string currencyCode,
        DateOnly effectiveOn,
        string? deliveryCountry,
        bool goodsExportConfirmed,
        IEnumerable<SaleLine> lines) =>
        new(id, organizationId, locationId, terminalId, customerPartyId, intent, currencyCode,
            effectiveOn, deliveryCountry, goodsExportConfirmed, lines, SaleStatus.Draft, null, null, 1);

    public static Sale Rehydrate(
        Guid id,
        string organizationId,
        string? locationId,
        string? terminalId,
        Guid? customerPartyId,
        SaleCommercialIntent intent,
        string currencyCode,
        DateOnly effectiveOn,
        string? deliveryCountry,
        bool goodsExportConfirmed,
        IEnumerable<SaleLine> lines,
        SaleStatus status,
        string? validationFingerprint,
        DateTimeOffset? validatedAtUtc,
        long version) =>
        new(id, organizationId, locationId, terminalId, customerPartyId, intent, currencyCode,
            effectiveOn, deliveryCountry, goodsExportConfirmed, lines, status,
            validationFingerprint, validatedAtUtc, version);

    public void ReplaceDraft(
        Guid? customerPartyId,
        SaleCommercialIntent intent,
        string currencyCode,
        DateOnly effectiveOn,
        string? deliveryCountry,
        bool goodsExportConfirmed,
        IEnumerable<SaleLine> lines,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        CustomerPartyId = customerPartyId;
        Intent = intent;
        CurrencyCode = Required(currencyCode, 3, "sales.currency_required").ToUpperInvariant();
        EffectiveOn = effectiveOn;
        DeliveryCountry = Country(deliveryCountry, "sales.invalid_delivery_country");
        GoodsExportConfirmed = goodsExportConfirmed;
        _lines.Clear();
        _lines.AddRange(lines);
        Status = SaleStatus.Draft;
        ValidationFingerprint = null;
        ValidatedAtUtc = null;
        Version++;
        Validate();
    }

    public void MarkValidated(string fingerprint, DateTimeOffset validatedAtUtc, long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new DomainRuleException("sales.validation_fingerprint_required", "Validation fingerprint is required.");
        }

        Status = SaleStatus.Validated;
        ValidationFingerprint = fingerprint.Trim();
        ValidatedAtUtc = validatedAtUtc;
        Version++;
    }

    private void Validate()
    {
        if (CurrencyCode.Length != 3 || CurrencyCode.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException("sales.currency_invalid", "Currency code must use ISO alpha-3 form.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainRuleException("sales.lines_required", "A sale draft requires at least one line.");
        }

        if (_lines.Select(x => x.Id).Distinct().Count() != _lines.Count)
        {
            throw new DomainRuleException("sales.line_id_duplicate", "Sale line identifiers must be unique inside a sale.");
        }

        if (Intent == SaleCommercialIntent.Export && string.IsNullOrWhiteSpace(DeliveryCountry)
            && _lines.Any(x => x.Kind == SaleLineKind.Product))
        {
            throw new DomainRuleException("sales.export_delivery_country_required", "Goods export drafts require a delivery country.");
        }
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new DomainRuleException("concurrency.stale_version", "The sale changed before this operation was applied.");
        }
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required sale value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > max)
        {
            throw new DomainRuleException(code, $"Sale value cannot exceed {max} characters.");
        }

        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > max)
        {
            throw new DomainRuleException("sales.value_too_long", $"Sale value cannot exceed {max} characters.");
        }

        return normalized;
    }

    private static string? Country(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException(code, "Country must be an ISO alpha-2 code.");
        }

        return normalized;
    }
}
