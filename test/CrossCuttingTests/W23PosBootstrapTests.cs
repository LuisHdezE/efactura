using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.Organizations;
using EFactura.Application.Sales;
using EFactura.Domain.Organizations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1;
using WebApi.CrossCutting.Requests;
using Xunit;

namespace CrossCuttingTests;

public sealed class W23PosBootstrapTests
{
    private const string OrganizationId = "org-a";

    [Fact]
    public async Task Bootstrap_filters_active_scoped_context_and_orders_deterministically()
    {
        var locations = new LocationRepository(
            Location("loc-b", "Zulu", "0002", active: true, version: 4),
            Location("loc-a", "alpha", "0001", active: true, version: 3),
            Location("loc-inactive", "Beta", "0003", active: false, version: 2),
            Location("loc-unscoped", "Gamma", "0004", active: true, version: 1),
            Location("loc-other", "Other", "0005", active: true, version: 1, organizationId: "org-b"));

        var terminals = new TerminalRepository(
            Terminal("term-z", "POS-Z", "Zeta", "loc-a", active: true, version: 5),
            Terminal("term-a", "POS-A", "Alpha", "loc-a", active: true, version: 2),
            Terminal("term-b", "POS-B", "Bravo", "loc-b", active: true, version: 7),
            Terminal("term-inactive", "POS-X", "Inactive", "loc-a", active: false, version: 1),
            Terminal("term-unscoped", "POS-U", "Unscoped", "loc-unscoped", active: true, version: 1),
            Terminal("term-other", "POS-O", "Other", "loc-a", active: true, version: 1, organizationId: "org-b"));

        var result = await UseCase(
            locations,
            terminals,
            Actor(
                new[] { "loc-a", "loc-b" },
                Array.Empty<string>(),
                Permissions.SalesRead)).ExecuteAsync(OrganizationId);

        Assert.Equal(new[] { "loc-a", "loc-b" }, result.Contexts.Select(context => context.LocationId));
        Assert.Equal(new[] { "term-a", "term-z" }, result.Contexts[0].Terminals.Select(terminal => terminal.TerminalId));
        Assert.Equal(new[] { "term-b" }, result.Contexts[1].Terminals.Select(terminal => terminal.TerminalId));
        Assert.Equal("0001", result.Contexts[0].DgiBranchCode);
        Assert.Equal(3, result.Contexts[0].LocationVersion);
        Assert.Equal(2, result.Contexts[0].Terminals[0].TerminalVersion);
    }

    [Fact]
    public async Task Explicit_terminal_scopes_restrict_ids_and_remove_empty_locations()
    {
        var locations = new LocationRepository(
            Location("loc-a", "Alpha", "0001"),
            Location("loc-b", "Beta", "0002"));
        var terminals = new TerminalRepository(
            Terminal("term-a", "POS-A", "A", "loc-a"),
            Terminal("term-b", "POS-B", "B", "loc-b"));

        var result = await UseCase(
            locations,
            terminals,
            Actor(new[] { "loc-a", "loc-b" }, new[] { "term-b" }, Permissions.SalesRead))
            .ExecuteAsync(OrganizationId);

        var context = Assert.Single(result.Contexts);
        Assert.Equal("loc-b", context.LocationId);
        Assert.Equal("term-b", Assert.Single(context.Terminals).TerminalId);
    }

    [Fact]
    public async Task Empty_location_scope_returns_empty_context_without_reading_repositories()
    {
        var locations = new LocationRepository(Location("loc-a", "Alpha", "0001"));
        var terminals = new TerminalRepository(Terminal("term-a", "POS-A", "A", "loc-a"));

        var result = await UseCase(
            locations,
            terminals,
            Actor(Array.Empty<string>(), Array.Empty<string>(), Permissions.SalesRead))
            .ExecuteAsync(OrganizationId);

        Assert.Empty(result.Contexts);
        Assert.Equal(0, locations.ListCalls);
        Assert.Equal(0, terminals.ListCalls);
    }

    [Fact]
    public async Task Missing_sales_read_is_forbidden()
    {
        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(
                new LocationRepository(),
                new TerminalRepository(),
                Actor(new[] { "loc-a" }, Array.Empty<string>()))
            .ExecuteAsync(OrganizationId));

        Assert.Equal(ApplicationProblemKind.Forbidden, error.Kind);
        Assert.Equal("permission_denied", error.Code);
    }

    [Fact]
    public async Task Company_scope_escape_is_forbidden()
    {
        var actor = new ActorContext(
            "actor-w23",
            "POS Reader",
            true,
            new HashSet<string>(new[] { Permissions.SalesRead }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "org-b" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-a" }, StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            null);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(new LocationRepository(), new TerminalRepository(), actor).ExecuteAsync(OrganizationId));

        Assert.Equal(ApplicationProblemKind.Forbidden, error.Kind);
        Assert.Equal("organization_scope_denied", error.Code);
    }

    [Fact]
    public async Task Projection_tag_is_stable_when_authorized_projection_is_unchanged()
    {
        var locations = new LocationRepository(Location("loc-a", "Alpha", "0001", version: 3));
        var terminals = new TerminalRepository(Terminal("term-a", "POS-A", "A", "loc-a", version: 4));
        var useCase = UseCase(
            locations,
            terminals,
            Actor(new[] { "loc-a" }, Array.Empty<string>(), Permissions.SalesRead));

        var first = await useCase.ExecuteAsync(OrganizationId);
        await Task.Yield();
        var second = await useCase.ExecuteAsync(OrganizationId);

        Assert.Equal(first.ProjectionTag, second.ProjectionTag);
        Assert.StartsWith("\"", first.ProjectionTag, StringComparison.Ordinal);
        Assert.EndsWith("\"", first.ProjectionTag, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scope_change_changes_tag_even_when_public_body_is_same()
    {
        var locations = new LocationRepository(Location("loc-a", "Alpha", "0001"));
        var terminals = new TerminalRepository(Terminal("term-a", "POS-A", "A", "loc-a"));

        var unboundedTerminalScope = await UseCase(
            locations,
            terminals,
            Actor(new[] { "loc-a" }, Array.Empty<string>(), Permissions.SalesRead))
            .ExecuteAsync(OrganizationId);

        var explicitSameTerminal = await UseCase(
            locations,
            terminals,
            Actor(new[] { "loc-a" }, new[] { "term-a" }, Permissions.SalesRead))
            .ExecuteAsync(OrganizationId);

        Assert.Equal(
            unboundedTerminalScope.Contexts[0].Terminals[0].TerminalId,
            explicitSameTerminal.Contexts[0].Terminals[0].TerminalId);
        Assert.NotEqual(unboundedTerminalScope.ProjectionTag, explicitSameTerminal.ProjectionTag);
    }

    [Fact]
    public async Task Controller_sets_private_revalidation_headers_and_returns_304_for_matching_tag()
    {
        var actorAccessor = new ActorAccessor(
            Actor(new[] { "loc-a" }, Array.Empty<string>(), Permissions.SalesRead));
        var locations = new LocationRepository(Location("loc-a", "Alpha", "0001"));
        var terminals = new TerminalRepository(Terminal("term-a", "POS-A", "A", "loc-a"));
        var controller = new PosBootstrapController(
            new V1OrganizationContextResolver(actorAccessor),
            locations,
            terminals,
            actorAccessor)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var first = await controller.Get();
        Assert.IsType<OkObjectResult>(first.Result);
        Assert.Equal("private, no-cache", controller.Response.Headers.CacheControl.ToString());
        var tag = controller.Response.Headers.ETag.ToString();
        Assert.False(string.IsNullOrWhiteSpace(tag));

        controller.Request.Headers["If-None-Match"] = tag;
        var second = await controller.Get();

        var notModified = Assert.IsType<StatusCodeResult>(second.Result);
        Assert.Equal(StatusCodes.Status304NotModified, notModified.StatusCode);
        Assert.Equal(tag, controller.Response.Headers.ETag.ToString());
    }

    private static GetPosBootstrapUseCase UseCase(
        IFiscalLocationRepository locations,
        ITerminalRepository terminals,
        ActorContext actor) =>
        new(locations, terminals, new ActorAccessor(actor));

    private static ActorContext Actor(
        IEnumerable<string> locationScopes,
        IEnumerable<string> terminalScopes,
        params string[] permissions) =>
        new(
            "actor-w23",
            "POS Reader",
            true,
            new HashSet<string>(permissions, StringComparer.Ordinal),
            new HashSet<string>(new[] { OrganizationId }, StringComparer.Ordinal),
            new HashSet<string>(locationScopes, StringComparer.Ordinal),
            new HashSet<string>(terminalScopes, StringComparer.Ordinal),
            null);

    private static FiscalLocation Location(
        string id,
        string name,
        string branchCode,
        bool active = true,
        long version = 1,
        string organizationId = OrganizationId) =>
        FiscalLocation.Rehydrate(
            id,
            organizationId,
            name,
            branchCode,
            "Address",
            "City",
            "Department",
            active,
            version);

    private static Terminal Terminal(
        string id,
        string code,
        string name,
        string locationId,
        bool active = true,
        long version = 1,
        string organizationId = OrganizationId) =>
        EFactura.Domain.Organizations.Terminal.Rehydrate(
            id,
            organizationId,
            code,
            name,
            locationId,
            active,
            version);

    private sealed class ActorAccessor(ActorContext current) : IActorContextAccessor
    {
        public ActorContext Current { get; } = current;
    }

    private sealed class LocationRepository(params FiscalLocation[] locations) : IFiscalLocationRepository
    {
        public int ListCalls { get; private set; }

        public Task<IReadOnlyCollection<FiscalLocation>> ListAsync(
            string organizationId,
            bool? active,
            CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyCollection<FiscalLocation>>(locations);
        }

        public Task<FiscalLocation?> GetAsync(string organizationId, string locationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(locations.FirstOrDefault(location => location.Id == locationId && location.OrganizationId == organizationId));

        public Task<bool> BranchCodeExistsAsync(string organizationId, string dgiBranchCode, string? excludingLocationId = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(FiscalLocation location, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(FiscalLocation location, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TerminalRepository(params Terminal[] terminals) : ITerminalRepository
    {
        public int ListCalls { get; private set; }

        public Task<IReadOnlyCollection<Terminal>> ListAsync(
            string organizationId,
            bool? active,
            string? locationId,
            CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyCollection<Terminal>>(terminals);
        }

        public Task<Terminal?> GetAsync(string organizationId, string terminalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(terminals.FirstOrDefault(terminal => terminal.Id == terminalId && terminal.OrganizationId == organizationId));

        public Task<bool> CodeExistsAsync(string organizationId, string normalizedCode, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasActiveAtLocationAsync(string organizationId, string locationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(Terminal terminal, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(Terminal terminal, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
