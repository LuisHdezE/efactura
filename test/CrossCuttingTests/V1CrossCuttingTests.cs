using System.Security.Claims;
using System.Text.Json;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Context;
using WebApi.CrossCutting.Correlation;
using WebApi.CrossCutting.Errors;
using Xunit;

namespace CrossCuttingTests;

public sealed class V1CrossCuttingTests
{
    [Fact]
    public async Task Correlation_middleware_preserves_safe_client_id_and_returns_header()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationContextKeys.HeaderName] = "client-abc_123";

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("client-abc_123", context.Items[CorrelationContextKeys.CorrelationIdItem]);
        Assert.Equal("client-abc_123", context.Response.Headers[CorrelationContextKeys.HeaderName].ToString());
        Assert.False(string.IsNullOrWhiteSpace(context.Items[CorrelationContextKeys.TraceIdItem]?.ToString()));
    }

    [Fact]
    public async Task Correlation_middleware_replaces_unsafe_client_id()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationContextKeys.HeaderName] = "bad\r\nvalue";

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var actual = context.Items[CorrelationContextKeys.CorrelationIdItem]?.ToString();
        Assert.NotNull(actual);
        Assert.NotEqual("bad\r\nvalue", actual);
        Assert.Equal(32, actual!.Length);
    }

    [Fact]
    public async Task V1_problem_middleware_maps_application_conflict_to_rfc9457_shape()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/sales/123/confirm";
        context.Response.Body = new MemoryStream();
        context.Items[CorrelationContextKeys.CorrelationIdItem] = "corr-123";
        context.Items[CorrelationContextKeys.TraceIdItem] = "trace-123";

        var middleware = new V1ProblemDetailsMiddleware(
            _ => throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The resource changed before this command could be applied.",
                conflictType: "stale_version",
                currentVersion: "18"),
            NullLogger<V1ProblemDetailsMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;
        Assert.Equal("concurrency_conflict", root.GetProperty("code").GetString());
        Assert.Equal("corr-123", root.GetProperty("correlationId").GetString());
        Assert.Equal("trace-123", root.GetProperty("traceId").GetString());
        Assert.Equal("stale_version", root.GetProperty("conflictType").GetString());
        Assert.Equal("18", root.GetProperty("currentVersion").GetString());
    }

    [Fact]
    public async Task V1_problem_middleware_does_not_replace_legacy_exception_contract()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/Customer/GetCustomer";

        var middleware = new V1ProblemDetailsMiddleware(
            _ => throw new InvalidOperationException("legacy"),
            NullLogger<V1ProblemDetailsMiddleware>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }

    [Fact]
    public void Http_actor_context_reads_permissions_and_scopes_without_using_roles_as_authority()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "actor-1"),
                new Claim("name", "Operator"),
                new Claim(V1ClaimTypes.Permission, Permissions.SalesRead),
                new Claim(V1ClaimTypes.Permissions, $"{Permissions.SalesCreate} {Permissions.SalesConfirm}"),
                new Claim(V1ClaimTypes.CompanyScope, "company-a"),
                new Claim(V1ClaimTypes.LocationScope, "location-a"),
                new Claim(V1ClaimTypes.TerminalScope, "terminal-a"),
                new Claim(V1ClaimTypes.DeviceId, "device-a"),
                new Claim(ClaimTypes.Role, "Administrator")
            },
            "test");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var accessor = new HttpActorContextAccessor(httpContextAccessor);
        var actor = accessor.Current;

        Assert.True(actor.IsAuthenticated);
        Assert.Equal("actor-1", actor.ActorId);
        Assert.True(actor.HasPermission(Permissions.SalesRead));
        Assert.True(actor.HasPermission(Permissions.SalesCreate));
        Assert.True(actor.HasPermission(Permissions.SalesConfirm));
        Assert.Contains("company-a", actor.CompanyScopes);
        Assert.Contains("location-a", actor.LocationScopes);
        Assert.Contains("terminal-a", actor.TerminalScopes);
        Assert.Equal("device-a", actor.DeviceId);
        Assert.DoesNotContain("Administrator", actor.Permissions);
    }

    [Fact]
    public async Task Permission_handler_requires_explicit_permission()
    {
        var requirement = new PermissionRequirement(Permissions.FiscalManageCae);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));

        var deniedActor = new ActorContext(
            "actor-1",
            null,
            true,
            new HashSet<string>(StringComparer.Ordinal) { Permissions.FiscalRead },
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            null);

        var deniedContext = new AuthorizationHandlerContext(new[] { requirement }, principal, null);
        await new PermissionAuthorizationHandler(new FakeActorAccessor(deniedActor)).HandleAsync(deniedContext);
        Assert.False(deniedContext.HasSucceeded);

        var allowedActor = deniedActor with
        {
            Permissions = new HashSet<string>(StringComparer.Ordinal) { Permissions.FiscalManageCae }
        };

        var allowedContext = new AuthorizationHandlerContext(new[] { requirement }, principal, null);
        await new PermissionAuthorizationHandler(new FakeActorAccessor(allowedActor)).HandleAsync(allowedContext);
        Assert.True(allowedContext.HasSucceeded);
    }

    private sealed class FakeActorAccessor : IActorContextAccessor
    {
        public FakeActorAccessor(ActorContext actor) => Current = actor;
        public ActorContext Current { get; }
    }
}
