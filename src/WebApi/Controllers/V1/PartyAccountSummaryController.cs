using EFactura.Application.Common.Security;
using EFactura.Application.Parties;
using EFactura.Application.Payables;
using EFactura.Application.Receivables;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/parties")]
public sealed class PartyAccountSummaryController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly IPartyAccountSummaryReadModel _accountSummary;

    public PartyAccountSummaryController(
        V1OrganizationContextResolver organization,
        IPartyAccountSummaryReadModel accountSummary)
    {
        _organization = organization;
        _accountSummary = accountSummary;
    }

    [HttpGet("{partyId:guid}/account-summary", Name = "getPartyAccountSummary")]
    [RequirePermission(Permissions.PartiesRead)]
    [ProducesResponseType(typeof(PartyAccountSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartyAccountSummaryDto>> Get(
        Guid partyId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var asOfUtc = DateTimeOffset.UtcNow;
        var summary = await _accountSummary.GetAsync(
            organizationId,
            partyId,
            asOfUtc,
            cancellationToken);

        Response.Headers["Cache-Control"] = "private, no-store";
        return Ok(Map(summary));
    }

    private static PartyAccountSummaryDto Map(PartyAccountSummary summary) =>
        new(
            summary.OrganizationId,
            summary.PartyId,
            summary.AsOfUtc,
            summary.ReceivablesApplicable,
            summary.Receivables.Select(Map).ToArray(),
            summary.PayablesApplicable,
            summary.Payables.Select(Map).ToArray());

    private static PartyAccountCurrencyDto Map(ReceivableCurrencyAccountSummary summary) =>
        new(
            summary.CurrencyCode,
            summary.Outstanding,
            summary.Overdue,
            new PartyAccountAgingDto(
                summary.Aging.Current,
                summary.Aging.Days1To30,
                summary.Aging.Days31To60,
                summary.Aging.Days61To90,
                summary.Aging.Days91Plus));

    private static PartyAccountCurrencyDto Map(PayableCurrencyAccountSummary summary) =>
        new(
            summary.CurrencyCode,
            summary.Outstanding,
            summary.Overdue,
            new PartyAccountAgingDto(
                summary.Aging.Current,
                summary.Aging.Days1To30,
                summary.Aging.Days31To60,
                summary.Aging.Days61To90,
                summary.Aging.Days91Plus));
}
