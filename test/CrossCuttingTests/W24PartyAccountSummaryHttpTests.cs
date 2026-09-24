using EFactura.Application.Common.Context;
using EFactura.Application.Parties;
using EFactura.Application.Payables;
using EFactura.Application.Receivables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Requests;
using Xunit;

namespace CrossCuttingTests;

public sealed class W24PartyAccountSummaryHttpTests
{
    [Fact]
    public async Task Controller_uses_server_organization_and_as_of_then_maps_authoritative_projection()
    {
        const string organizationId = "org-w24";
        var partyId = Guid.NewGuid();
        var actor = new ActorContext(
            "actor-w24",
            "W2.4 Reader",
            true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal) { organizationId },
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            null);
        var projection = new CapturingSummaryReadModel();
        var controller = new PartyAccountSummaryController(
            new V1OrganizationContextResolver(new FakeActorAccessor(actor)),
            projection)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var before = DateTimeOffset.UtcNow;
        var action = await controller.Get(partyId);
        var after = DateTimeOffset.UtcNow;

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var dto = Assert.IsType<PartyAccountSummaryDto>(ok.Value);

        Assert.Equal(organizationId, projection.OrganizationId);
        Assert.Equal(partyId, projection.PartyId);
        Assert.InRange(projection.AsOfUtc, before, after);
        Assert.Equal(TimeSpan.Zero, projection.AsOfUtc.Offset);

        Assert.Equal(organizationId, dto.OrganizationId);
        Assert.Equal(partyId, dto.PartyId);
        Assert.Equal(projection.AsOfUtc, dto.AsOfUtc);
        Assert.True(dto.ReceivablesApplicable);
        Assert.True(dto.PayablesApplicable);

        var receivable = Assert.Single(dto.Receivables);
        Assert.Equal("UYU", receivable.CurrencyCode);
        Assert.Equal(75.125000m, receivable.Outstanding);
        Assert.Equal(25.125000m, receivable.Overdue);
        Assert.Equal(50.000000m, receivable.Aging.Current);
        Assert.Equal(25.125000m, receivable.Aging.Days1To30);
        Assert.Equal(0m, receivable.Aging.Days31To60);
        Assert.Equal(0m, receivable.Aging.Days61To90);
        Assert.Equal(0m, receivable.Aging.Days91Plus);

        var payable = Assert.Single(dto.Payables);
        Assert.Equal("USD", payable.CurrencyCode);
        Assert.Equal(50.500000m, payable.Outstanding);
        Assert.Equal(50.500000m, payable.Overdue);
        Assert.Equal(0m, payable.Aging.Current);
        Assert.Equal(0m, payable.Aging.Days1To30);
        Assert.Equal(50.500000m, payable.Aging.Days31To60);
        Assert.Equal(0m, payable.Aging.Days61To90);
        Assert.Equal(0m, payable.Aging.Days91Plus);

        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl.ToString());
    }

    private sealed class FakeActorAccessor : IActorContextAccessor
    {
        public FakeActorAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }

    private sealed class CapturingSummaryReadModel : IPartyAccountSummaryReadModel
    {
        public string OrganizationId { get; private set; } = string.Empty;
        public Guid PartyId { get; private set; }
        public DateTimeOffset AsOfUtc { get; private set; }

        public Task<PartyAccountSummary> GetAsync(
            string organizationId,
            Guid partyId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken = default)
        {
            OrganizationId = organizationId;
            PartyId = partyId;
            AsOfUtc = asOfUtc;

            return Task.FromResult(new PartyAccountSummary(
                organizationId,
                partyId,
                asOfUtc,
                true,
                new[]
                {
                    new ReceivableCurrencyAccountSummary(
                        "UYU",
                        75.125000m,
                        25.125000m,
                        new ReceivableAgingBuckets(50.000000m, 25.125000m, 0m, 0m, 0m))
                },
                true,
                new[]
                {
                    new PayableCurrencyAccountSummary(
                        "USD",
                        50.500000m,
                        50.500000m,
                        new PayableAgingBuckets(0m, 0m, 50.500000m, 0m, 0m))
                }));
        }
    }
}
