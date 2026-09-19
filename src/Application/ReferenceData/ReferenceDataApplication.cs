using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;

namespace EFactura.Application.ReferenceData;

public sealed record ReferenceDataSet<T>(
    string SourceName,
    string SourceVersion,
    IReadOnlyList<T> Items);

public sealed record CountryReference(
    string Alpha2Code,
    string Name);

public sealed record UruguayDepartmentReference(string Name);

public sealed record FiscalIdentityTypeReference(
    int Code,
    string Name,
    string CountryRule,
    IReadOnlyList<string> AllowedIssuingCountryCodes,
    bool AllowsOtherIsoCountry,
    bool AllowsSpecialCountryFallback);

public sealed record CurrencyReference(
    string AlphabeticCode,
    string Name);

public sealed record FiscalDocumentTypeReference(
    int Code,
    string Name,
    string Family,
    string CorrectionKind,
    int ContingencyCode,
    bool RequiresApplicabilityValidation);

public sealed record InvoiceIndicatorReference(
    int Code,
    string Name,
    string TaxTreatment);

public interface IReferenceDataCatalog
{
    ValueTask<ReferenceDataSet<CountryReference>> ListCountriesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ReferenceDataSet<UruguayDepartmentReference>> ListUruguayDepartmentsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ReferenceDataSet<FiscalIdentityTypeReference>> ListFiscalIdentityTypesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ReferenceDataSet<CurrencyReference>> ListCurrenciesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ReferenceDataSet<FiscalDocumentTypeReference>> ListFiscalDocumentTypesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ReferenceDataSet<InvoiceIndicatorReference>> ListInvoiceIndicatorsAsync(
        CancellationToken cancellationToken = default);
}

public sealed class ListCountriesUseCase
{
    private readonly IReferenceDataCatalog _catalog;
    private readonly IActorContextAccessor _actorContextAccessor;

    public ListCountriesUseCase(
        IReferenceDataCatalog catalog,
        IActorContextAccessor actorContextAccessor)
    {
        _catalog = catalog;
        _actorContextAccessor = actorContextAccessor;
    }

    public ValueTask<ReferenceDataSet<CountryReference>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(_actorContextAccessor.Current);
        return _catalog.ListCountriesAsync(cancellationToken);
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

public sealed class ListCurrenciesUseCase
{
    private readonly IReferenceDataCatalog _catalog;
    private readonly IActorContextAccessor _actorContextAccessor;

    public ListCurrenciesUseCase(
        IReferenceDataCatalog catalog,
        IActorContextAccessor actorContextAccessor)
    {
        _catalog = catalog;
        _actorContextAccessor = actorContextAccessor;
    }

    public ValueTask<ReferenceDataSet<CurrencyReference>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(_actorContextAccessor.Current);
        return _catalog.ListCurrenciesAsync(cancellationToken);
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

public sealed class ListFiscalDocumentTypesUseCase
{
    private readonly IReferenceDataCatalog _catalog;
    private readonly IActorContextAccessor _actorContextAccessor;

    public ListFiscalDocumentTypesUseCase(
        IReferenceDataCatalog catalog,
        IActorContextAccessor actorContextAccessor)
    {
        _catalog = catalog;
        _actorContextAccessor = actorContextAccessor;
    }

    public ValueTask<ReferenceDataSet<FiscalDocumentTypeReference>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(_actorContextAccessor.Current);
        return _catalog.ListFiscalDocumentTypesAsync(cancellationToken);
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

public sealed class ListInvoiceIndicatorsUseCase
{
    private readonly IReferenceDataCatalog _catalog;
    private readonly IActorContextAccessor _actorContextAccessor;

    public ListInvoiceIndicatorsUseCase(
        IReferenceDataCatalog catalog,
        IActorContextAccessor actorContextAccessor)
    {
        _catalog = catalog;
        _actorContextAccessor = actorContextAccessor;
    }

    public ValueTask<ReferenceDataSet<InvoiceIndicatorReference>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(_actorContextAccessor.Current);
        return _catalog.ListInvoiceIndicatorsAsync(cancellationToken);
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
