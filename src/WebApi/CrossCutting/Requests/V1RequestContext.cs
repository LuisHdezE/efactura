using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;

namespace WebApi.CrossCutting.Requests;

public sealed class V1OrganizationContextResolver
{
    public const string HeaderName = "X-Organization-Id";

    private readonly IActorContextAccessor _actorContext;

    public V1OrganizationContextResolver(IActorContextAccessor actorContext)
    {
        _actorContext = actorContext;
    }

    public string Resolve(HttpRequest request)
    {
        var actor = _actorContext.Current;
        if (!actor.IsAuthenticated)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.AuthenticationRequired,
                "authentication_required",
                "Authentication is required.");
        }

        var requested = request.Headers[HeaderName].FirstOrDefault()?.Trim();
        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (!actor.CompanyScopes.Contains(requested))
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Forbidden,
                    "organization_scope_denied",
                    "The requested organization is outside the actor scope.");
            }

            return requested;
        }

        if (actor.CompanyScopes.Count == 1)
        {
            return actor.CompanyScopes.Single();
        }

        if (actor.CompanyScopes.Count == 0)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_missing",
                "The authenticated actor has no organization scope.");
        }

        throw new ApplicationProblemException(
            ApplicationProblemKind.BadRequest,
            "organization_context_required",
            $"Header {HeaderName} is required when the actor has access to more than one organization.");
    }
}

public static class V1RequestContract
{
    public const string IdempotencyHeaderName = "Idempotency-Key";

    private static readonly JsonSerializerOptions HashSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static string RequireIdempotencyKey(HttpRequest request)
    {
        var key = request.Headers[IdempotencyHeaderName].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.BadRequest,
                "idempotency_key_required",
                $"Header {IdempotencyHeaderName} is required for this operation.");
        }

        if (key.Length > 200)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.BadRequest,
                "idempotency_key_too_long",
                "The idempotency key cannot exceed 200 characters.");
        }

        return key;
    }

    public static string ComputeRequestHash<T>(T request)
    {
        var json = JsonSerializer.Serialize(request, HashSerializerOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
