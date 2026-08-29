using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.Sales;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListSalesUseCase _list;
    private readonly GetSaleUseCase _get;
    private readonly CreateSaleUseCase _create;
    private readonly UpdateSaleDraftUseCase _update;
    private readonly ValidateSaleUseCase _validate;
    private readonly GetSaleFiscalPreviewUseCase _preview;

    public SalesController(
        V1OrganizationContextResolver organization,
        ListSalesUseCase list,
        GetSaleUseCase get,
        CreateSaleUseCase create,
        UpdateSaleDraftUseCase update,
        ValidateSaleUseCase validate,
        GetSaleFiscalPreviewUseCase preview)
    {
        _organization = organization;
        _list = list;
        _get = get;
        _create = create;
        _update = update;
        _validate = validate;
        _preview = preview;
    }

    [HttpGet]
    [RequirePermission(Permissions.SalesRead)]
    public async Task<ActionResult<PageResponse<SaleDto>>> List(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? customerPartyId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _list.ExecuteAsync(
            new SaleSearchRequest(
                organizationId,
                from,
                to,
                customerPartyId,
                string.IsNullOrWhiteSpace(status) ? null : ParseStatus(status),
                page,
                pageSize),
            cancellationToken);

        return Ok(new PageResponse<SaleDto>(
            result.Items.Select(MapSale).ToArray(),
            result.Page,
            result.PageSize,
            result.Total));
    }

    [HttpPost]
    [RequirePermission(Permissions.SalesCreate)]
    public async Task<ActionResult<SaleDto>> Create(
        [FromBody] SaleCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _create.ExecuteAsync(
            new CreateSaleCommand(
                organizationId,
                request.LocationId,
                request.TerminalId,
                ParseOptionalGuid(request.CustomerPartyId, "sales.invalid_customer_id"),
                ParseIntent(request.Intent),
                request.CurrencyCode,
                request.EffectiveOn,
                request.DeliveryCountry,
                GoodsExportConfirmed: false,
                request.Lines.Select(MapLine).ToArray(),
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        var resource = MapSale(await _get.ExecuteAsync(organizationId, result.SaleId, cancellationToken));
        return CreatedAtAction(nameof(Get), new { saleId = result.SaleId }, resource);
    }

    [HttpGet("{saleId:guid}")]
    [RequirePermission(Permissions.SalesRead)]
    public async Task<ActionResult<SaleDto>> Get(
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(MapSale(await _get.ExecuteAsync(organizationId, saleId, cancellationToken)));
    }

    [HttpPatch("{saleId:guid}")]
    [RequirePermission(Permissions.SalesCreate)]
    public async Task<ActionResult<SaleDto>> UpdateDraft(
        Guid saleId,
        [FromBody] SaleDraftUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _update.ExecuteAsync(
            new UpdateSaleDraftCommand(
                organizationId,
                saleId,
                request.ExpectedVersion,
                ParseOptionalGuid(request.CustomerPartyId, "sales.invalid_customer_id"),
                ParseIntent(request.Intent),
                request.CurrencyCode,
                request.EffectiveOn,
                request.DeliveryCountry,
                GoodsExportConfirmed: false,
                request.Lines.Select(MapLine).ToArray(),
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(MapSale(await _get.ExecuteAsync(organizationId, saleId, cancellationToken)));
    }

    [HttpPost("{saleId:guid}/validate")]
    [RequirePermission(Permissions.SalesCreate)]
    public async Task<ActionResult<SaleValidationDto>> Validate(
        Guid saleId,
        [FromBody] SaleValidateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _validate.ExecuteAsync(
            new ValidateSaleCommand(
                organizationId,
                saleId,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(new SaleValidationDto(
            result.Valid,
            result.Replayed,
            MapSale(result.Sale),
            MapPreview(result.Preview)));
    }

    [HttpGet("{saleId:guid}/fiscal-preview")]
    [RequirePermission(Permissions.SalesRead)]
    public async Task<ActionResult<SaleFiscalPreviewDto>> FiscalPreview(
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(MapPreview(await _preview.ExecuteAsync(organizationId, saleId, cancellationToken)));
    }

    private static SaleLineInput MapLine(SaleLineRequest line) => new(
        ParseGuid(line.ItemId, "sales.invalid_item_id"),
        line.Quantity,
        line.UnitPrice,
        ParseServiceScope(line.ServicePerformanceScope),
        line.ServiceUseCountry,
        ParseExportServiceKind(line.ExportServiceKind),
        // Regulatory qualification facts are not accepted as authoritative booleans
        // from the public client in this slice. They remain Unknown until evidence is verified.
        SaleRegulatoryFactStatus.Unknown,
        SaleRegulatoryFactStatus.Unknown,
        SaleRegulatoryFactStatus.Unknown,
        SaleRegulatoryFactStatus.Unknown,
        SaleRegulatoryFactStatus.Unknown);

    private static SaleCommercialIntent ParseIntent(string value) =>
        V1ApiEnum.Parse<SaleCommercialIntent>(
            value,
            "sales.invalid_intent",
            "Sale intent must be CONSUMER_FINAL, TAXPAYER_INVOICE or EXPORT.");

    private static SaleStatus ParseStatus(string value) =>
        V1ApiEnum.Parse<SaleStatus>(
            value,
            "sales.invalid_status",
            "Sale status is not supported by this API slice.");

    private static SaleServicePerformanceScope ParseServiceScope(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SaleServicePerformanceScope.UnknownOrMixed;

        return V1ApiEnum.Parse<SaleServicePerformanceScope>(
            value,
            "sales.invalid_service_performance_scope",
            "Service performance scope is invalid.");
    }

    private static SaleExportServiceKind ParseExportServiceKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SaleExportServiceKind.None;

        return V1ApiEnum.Parse<SaleExportServiceKind>(
            value,
            "sales.invalid_export_service_kind",
            "Export-service kind is invalid.");
    }

    private static Guid ParseGuid(string value, string code) =>
        Guid.TryParse(value, out var parsed)
            ? parsed
            : throw ValidationProblem(code, "The supplied identifier is invalid.");

    private static Guid? ParseOptionalGuid(string? value, string code) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseGuid(value, code);

    private static SaleDto MapSale(SaleView sale) => new(
        sale.Id.ToString(),
        sale.Version,
        ToApiEnum(sale.Status),
        sale.OrganizationId,
        sale.LocationId,
        sale.TerminalId,
        sale.CustomerPartyId?.ToString(),
        ToApiEnum(sale.Intent),
        sale.CurrencyCode,
        sale.EffectiveOn,
        sale.DeliveryCountry,
        sale.NetAmount,
        sale.ValidationFingerprint,
        sale.ValidatedAtUtc,
        sale.Lines.Select(line => new SaleLineDto(
            line.Id.ToString(),
            line.ItemId.ToString(),
            line.ItemCode,
            line.ItemName,
            ToApiEnum(line.Kind),
            line.Quantity,
            line.UnitPrice,
            line.NetAmount,
            line.TaxProfileId?.ToString())).ToArray());

    private static SaleFiscalPreviewDto MapPreview(SaleFiscalPreviewView preview) => new(
        preview.SaleId.ToString(),
        preview.SaleVersion,
        preview.CurrencyCode,
        preview.NetAmount,
        preview.TaxAmount,
        preview.TotalAmount,
        preview.Lines.Select(line => new SaleFiscalPreviewLineDto(
            line.LineId.ToString(),
            line.ItemCode,
            line.NetAmount,
            ToApiEnum(line.TaxTreatmentStatus),
            ToApiEnum(line.TaxTreatment),
            line.TreatmentCode,
            ToApiEnum(line.TaxRateStatus),
            ToApiEnum(line.VatLiability),
            ToApiEnum(line.VatRateKind),
            line.AppliedRatePercent,
            line.TaxAmount,
            line.Reasons,
            line.MissingFacts,
            line.RuleEvidence.Select(MapRule).ToArray())).ToArray(),
        ToApiEnum(preview.OverallTaxTreatment.Status),
        ToApiEnum(preview.OverallTaxTreatment.Classification),
        preview.OverallTaxTreatment.TreatmentCode,
        new CfeDecisionDto(
            ToApiEnum(preview.CfeEligibility.Status),
            ToApiEnum(preview.CfeSelection.Status),
            preview.CfeSelection.SelectedFamily.HasValue ? (int)preview.CfeSelection.SelectedFamily.Value : null,
            preview.CfeSelection.SelectedFamily.HasValue ? ToApiEnum(preview.CfeSelection.SelectedFamily.Value) : null,
            preview.CfeEligibility.Candidates.Select(candidate => new CfeCandidateDto(
                (int)candidate.Family,
                ToApiEnum(candidate.Family),
                ToApiEnum(candidate.ReceiverIdentification),
                candidate.Reasons)).ToArray(),
            preview.CfeEligibility.Reasons.Concat(preview.CfeSelection.Reasons).Distinct(StringComparer.Ordinal).ToArray(),
            preview.CfeEligibility.MissingFacts.Concat(preview.CfeSelection.MissingFacts).Distinct(StringComparer.Ordinal).ToArray(),
            preview.CfeEligibility.FormatVersion),
        preview.ReadyForConfirmation,
        preview.ValidationFingerprint,
        preview.Findings,
        "PREVIEW_ONLY_NOT_FINAL_CFE_ARITHMETIC");

    private static FiscalRuleReferenceDto MapRule(RegulatoryRuleEvidence rule) => new(
        rule.RuleId,
        rule.SourceName,
        rule.SourceReference,
        rule.SourceVersion,
        rule.EffectiveFrom,
        rule.EffectiveTo,
        rule.Clause);

    private static string ToApiEnum<T>(T value) where T : struct, Enum =>
        ToSnake(value.ToString()).ToUpperInvariant();

    private static string ToSnake(string value)
    {
        var chars = new List<char>(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current) && !char.IsUpper(value[index - 1]))
                chars.Add('_');
            chars.Add(current);
        }
        return new string(chars.ToArray());
    }

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
            Response.Headers["Idempotent-Replayed"] = "true";
    }

    private static ApplicationProblemException ValidationProblem(string code, string detail) =>
        new(ApplicationProblemKind.Validation, code, detail);
}
