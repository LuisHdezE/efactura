using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.Parties;
using EFactura.Domain.Parties;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/parties")]
public sealed class PartiesController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListPartiesUseCase _list;
    private readonly GetPartyUseCase _get;
    private readonly CreatePartyUseCase _create;
    private readonly UpdatePartyUseCase _update;
    private readonly AddPartyFiscalIdentityUseCase _addFiscalIdentity;
    private readonly UpdatePartyFiscalIdentityUseCase _updateFiscalIdentity;
    private readonly SetPartyRolesUseCase _setRoles;

    public PartiesController(
        V1OrganizationContextResolver organization,
        ListPartiesUseCase list,
        GetPartyUseCase get,
        CreatePartyUseCase create,
        UpdatePartyUseCase update,
        AddPartyFiscalIdentityUseCase addFiscalIdentity,
        UpdatePartyFiscalIdentityUseCase updateFiscalIdentity,
        SetPartyRolesUseCase setRoles)
    {
        _organization = organization;
        _list = list;
        _get = get;
        _create = create;
        _update = update;
        _addFiscalIdentity = addFiscalIdentity;
        _updateFiscalIdentity = updateFiscalIdentity;
        _setRoles = setRoles;
    }

    [HttpGet]
    [RequirePermission(Permissions.PartiesRead)]
    public async Task<ActionResult<PageResponse<PartyDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? active = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        PartyRole? parsedRole = string.IsNullOrWhiteSpace(role) ? null : ParseRole(role);
        var result = await _list.ExecuteAsync(
            new PartySearchRequest(organizationId, search, parsedRole, active, page, pageSize),
            cancellationToken);

        return Ok(new PageResponse<PartyDto>(
            result.Items.Select(Map).ToArray(),
            result.Page,
            result.PageSize,
            result.Total));
    }

    [HttpGet("{partyId:guid}")]
    [RequirePermission(Permissions.PartiesRead)]
    public async Task<ActionResult<PartyDto>> Get(Guid partyId, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, partyId, cancellationToken)));
    }

    [HttpPost]
    [RequirePermission(Permissions.PartiesManage)]
    public async Task<ActionResult<PartyDto>> Create(
        [FromBody] PartyCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var idempotencyKey = V1RequestContract.RequireIdempotencyKey(Request);
        var command = new CreatePartyCommand(
            organizationId,
            ParseKind(request.Kind),
            request.Name,
            request.ResidenceCountry,
            request.TaxResidenceCountry,
            ParseRoles(request.Roles),
            (request.FiscalIdentities ?? Array.Empty<PartyFiscalIdentityRequest>()).Select(Map).ToArray(),
            idempotencyKey,
            V1RequestContract.ComputeRequestHash(request));

        var result = await _create.ExecuteAsync(command, cancellationToken);
        SetReplayHeader(result.Replayed);
        var resource = Map(await _get.ExecuteAsync(organizationId, result.PartyId, cancellationToken));
        return CreatedAtAction(nameof(Get), new { partyId = result.PartyId }, resource);
    }

    [HttpPatch("{partyId:guid}")]
    [RequirePermission(Permissions.PartiesManage)]
    public async Task<ActionResult<PartyDto>> Update(
        Guid partyId,
        [FromBody] PartyUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _update.ExecuteAsync(
            new UpdatePartyCommand(
                organizationId,
                partyId,
                string.IsNullOrWhiteSpace(request.Kind) ? null : ParseKind(request.Kind),
                request.Name,
                request.ResidenceCountry,
                request.TaxResidenceCountry,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(await _get.ExecuteAsync(organizationId, partyId, cancellationToken)));
    }

    [HttpPost("{partyId:guid}/fiscal-identities")]
    [RequirePermission(Permissions.PartiesFiscalManage)]
    public async Task<ActionResult<PartyDto>> AddFiscalIdentity(
        Guid partyId,
        [FromBody] PartyFiscalIdentityCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _addFiscalIdentity.ExecuteAsync(
            new AddPartyFiscalIdentityCommand(
                organizationId,
                partyId,
                new PartyFiscalIdentityInput(
                    request.TypeCode,
                    request.Number,
                    request.IssuingCountry,
                    request.ValidFrom,
                    request.ValidTo),
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(await _get.ExecuteAsync(organizationId, partyId, cancellationToken)));
    }

    [HttpPut("{partyId:guid}/fiscal-identities/{identityId:guid}")]
    [RequirePermission(Permissions.PartiesFiscalManage)]
    public async Task<ActionResult<PartyDto>> UpdateFiscalIdentity(
        Guid partyId,
        Guid identityId,
        [FromBody] PartyFiscalIdentityUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _updateFiscalIdentity.ExecuteAsync(
            new UpdatePartyFiscalIdentityCommand(
                organizationId,
                partyId,
                identityId,
                new PartyFiscalIdentityInput(
                    request.TypeCode,
                    request.Number,
                    request.IssuingCountry,
                    request.ValidFrom,
                    request.ValidTo),
                request.Active,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(await _get.ExecuteAsync(organizationId, partyId, cancellationToken)));
    }

    [HttpPut("{partyId:guid}/roles")]
    [RequirePermission(Permissions.PartiesManage)]
    public async Task<ActionResult<PartyDto>> SetRoles(
        Guid partyId,
        [FromBody] PartyRolesUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _setRoles.ExecuteAsync(
            new SetPartyRolesCommand(
                organizationId,
                partyId,
                ParseRoles(request.Roles),
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(await _get.ExecuteAsync(organizationId, partyId, cancellationToken)));
    }

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
        {
            Response.Headers["Idempotent-Replayed"] = "true";
        }
    }

    private static PartyKind ParseKind(string value) =>
        Enum.TryParse<PartyKind>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw Validation("party.invalid_kind", "Party kind must be PERSON or ORGANIZATION.");

    private static PartyRole ParseRole(string value) =>
        Enum.TryParse<PartyRole>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw Validation("party.invalid_role", "Party role must be CUSTOMER or SUPPLIER.");

    private static IReadOnlyCollection<PartyRole> ParseRoles(IEnumerable<string> roles)
    {
        var parsed = roles?.Select(ParseRole).Distinct().ToArray() ?? Array.Empty<PartyRole>();
        if (parsed.Length == 0)
        {
            throw Validation("party.role_required", "At least one party role is required.");
        }

        return parsed;
    }

    private static PartyFiscalIdentityInput Map(PartyFiscalIdentityRequest request) =>
        new(request.TypeCode, request.Number, request.IssuingCountry, request.ValidFrom, request.ValidTo);

    private static PartyDto Map(PartyView party) =>
        new(
            party.Id.ToString(),
            party.Version,
            party.Active,
            party.Kind.ToString().ToUpperInvariant(),
            party.Name,
            party.ResidenceCountry,
            party.TaxResidenceCountry,
            party.Roles.Select(x => x.ToString().ToUpperInvariant()).ToArray(),
            party.FiscalIdentities.Select(x => new PartyFiscalIdentityDto(
                x.Id.ToString(),
                x.TypeCode,
                x.Number,
                x.IssuingCountry,
                x.ValidFrom,
                x.ValidTo,
                x.Active)).ToArray());

    private static ApplicationProblemException Validation(string code, string detail) =>
        new(ApplicationProblemKind.Validation, code, detail);
}
