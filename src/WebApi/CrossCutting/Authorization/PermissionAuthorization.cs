using EFactura.Application.Common.Context;
using EFactura.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace WebApi.CrossCutting.Authorization;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        if (!Permissions.IsKnown(permission))
        {
            throw new ArgumentOutOfRangeException(nameof(permission), permission, "Unknown eFactura permission.");
        }

        Policy = PermissionPolicyProvider.PolicyPrefix + permission;
    }
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IActorContextAccessor _actorContextAccessor;

    public PermissionAuthorizationHandler(IActorContextAccessor actorContextAccessor)
    {
        _actorContextAccessor = actorContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var actor = _actorContextAccessor.Current;
        if (actor.IsAuthenticated && actor.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "Permission:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permission = policyName[PolicyPrefix.Length..];
        if (!Permissions.IsKnown(permission))
        {
            throw new InvalidOperationException($"Unknown eFactura permission policy '{permission}'.");
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
