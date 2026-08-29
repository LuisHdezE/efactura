using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Taxation;

public sealed record TaxProfileSearchRequest(
    string OrganizationId,
    DateOnly OnDate,
    int Page = 1,
    int PageSize = 100);

public sealed record TaxProfileView(
    Guid Id,
    string? OrganizationId,
    string Code,
    string Name,
    TaxTreatmentKind Treatment,
    decimal? RatePercent,
    int CfeBillingIndicator,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string RuleVersion,
    string SourceAuthority,
    string SourceReference,
    string SourceUri,
    string CfeSpecificationVersion,
    DateTimeOffset VerifiedAt,
    bool SystemProfile)
{
    public static TaxProfileView FromDomain(TaxProfile profile) =>
        new(
            profile.Id,
            profile.OrganizationId,
            profile.Code,
            profile.Name,
            profile.Treatment,
            profile.RatePercent,
            profile.CfeBillingIndicator,
            profile.EffectiveFrom,
            profile.EffectiveTo,
            profile.RuleVersion,
            profile.SourceAuthority,
            profile.SourceReference,
            profile.SourceUri,
            profile.CfeSpecificationVersion,
            profile.VerifiedAt,
            profile.IsSystemProfile);
}

public interface ITaxProfileRepository
{
    Task<PageResult<TaxProfile>> SearchUsableAsync(
        TaxProfileSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<TaxProfile?> GetUsableAsync(
        string organizationId,
        Guid taxProfileId,
        DateOnly onDate,
        CancellationToken cancellationToken = default);
}

public interface ITaxProfileAssignmentValidator
{
    Task<TaxProfileView> RequireUsableAsync(
        string organizationId,
        Guid taxProfileId,
        DateOnly onDate,
        CancellationToken cancellationToken = default);
}

public sealed class TaxProfileAssignmentValidator : ITaxProfileAssignmentValidator
{
    private readonly ITaxProfileRepository _profiles;

    public TaxProfileAssignmentValidator(ITaxProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<TaxProfileView> RequireUsableAsync(
        string organizationId,
        Guid taxProfileId,
        DateOnly onDate,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profiles.GetUsableAsync(organizationId, taxProfileId, onDate, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "tax.profile_not_usable",
                "The selected tax profile does not exist, is inactive, is outside its effective period, or is not available to this organization.");

        return TaxProfileView.FromDomain(profile);
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
        EnsureAuthorized(request.OrganizationId);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var result = await _profiles.SearchUsableAsync(
            request with { Page = page, PageSize = pageSize },
            cancellationToken);

        return new PageResult<TaxProfileView>(
            result.Items.Select(TaxProfileView.FromDomain).ToArray(),
            page,
            pageSize,
            result.Total);
    }

    private void EnsureAuthorized(string organizationId)
    {
        var actor = _actorContext.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.CatalogRead))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The actor is not allowed to read tax profiles.");
        }

        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to read tax profiles in this organization.");
        }
    }
}
