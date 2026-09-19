using EFactura.Application.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;

namespace WebApi.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/reference-data")]
public sealed class ReferenceDataController : ControllerBase
{
    private readonly ListUruguayDepartmentsUseCase _listUruguayDepartments;
    private readonly ListFiscalIdentityTypesUseCase _listFiscalIdentityTypes;

    public ReferenceDataController(
        ListUruguayDepartmentsUseCase listUruguayDepartments,
        ListFiscalIdentityTypesUseCase listFiscalIdentityTypes)
    {
        _listUruguayDepartments = listUruguayDepartments;
        _listFiscalIdentityTypes = listFiscalIdentityTypes;
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
}
