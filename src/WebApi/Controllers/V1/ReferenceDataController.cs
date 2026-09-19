using EFactura.Application.Common.Security;
using EFactura.Application.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;

namespace WebApi.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/reference-data")]
public sealed class ReferenceDataController : ControllerBase
{
    private readonly ListCountriesUseCase _listCountries;
    private readonly ListUruguayDepartmentsUseCase _listUruguayDepartments;
    private readonly ListFiscalIdentityTypesUseCase _listFiscalIdentityTypes;
    private readonly ListCurrenciesUseCase _listCurrencies;
    private readonly ListFiscalDocumentTypesUseCase _listFiscalDocumentTypes;
    private readonly ListInvoiceIndicatorsUseCase _listInvoiceIndicators;

    public ReferenceDataController(
        ListCountriesUseCase listCountries,
        ListUruguayDepartmentsUseCase listUruguayDepartments,
        ListFiscalIdentityTypesUseCase listFiscalIdentityTypes,
        ListCurrenciesUseCase listCurrencies,
        ListFiscalDocumentTypesUseCase listFiscalDocumentTypes,
        ListInvoiceIndicatorsUseCase listInvoiceIndicators)
    {
        _listCountries = listCountries;
        _listUruguayDepartments = listUruguayDepartments;
        _listFiscalIdentityTypes = listFiscalIdentityTypes;
        _listCurrencies = listCurrencies;
        _listFiscalDocumentTypes = listFiscalDocumentTypes;
        _listInvoiceIndicators = listInvoiceIndicators;
    }

    [HttpGet("countries", Name = "listCountries")]
    [ProducesResponseType(typeof(ReferenceDataCollectionDto<CountryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReferenceDataCollectionDto<CountryDto>>> ListCountries(
        CancellationToken cancellationToken)
    {
        var result = await _listCountries.ExecuteAsync(cancellationToken);
        return Ok(new ReferenceDataCollectionDto<CountryDto>(
            result.SourceName,
            result.SourceVersion,
            result.Items.Select(item => new CountryDto(item.Alpha2Code, item.Name)).ToArray()));
    }

    [HttpGet("uruguay-departments", Name = "listUruguayDepartments")]
    [ProducesResponseType(typeof(ReferenceDataCollectionDto<UruguayDepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReferenceDataCollectionDto<UruguayDepartmentDto>>> ListUruguayDepartments(
        CancellationToken cancellationToken)
    {
        var result = await _listUruguayDepartments.ExecuteAsync(cancellationToken);
        return Ok(new ReferenceDataCollectionDto<UruguayDepartmentDto>(
            result.SourceName,
            result.SourceVersion,
            result.Items.Select(item => new UruguayDepartmentDto(item.Name)).ToArray()));
    }

    [HttpGet("fiscal-identity-types", Name = "listFiscalIdentityTypes")]
    [ProducesResponseType(typeof(ReferenceDataCollectionDto<FiscalIdentityTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReferenceDataCollectionDto<FiscalIdentityTypeDto>>> ListFiscalIdentityTypes(
        CancellationToken cancellationToken)
    {
        var result = await _listFiscalIdentityTypes.ExecuteAsync(cancellationToken);
        return Ok(new ReferenceDataCollectionDto<FiscalIdentityTypeDto>(
            result.SourceName,
            result.SourceVersion,
            result.Items.Select(item => new FiscalIdentityTypeDto(
                item.Code,
                item.Name,
                item.CountryRule,
                item.AllowedIssuingCountryCodes,
                item.AllowsOtherIsoCountry,
                item.AllowsSpecialCountryFallback)).ToArray()));
    }

    [HttpGet("currencies", Name = "listCurrencies")]
    [ProducesResponseType(typeof(ReferenceDataCollectionDto<CurrencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReferenceDataCollectionDto<CurrencyDto>>> ListCurrencies(
        CancellationToken cancellationToken)
    {
        var result = await _listCurrencies.ExecuteAsync(cancellationToken);
        return Ok(new ReferenceDataCollectionDto<CurrencyDto>(
            result.SourceName,
            result.SourceVersion,
            result.Items.Select(item => new CurrencyDto(item.AlphabeticCode, item.Name)).ToArray()));
    }

    [HttpGet("fiscal-document-types", Name = "listFiscalDocumentTypes")]
    [RequirePermission(Permissions.FiscalRead)]
    [ProducesResponseType(typeof(ReferenceDataCollectionDto<FiscalDocumentTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReferenceDataCollectionDto<FiscalDocumentTypeDto>>> ListFiscalDocumentTypes(
        CancellationToken cancellationToken)
    {
        var result = await _listFiscalDocumentTypes.ExecuteAsync(cancellationToken);
        return Ok(new ReferenceDataCollectionDto<FiscalDocumentTypeDto>(
            result.SourceName,
            result.SourceVersion,
            result.Items.Select(item => new FiscalDocumentTypeDto(
                item.Code,
                item.Name,
                item.Family,
                item.CorrectionKind,
                item.ContingencyCode,
                item.RequiresApplicabilityValidation)).ToArray()));
    }

    [HttpGet("invoice-indicators", Name = "listInvoiceIndicators")]
    [RequirePermission(Permissions.FiscalRead)]
    [ProducesResponseType(typeof(ReferenceDataCollectionDto<InvoiceIndicatorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReferenceDataCollectionDto<InvoiceIndicatorDto>>> ListInvoiceIndicators(
        CancellationToken cancellationToken)
    {
        var result = await _listInvoiceIndicators.ExecuteAsync(cancellationToken);
        return Ok(new ReferenceDataCollectionDto<InvoiceIndicatorDto>(
            result.SourceName,
            result.SourceVersion,
            result.Items.Select(item => new InvoiceIndicatorDto(
                item.Code,
                item.Name,
                item.TaxTreatment)).ToArray()));
    }
}
