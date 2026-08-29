using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Taxation;

public sealed record TaxProfileSearchRequest(
    string OrganizationId,
    DateOnly EffectiveOn,
    string? Search,
    bool ActiveOnly,
    int Page = 1,
    int PageSize = 100);

public sealed record TaxProfileView(
    Guid Id,
    long Version,
    string Code,
    string Name,
    string TreatmentCode,
    decimal RatePercent,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceName,
    string SourceReference,
    string SourceVersion,
    bool Active)
{
    public static TaxProfileView FromDomain(TaxProfile profile) => new(
        profile.Id,
        profile.Version,
        profile.Code,
        profile.Name,
        profile.TreatmentCode,
        profile.RatePercent,
        profile.EffectiveFrom,
        profile.EffectiveTo,
        profile.SourceName,
        profile.SourceReference,
        profile.SourceVersion,
        profile.Active);
}

public interface ITaxProfileRepository
{
    Task<TaxProfile?> GetAsync(string organizationId, Guid profileId, CancellationToken cancellationToken = default);
    Task<PageResult<TaxProfile>> SearchAsync(TaxProfileSearchRequest request, CancellationToken cancellationToken = default);
    Task AddAsync(TaxProfile profile, CancellationToken cancellationToken = default);
}

public interface ITaxProfileAssignmentValidator
{
    Task ValidateAssignableAsync(
        string organizationId,
        Guid profileId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default);
}

public sealed class TaxProfileAssignmentValidator : ITaxProfileAssignmentValidator
{
    private readonly ITaxProfileRepository _profiles;

    public TaxProfileAssignmentValidator(ITaxProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task ValidateAssignableAsync(
        string organizationId,
        Guid profileId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profiles.GetAsync(organizationId, profileId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "tax.profile_not_found",
                "The selected tax profile does not exist in this organization.");

        if (!profile.IsEffectiveOn(effectiveOn))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "tax.profile_not_effective",
                "The selected tax profile is not active for the requested effective date.");
        }
    }
}

public sealed class ListTaxProfilesUseCase
{
    private readonly ITaxProfileRepository _profiles;
    private readonly IActorContextAccessor _actorContext;

    public ListTaxProfilesUseCase(ITaxProfileRepository profiles, IActorContextAccessor actorContext)
    {
        _profiles = profiles;
        _actorContext = actorContext;
    }

    public async Task<PageResult<TaxProfileView>> ExecuteAsync(
        TaxProfileSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorContext.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.CatalogRead))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The actor is not allowed to read tax profiles.");
        }

        if (!actor.CompanyScopes.Contains(request.OrganizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to read tax profiles in this organization.");
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var result = await _profiles.SearchAsync(request with { Page = page, PageSize = pageSize }, cancellationToken);

        return new PageResult<TaxProfileView>(
            result.Items.Select(TaxProfileView.FromDomain).ToArray(),
            page,
            pageSize,
            result.Total);
    }
}
