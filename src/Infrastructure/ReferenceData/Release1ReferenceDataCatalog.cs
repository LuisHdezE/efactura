using EFactura.Application.ReferenceData;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.ReferenceData;

public sealed class Release1ReferenceDataCatalog : IReferenceDataCatalog
{
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

    public ValueTask<ReferenceDataSet<UruguayDepartmentReference>> ListUruguayDepartmentsAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(UruguayDepartments);

    public ValueTask<ReferenceDataSet<FiscalIdentityTypeReference>> ListFiscalIdentityTypesAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(FiscalIdentityTypes);
}

public static class ReferenceDataServiceCollectionExtensions
{
    public static IServiceCollection AddReferenceDataFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IReferenceDataCatalog, Release1ReferenceDataCatalog>();
        services.AddScoped<ListUruguayDepartmentsUseCase>();
        services.AddScoped<ListFiscalIdentityTypesUseCase>();

        return services;
    }
}
