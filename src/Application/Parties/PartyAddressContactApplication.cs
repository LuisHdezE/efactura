using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Domain.Parties;

namespace EFactura.Application.Parties;

public sealed record PartyAddressInput(
    PartyAddressKind Kind,
    string AddressLine,
    string City,
    string? Region,
    string CountryCode,
    string? PostalCode,
    bool Primary);

public sealed record PartyContactInput(
    string TypeCode,
    string Value,
    bool Primary);

public sealed record PartyAddressView(
    Guid Id,
    PartyAddressKind Kind,
    string AddressLine,
    string City,
    string? Region,
    string CountryCode,
    string? PostalCode,
    bool Primary);

public sealed record PartyContactView(
    Guid Id,
    string TypeCode,
    string Value,
    bool Primary);

public sealed record PartyDetailView(
    Guid Id,
    long Version,
    bool Active,
    PartyKind Kind,
    string Name,
    string ResidenceCountry,
    string TaxResidenceCountry,
    IReadOnlyCollection<PartyRole> Roles,
    IReadOnlyCollection<PartyFiscalIdentityView> FiscalIdentities,
    IReadOnlyCollection<PartyAddressView> Addresses,
    IReadOnlyCollection<PartyContactView> Contacts)
{
    public static PartyDetailView FromDomain(Party party) =>
        new(
            party.Id,
            party.Version,
            party.Active,
            party.Kind,
            party.Name,
            party.ResidenceCountry,
            party.TaxResidenceCountry,
            party.Roles.OrderBy(x => x).ToArray(),
            party.FiscalIdentities
                .OrderBy(x => x.TypeCode, StringComparer.Ordinal)
                .ThenBy(x => x.Number, StringComparer.Ordinal)
                .Select(x => new PartyFiscalIdentityView(
                    x.Id,
                    x.TypeCode,
                    x.Number,
                    x.IssuingCountry,
                    x.ValidFrom,
                    x.ValidTo,
                    x.Active))
                .ToArray(),
            party.Addresses
                .OrderBy(x => x.Kind)
                .ThenByDescending(x => x.Primary)
                .ThenBy(x => x.AddressLine, StringComparer.Ordinal)
                .Select(x => new PartyAddressView(
                    x.Id,
                    x.Kind,
                    x.AddressLine,
                    x.City,
                    x.Region,
                    x.CountryCode,
                    x.PostalCode,
                    x.Primary))
                .ToArray(),
            party.Contacts
                .OrderBy(x => x.TypeCode, StringComparer.Ordinal)
                .ThenByDescending(x => x.Primary)
                .ThenBy(x => x.Value, StringComparer.Ordinal)
                .Select(x => new PartyContactView(x.Id, x.TypeCode, x.Value, x.Primary))
                .ToArray());
}

public sealed class ListPartiesWithChannelsUseCase
{
    private readonly IPartyMaintenanceRepository _parties;
    private readonly IActorContextAccessor _actorContext;

    public ListPartiesWithChannelsUseCase(
        IPartyMaintenanceRepository parties,
        IActorContextAccessor actorContext)
    {
        _parties = parties;
        _actorContext = actorContext;
    }

    public async Task<PageResult<PartyDetailView>> ExecuteAsync(
        PartySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ListPartiesUseCase.EnsureReadAuthorized(_actorContext.Current, request.OrganizationId);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var result = await _parties.SearchAsync(request with { Page = page, PageSize = pageSize }, cancellationToken);
        return new PageResult<PartyDetailView>(
            result.Items.Select(PartyDetailView.FromDomain).ToArray(),
            page,
            pageSize,
            result.Total);
    }
}

public sealed class GetPartyWithChannelsUseCase
{
    private readonly IPartyRepository _parties;
    private readonly IActorContextAccessor _actorContext;

    public GetPartyWithChannelsUseCase(IPartyRepository parties, IActorContextAccessor actorContext)
    {
        _parties = parties;
        _actorContext = actorContext;
    }

    public async Task<PartyDetailView> ExecuteAsync(
        string organizationId,
        Guid partyId,
        CancellationToken cancellationToken = default)
    {
        ListPartiesUseCase.EnsureReadAuthorized(_actorContext.Current, organizationId);
        var party = await _parties.GetAsync(organizationId, partyId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "party.not_found",
                "The requested party was not found.");

        return PartyDetailView.FromDomain(party);
    }
}

public sealed record UpdatePartyWithChannelsCommand(
    string OrganizationId,
    Guid PartyId,
    PartyKind? Kind,
    string? Name,
    string? ResidenceCountry,
    string? TaxResidenceCountry,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash,
    IReadOnlyCollection<PartyAddressInput>? Addresses = null,
    IReadOnlyCollection<PartyContactInput>? Contacts = null);

public sealed class UpdatePartyWithChannelsUseCase
{
    private readonly PartyMutationWorkflow _workflow;

    public UpdatePartyWithChannelsUseCase(PartyMutationWorkflow workflow)
    {
        _workflow = workflow;
    }

    public Task<PartyMutationResult> ExecuteAsync(
        UpdatePartyWithChannelsCommand command,
        CancellationToken cancellationToken = default) =>
        _workflow.ExecuteAsync(
            command.OrganizationId,
            command.PartyId,
            Permissions.PartiesManage,
            "party.update",
            command.IdempotencyKey,
            command.RequestHash,
            "party.updated",
            "master-data-updated",
            (party, _) =>
            {
                var addresses = command.Addresses is null
                    ? party.Addresses.ToArray()
                    : command.Addresses.Select(input => PartyAddress.Create(
                        Guid.NewGuid(),
                        input.Kind,
                        input.AddressLine,
                        input.City,
                        input.Region,
                        input.CountryCode,
                        input.PostalCode,
                        input.Primary)).ToArray();

                var contacts = command.Contacts is null
                    ? party.Contacts.ToArray()
                    : command.Contacts.Select(input => PartyContact.Create(
                        Guid.NewGuid(),
                        input.TypeCode,
                        input.Value,
                        input.Primary)).ToArray();

                party.UpdateMasterData(
                    command.Kind ?? party.Kind,
                    string.IsNullOrWhiteSpace(command.Name) ? party.Name : command.Name,
                    string.IsNullOrWhiteSpace(command.ResidenceCountry)
                        ? party.ResidenceCountry
                        : command.ResidenceCountry,
                    string.IsNullOrWhiteSpace(command.TaxResidenceCountry)
                        ? party.TaxResidenceCountry
                        : command.TaxResidenceCountry,
                    addresses,
                    contacts,
                    command.ExpectedVersion);

                return Task.CompletedTask;
            },
            party => new Dictionary<string, string?>
            {
                ["version"] = party.Version.ToString(),
                ["kind"] = party.Kind.ToString(),
                ["residenceCountry"] = party.ResidenceCountry,
                ["taxResidenceCountry"] = party.TaxResidenceCountry,
                ["addressCount"] = party.Addresses.Count.ToString(),
                ["contactCount"] = party.Contacts.Count.ToString()
            },
            cancellationToken);
}
