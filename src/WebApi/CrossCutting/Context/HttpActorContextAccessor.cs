using System.Security.Claims;
using EFactura.Application.Common.Context;

namespace WebApi.CrossCutting.Context;

public static class V1ClaimTypes
{
    public const string Permission = "permission";
    public const string Permissions = "permissions";
    public const string CompanyScope = "company_scope";
    public const string LocationScope = "location_scope";
    public const string TerminalScope = "terminal_scope";
    public const string DeviceId = "device_id";
}

public sealed class HttpActorContextAccessor : IActorContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpActorContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ActorContext Current
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return ActorContext.Anonymous;
            }

            var actorId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            var displayName = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name);
            var permissions = ReadSet(user, V1ClaimTypes.Permission, V1ClaimTypes.Permissions);
            var companies = ReadSet(user, V1ClaimTypes.CompanyScope);
            var locations = ReadSet(user, V1ClaimTypes.LocationScope);
            var terminals = ReadSet(user, V1ClaimTypes.TerminalScope);
            var deviceId = user.FindFirstValue(V1ClaimTypes.DeviceId);

            return new ActorContext(
                actorId,
                displayName,
                true,
                permissions,
                companies,
                locations,
                terminals,
                deviceId);
        }
    }

    private static IReadOnlySet<string> ReadSet(ClaimsPrincipal user, params string[] claimTypes)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in user.Claims.Where(claim => claimTypes.Contains(claim.Type, StringComparer.Ordinal)))
        {
            foreach (var value in claim.Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                values.Add(value);
            }
        }

        return values;
    }
}
