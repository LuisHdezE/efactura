using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;

namespace EFactura.Application.ReferenceData;

public sealed record ReferenceDataSet<T>(
    string SourceName,
    string SourceVersion,
    IReadOnlyList<T> Items);

public sealed record UruguayDepartmentReference(string Name);

public sealed record FiscalIdentityTypeReference(
    int Code,
    string Name,
    string CountryRule,
    IReadOnlyList<string> AllowedIssuingCountryCodes,
    bool AllowsOtherIsoCountry,
    bool AllowsSpecialCountryFallback);

public interface IReferenceDataCatalog
{
    ValueTask<ReferenceDataSet<UruguayDepartmentReference>> ListUruguayDepartmentsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ReferenceDataSet<FiscalIdentityTypeReference>> ListFiscalIdentityTypesAsync(
        CancellationToken cancellationToken = default);
}

public sealed class ListUruguayDepartmentsUseCase
{
    private readonly IReferenceDataCatalog _catalog;
    private readonly IActorContextAccessor _actorContextAccessor;

    public ListUruguayDepartmentsUseCase(
        IReferenceDataCatalog catalog,
        IActorContextAccessor actorContextAccessor)
    {
        _catalog = catalog;
        _actorContextAccessor = actorContextAccessor;
    }

    public ValueTask<ReferenceDataSet<UruguayDepartmentReference>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(_actorContextAccessor.Current);
        return _catalog.ListUruguayDepartmentsAsync(cancellationToken);
    }

    private static void EnsureAuthenticated(ActorContext actor)
    {
        if (!actor.IsAuthenticated)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.AuthenticationRequired,
                "authentication_required",
                "A valid bearer token is required for this operation.");
        }
    }
}

public sealed class ListFiscalIdentityTypesUseCase
{
    private readonly IReferenceDataCatalog _catalog;
    private readonly IActorContextAccessor _actorContextAccessor;

    public ListFiscalIdentityTypesUseCase(
        IReferenceDataCatalog catalog,
        IActorContextAccessor actorContextAccessor)
    {
        _catalog = catalog;
        _actorContextAccessor = actorContextAccessor;
    }

    public ValueTask<ReferenceDataSet<FiscalIdentityTypeReference>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(_actorContextAccessor.Current);
        return _catalog.ListFiscalIdentityTypesAsync(cancellationToken);
    }

    private static void EnsureAuthenticated(ActorContext actor)
    {
        if (!actor.IsAuthenticated)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.AuthenticationRequired,
                "authentication_required",
                "A valid bearer token is required for this operation.");
        }
    }
}
