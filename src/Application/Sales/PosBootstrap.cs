using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Security;
using EFactura.Application.Organizations;
using EFactura.Domain.Organizations;

namespace EFactura.Application.Sales;

public sealed record PosBootstrapTerminalView(
    string TerminalId,
    string Code,
    string Name,
    long TerminalVersion);

public sealed record PosBootstrapContextView(
    string LocationId,
    string LocationName,
    string DgiBranchCode,
    long LocationVersion,
    IReadOnlyList<PosBootstrapTerminalView> Terminals);

public sealed record PosBootstrapView(
    string OrganizationId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<PosBootstrapContextView> Contexts,
    string ProjectionTag);

/// <summary>
/// Builds the minimal actor-scoped operational context required by POS clients.
/// This is deliberately read-only and does not compose organization.read use cases,
/// mutate persistence, or absorb payment/catalog/party/fiscal responsibilities.
/// </summary>
public sealed class GetPosBootstrapUseCase
{
    private readonly IFiscalLocationRepository _locations;
    private readonly ITerminalRepository _terminals;
    private readonly IActorContextAccessor _actors;

    public GetPosBootstrapUseCase(
        IFiscalLocationRepository locations,
        ITerminalRepository terminals,
        IActorContextAccessor actors)
    {
        _locations = locations;
        _terminals = terminals;
        _actors = actors;
    }

    public async Task<PosBootstrapView> ExecuteAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        SalesAuthorization.Ensure(_actors, organizationId, Permissions.SalesRead);
        var actor = _actors.Current;

        if (actor.LocationScopes.Count == 0)
            return Build(organizationId, actor, Array.Empty<PosBootstrapContextView>());

        var locationCandidates = await _locations.ListAsync(organizationId, true, cancellationToken);
        var terminalCandidates = await _terminals.ListAsync(organizationId, true, null, cancellationToken);

        var allowedLocations = locationCandidates
            .Where(location =>
                location.Active
                && string.Equals(location.OrganizationId, organizationId, StringComparison.Ordinal)
                && actor.LocationScopes.Contains(location.Id))
            .OrderBy(location => location.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(location => location.Id, StringComparer.Ordinal)
            .ToArray();

        var contexts = new List<PosBootstrapContextView>(allowedLocations.Length);
        foreach (var location in allowedLocations)
        {
            var terminalViews = terminalCandidates
                .Where(terminal =>
                    terminal.Active
                    && string.Equals(terminal.OrganizationId, organizationId, StringComparison.Ordinal)
                    && string.Equals(terminal.LocationId, location.Id, StringComparison.Ordinal)
                    && (actor.TerminalScopes.Count == 0 || actor.TerminalScopes.Contains(terminal.Id)))
                .OrderBy(terminal => terminal.Code, StringComparer.OrdinalIgnoreCase)
                .ThenBy(terminal => terminal.Id, StringComparer.Ordinal)
                .Select(terminal => new PosBootstrapTerminalView(
                    terminal.Id,
                    terminal.Code,
                    terminal.Name,
                    terminal.Version))
                .ToArray();

            if (terminalViews.Length == 0)
                continue;

            contexts.Add(new PosBootstrapContextView(
                location.Id,
                location.Name,
                location.DgiBranchCode,
                location.Version,
                terminalViews));
        }

        return Build(organizationId, actor, contexts);
    }

    private static PosBootstrapView Build(
        string organizationId,
        ActorContext actor,
        IReadOnlyList<PosBootstrapContextView> contexts) =>
        new(
            organizationId,
            DateTimeOffset.UtcNow,
            contexts,
            PosBootstrapProjectionTag.Create(
                organizationId,
                actor.LocationScopes,
                actor.TerminalScopes,
                contexts));
}

internal static class PosBootstrapProjectionTag
{
    public static string Create(
        string organizationId,
        IReadOnlySet<string> locationScopes,
        IReadOnlySet<string> terminalScopes,
        IReadOnlyList<PosBootstrapContextView> contexts)
    {
        var canonical = new StringBuilder();
        Append(canonical, organizationId);

        foreach (var scope in locationScopes.OrderBy(value => value, StringComparer.Ordinal))
            Append(canonical, $"L:{scope}");

        foreach (var scope in terminalScopes.OrderBy(value => value, StringComparer.Ordinal))
            Append(canonical, $"T:{scope}");

        foreach (var context in contexts)
        {
            Append(canonical, context.LocationId);
            Append(canonical, context.LocationName);
            Append(canonical, context.DgiBranchCode);
            Append(canonical, context.LocationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));

            foreach (var terminal in context.Terminals)
            {
                Append(canonical, terminal.TerminalId);
                Append(canonical, terminal.Code);
                Append(canonical, terminal.Name);
                Append(canonical, terminal.TerminalVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }

    private static void Append(StringBuilder builder, string value) =>
        builder.Append(value.Length).Append(':').Append(value).Append('|');
}
