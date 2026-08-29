using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Results;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Sales;

public sealed record SaleLineInput(
    Guid ItemId,
    decimal Quantity,
    decimal UnitPrice,
    SaleServicePerformanceScope ServicePerformanceScope = SaleServicePerformanceScope.UnknownOrMixed,
    string? ServiceUseCountry = null,
    SaleExportServiceKind ExportServiceKind = SaleExportServiceKind.None,
    SaleRegulatoryFactStatus RecipientIsPersonAbroad = SaleRegulatoryFactStatus.Unknown,
    SaleRegulatoryFactStatus ExclusiveUseAbroad = SaleRegulatoryFactStatus.Unknown,
    SaleRegulatoryFactStatus ForeignEconomicRelation = SaleRegulatoryFactStatus.Unknown,
    SaleRegulatoryFactStatus RecipientInstalledInFreeZone = SaleRegulatoryFactStatus.Unknown,
    SaleRegulatoryFactStatus ProviderFromNonFreeNationalTerritory = SaleRegulatoryFactStatus.Unknown);

public sealed record CreateSaleCommand(
    string OrganizationId,
    string? LocationId,
    string? TerminalId,
    Guid? CustomerPartyId,
    SaleCommercialIntent Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    string? DeliveryCountry,
    bool GoodsExportConfirmed,
    IReadOnlyCollection<SaleLineInput> Lines,
    string IdempotencyKey,
    string RequestHash);

public sealed record UpdateSaleDraftCommand(
    string OrganizationId,
    Guid SaleId,
    long ExpectedVersion,
    Guid? CustomerPartyId,
    SaleCommercialIntent Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    string? DeliveryCountry,
    bool GoodsExportConfirmed,
    IReadOnlyCollection<SaleLineInput> Lines,
    string IdempotencyKey,
    string RequestHash);

public sealed record ValidateSaleCommand(
    string OrganizationId,
    Guid SaleId,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record SaleSearchRequest(
    string OrganizationId,
    DateOnly? From,
    DateOnly? To,
    Guid? CustomerPartyId,
    SaleStatus? Status,
    int Page = 1,
    int PageSize = 50);

public sealed record SaleLineView(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    SaleLineKind Kind,
    decimal Quantity,
    decimal UnitPrice,
    decimal NetAmount,
    Guid? TaxProfileId);

public sealed record SaleView(
    Guid Id,
    long Version,
    SaleStatus Status,
    string OrganizationId,
    string? LocationId,
    string? TerminalId,
    Guid? CustomerPartyId,
    SaleCommercialIntent Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    string? DeliveryCountry,
    bool GoodsExportConfirmed,
    decimal NetAmount,
    string? ValidationFingerprint,
    DateTimeOffset? ValidatedAtUtc,
    IReadOnlyCollection<SaleLineView> Lines)
{
    public static SaleView FromDomain(Sale sale) => new(
        sale.Id,
        sale.Version,
        sale.Status,
        sale.OrganizationId,
        sale.LocationId,
        sale.TerminalId,
        sale.CustomerPartyId,
        sale.Intent,
        sale.CurrencyCode,
        sale.EffectiveOn,
        sale.DeliveryCountry,
        sale.GoodsExportConfirmed,
        sale.NetAmount,
        sale.ValidationFingerprint,
        sale.ValidatedAtUtc,
        sale.Lines.Select(line => new SaleLineView(
            line.Id,
            line.ItemId,
            line.ItemCode,
            line.ItemName,
            line.Kind,
            line.Quantity,
            line.UnitPrice,
            line.NetAmount,
            line.TaxProfileId)).ToArray());
}

public sealed record SaleFiscalPreviewLineView(
    Guid LineId,
    string ItemCode,
    decimal NetAmount,
    TaxDecisionStatus TaxTreatmentStatus,
    TaxTreatmentClassification TaxTreatment,
    string TreatmentCode,
    TaxRateResolutionStatus TaxRateStatus,
    VatLiabilityKind VatLiability,
    VatRateKind VatRateKind,
    decimal? AppliedRatePercent,
    decimal? TaxAmount,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> MissingFacts,
    IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence);

public sealed record SaleFiscalPreviewView(
    Guid SaleId,
    long SaleVersion,
    string CurrencyCode,
    decimal NetAmount,
    decimal? TaxAmount,
    decimal? TotalAmount,
    IReadOnlyCollection<SaleFiscalPreviewLineView> Lines,
    TaxTreatmentDecision OverallTaxTreatment,
    CfeEligibilityResult CfeEligibility,
    CfeSelectionResult CfeSelection,
    bool ReadyForConfirmation,
    string ValidationFingerprint,
    IReadOnlyCollection<string> Findings);

public sealed record SaleMutationResult(Guid SaleId, long Version, bool Replayed);
public sealed record SaleValidationResult(bool Valid, SaleView Sale, SaleFiscalPreviewView Preview, bool Replayed);

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<Sale?> GetAsync(string organizationId, Guid saleId, CancellationToken cancellationToken = default);
    Task<PageResult<Sale>> SearchAsync(SaleSearchRequest request, CancellationToken cancellationToken = default);
    Task SaveAsync(Sale sale, CancellationToken cancellationToken = default);
}

public interface IUiAmountConverter
{
    Task<decimal?> TryConvertToUiAsync(
        string currencyCode,
        decimal amount,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default);
}

public sealed class Release1UiAmountConverter : IUiAmountConverter
{
    public Task<decimal?> TryConvertToUiAsync(
        string currencyCode,
        decimal amount,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Release 1 deliberately refuses to invent an effective-date UI quote.
        // A deployment adapter must later supply authoritative conversion for UYU/USD/etc.
        return Task.FromResult<decimal?>(
            string.Equals(currencyCode, "UI", StringComparison.OrdinalIgnoreCase) ? amount : null);
    }
}

public sealed record SaleDraftCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SaleId,
    string OrganizationId) : IIntegrationEvent;

public sealed record SaleDraftUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SaleId,
    string OrganizationId) : IIntegrationEvent;

public sealed record SaleValidatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SaleId,
    string OrganizationId,
    string ValidationFingerprint) : IIntegrationEvent;
