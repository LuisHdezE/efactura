using System.Text.Json;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.ReferenceData;
using Infrastructure.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using WebApi.CrossCutting.Authorization;
using Xunit;

namespace CrossCuttingTests;

public sealed class ReferenceDataFoundationTests
{
    [Fact]
    public async Task Release1_catalog_exposes_the_exact_19_uruguay_departments()
    {
        var catalog = new Release1ReferenceDataCatalog();

        var result = await catalog.ListUruguayDepartmentsAsync();

        Assert.Equal("Uruguay administrative departments", result.SourceName);
        Assert.Equal("release-1", result.SourceVersion);
        Assert.Equal(
            new[]
            {
                "Artigas", "Canelones", "Cerro Largo", "Colonia", "Durazno", "Flores", "Florida",
                "Lavalleja", "Maldonado", "Montevideo", "Paysandú", "Río Negro", "Rivera", "Rocha",
                "Salto", "San José", "Soriano", "Tacuarembó", "Treinta y Tres"
            },
            result.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Release1_catalog_exposes_versioned_fiscal_identity_metadata()
    {
        var catalog = new Release1ReferenceDataCatalog();

        var result = await catalog.ListFiscalIdentityTypesAsync();

        Assert.Equal("DGI Formato_CFE", result.SourceName);
        Assert.Equal("25-2", result.SourceVersion);
        Assert.Equal(Enumerable.Range(1, 7), result.Items.Select(item => item.Code));

        var dni = Assert.Single(result.Items, item => item.Code == 6);
        Assert.Equal("DNI", dni.Name);
        Assert.Equal("AR_BR_CL_PY_ONLY", dni.CountryRule);
        Assert.Equal(new[] { "AR", "BR", "CL", "PY" }, dni.AllowedIssuingCountryCodes);
        Assert.False(dni.AllowsOtherIsoCountry);
        Assert.False(dni.AllowsSpecialCountryFallback);

        var other = Assert.Single(result.Items, item => item.Code == 4);
        Assert.True(other.AllowsOtherIsoCountry);
        Assert.True(other.AllowsSpecialCountryFallback);
    }

    [Fact]
    public async Task Reference_use_cases_require_an_authenticated_actor()
    {
        var catalog = new Release1ReferenceDataCatalog();
        var actor = new FakeActorAccessor(ActorContext.Anonymous);

        var departments = new ListUruguayDepartmentsUseCase(catalog, actor);
        var identities = new ListFiscalIdentityTypesUseCase(catalog, actor);

        var departmentsError = await Assert.ThrowsAsync<ApplicationProblemException>(
            async () => await departments.ExecuteAsync());
        var identitiesError = await Assert.ThrowsAsync<ApplicationProblemException>(
            async () => await identities.ExecuteAsync());

        Assert.Equal(ApplicationProblemKind.AuthenticationRequired, departmentsError.Kind);
        Assert.Equal("authentication_required", departmentsError.Code);
        Assert.Equal(ApplicationProblemKind.AuthenticationRequired, identitiesError.Kind);
        Assert.Equal("authentication_required", identitiesError.Code);
    }

    [Fact]
    public async Task Reference_use_cases_return_catalog_for_authenticated_actor_without_business_permission()
    {
        var catalog = new Release1ReferenceDataCatalog();
        var actor = new FakeActorAccessor(new ActorContext(
            "actor-1",
            "Reference Reader",
            true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            null));

        var departments = await new ListUruguayDepartmentsUseCase(catalog, actor).ExecuteAsync();
        var identities = await new ListFiscalIdentityTypesUseCase(catalog, actor).ExecuteAsync();

        Assert.Equal(19, departments.Items.Count);
        Assert.Equal(7, identities.Items.Count);
    }

    [Fact]
    public async Task V1_authorization_challenge_returns_rfc9457_authentication_required()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/reference-data/uruguay-departments";
        context.Response.Body = new MemoryStream();

        var handler = new V1AuthorizationMiddlewareResultHandler();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            policy,
            PolicyAuthorizationResult.Challenge());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("authentication_required", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task V1_permission_policy_forbidden_returns_rfc9457_permission_denied()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/pos/bootstrap";
        context.Response.Body = new MemoryStream();

        var handler = new V1AuthorizationMiddlewareResultHandler();
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(Permissions.SalesRead))
            .Build();

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            policy,
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("permission_denied", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task V1_non_permission_forbidden_preserves_generic_forbidden_code()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/reference-data/uruguay-departments";
        context.Response.Body = new MemoryStream();

        var handler = new V1AuthorizationMiddlewareResultHandler();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            policy,
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("forbidden", json.RootElement.GetProperty("code").GetString());
    }

    private sealed class FakeActorAccessor : IActorContextAccessor
    {
        public FakeActorAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }
}
