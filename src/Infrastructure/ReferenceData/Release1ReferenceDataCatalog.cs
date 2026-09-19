using EFactura.Application.ReferenceData;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.ReferenceData;

public sealed class Release1ReferenceDataCatalog : IReferenceDataCatalog
{
    private static readonly ReferenceDataSet<CountryReference> Countries = new(
        "ISO 3166-1 current country codes",
        "snapshot-2026-09-19",
        Release1CountryCatalog.Items);

    private static readonly ReferenceDataSet<UruguayDepartmentReference> UruguayDepartments = new(
        "Uruguay administrative departments",
        "release-1",
        new UruguayDepartmentReference[]
        {
            new("Artigas"),
            new("Canelones"),
            new("Cerro Largo"),
            new("Colonia"),
            new("Durazno"),
            new("Flores"),
            new("Florida"),
            new("Lavalleja"),
            new("Maldonado"),
            new("Montevideo"),
            new("Paysandú"),
            new("Río Negro"),
            new("Rivera"),
            new("Rocha"),
            new("Salto"),
            new("San José"),
            new("Soriano"),
            new("Tacuarembó"),
            new("Treinta y Tres")
        });

    private static readonly ReferenceDataSet<FiscalIdentityTypeReference> FiscalIdentityTypes = new(
        "DGI Formato_CFE",
        "25-2",
        new FiscalIdentityTypeReference[]
        {
            new(1, "NIE", "UY_ONLY", new[] { "UY" }, false, false),
            new(2, "RUC (Uruguay)", "UY_ONLY", new[] { "UY" }, false, false),
            new(3, "C.I. (Uruguay)", "UY_ONLY", new[] { "UY" }, false, false),
            new(4, "Otros", "FOREIGN_OR_SPECIAL_FALLBACK", Array.Empty<string>(), true, true),
            new(5, "Pasaporte", "ANY_COUNTRY", Array.Empty<string>(), true, false),
            new(6, "DNI", "AR_BR_CL_PY_ONLY", new[] { "AR", "BR", "CL", "PY" }, false, false),
            new(7, "NIFE", "FOREIGN_OR_SPECIAL_FALLBACK", Array.Empty<string>(), true, true)
        });

    private static readonly ReferenceDataSet<CurrencyReference> Currencies = new(
        "ISO 4217 / eFactura Release-1 supported currency policy",
        "release-1/amendment-180",
        Release1CurrencyCatalog.Items);

    private static readonly ReferenceDataSet<FiscalDocumentTypeReference> FiscalDocumentTypes = new(
        "DGI Formato_CFE / eFactura Release-1 enabled document policy",
        "25-2/release-1-domestic",
        Release1FiscalReferenceCatalog.DocumentTypes);

    private static readonly ReferenceDataSet<InvoiceIndicatorReference> InvoiceIndicators = new(
        "DGI Formato_CFE / eFactura Release-1 supported indicator policy",
        "25-2/release-1",
        Release1FiscalReferenceCatalog.InvoiceIndicators);

    public ValueTask<ReferenceDataSet<CountryReference>> ListCountriesAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Countries);

    public ValueTask<ReferenceDataSet<UruguayDepartmentReference>> ListUruguayDepartmentsAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(UruguayDepartments);

    public ValueTask<ReferenceDataSet<FiscalIdentityTypeReference>> ListFiscalIdentityTypesAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(FiscalIdentityTypes);

    public ValueTask<ReferenceDataSet<CurrencyReference>> ListCurrenciesAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Currencies);

    public ValueTask<ReferenceDataSet<FiscalDocumentTypeReference>> ListFiscalDocumentTypesAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(FiscalDocumentTypes);

    public ValueTask<ReferenceDataSet<InvoiceIndicatorReference>> ListInvoiceIndicatorsAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InvoiceIndicators);
}

public static class ReferenceDataServiceCollectionExtensions
{
    public static IServiceCollection AddReferenceDataFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IReferenceDataCatalog, Release1ReferenceDataCatalog>();
        services.AddScoped<ListCountriesUseCase>();
        services.AddScoped<ListUruguayDepartmentsUseCase>();
        services.AddScoped<ListFiscalIdentityTypesUseCase>();
        services.AddScoped<ListCurrenciesUseCase>();
        services.AddScoped<ListFiscalDocumentTypesUseCase>();
        services.AddScoped<ListInvoiceIndicatorsUseCase>();

        return services;
    }
}
