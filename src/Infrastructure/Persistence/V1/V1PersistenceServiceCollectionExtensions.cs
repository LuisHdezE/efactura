using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Application.Parties;
using EFactura.Application.Sales;
using EFactura.Application.Taxation;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.V1;

public static class V1PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddV1Persistence(
        this IServiceCollection services,
        V1DatabaseProvider provider,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<V1PersistenceDbContext>(options =>
            V1PersistenceDatabaseConfigurator.Configure(options, provider, connectionString));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ITransactionManager, EfTransactionManager>();
        services.AddScoped<IAuditWriter, EfAuditWriter>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();
        services.AddScoped<IInboxStore, EfInboxStore>();

        services.AddScoped<EfPartyRepository>();
        services.AddScoped<IPartyRepository>(sp => sp.GetRequiredService<EfPartyRepository>());
        services.AddScoped<IPartyMaintenanceRepository>(sp => sp.GetRequiredService<EfPartyRepository>());

        services.AddScoped<EfCommercialItemRepository>();
        services.AddScoped<ICommercialItemRepository>(sp => sp.GetRequiredService<EfCommercialItemRepository>());
        services.AddScoped<ICommercialItemMaintenanceRepository>(sp => sp.GetRequiredService<EfCommercialItemRepository>());

        services.AddScoped<IItemCategoryRepository, EfItemCategoryRepository>();
        services.AddScoped<ITaxProfileRepository, EfTaxProfileRepository>();
        services.AddScoped<ITaxProfileAssignmentValidator, TaxProfileAssignmentValidator>();
        services.AddScoped<ListTaxProfilesUseCase>();
        services.AddScoped<TaxSafeUpdateCommercialItemUseCase>();

        services.AddSingleton<TaxTreatmentDecisionEngine>();
        services.AddSingleton<ITaxTreatmentRulePackProvider, UruguayRelease1TaxTreatmentRulePackProvider>();
        services.AddSingleton<IExportServiceEligibilityEvaluator, Article34Numeral11ExportServiceEligibilityEvaluator>();
        services.AddScoped<ResolveTaxTreatmentUseCase>();

        services.AddSingleton<IVatRateRulePackProvider, UruguayRelease1VatRateRulePackProvider>();
        services.AddScoped<ResolveTaxRateUseCase>();

        services.AddSingleton<CfeEligibilityPolicy>();
        services.AddSingleton<ICfeEligibilityRulePackProvider, UruguayCfe25_2EligibilityRulePackProvider>();
        services.AddScoped<PrepareCfeEligibilityUseCase>();

        services.AddSingleton<CfeSelectionPolicy>();
        services.AddSingleton<ICfeSelectionConfigurationProvider, Release1CfeSelectionConfigurationProvider>();
        services.AddScoped<SelectCfeUseCase>();

        services.AddScoped<ISaleRepository, EfSaleRepository>();
        services.AddScoped<SaleDraftBuilder>();
        services.AddScoped<CreateSaleUseCase>();
        services.AddScoped<UpdateSaleDraftUseCase>();
        services.AddScoped<GetSaleUseCase>();
        services.AddScoped<ListSalesUseCase>();
        services.AddScoped<IUiAmountConverter, Release1UiAmountConverter>();
        services.AddScoped<GetSaleFiscalPreviewUseCase>();
        services.AddScoped<ValidateSaleUseCase>();

        return services;
    }
}
