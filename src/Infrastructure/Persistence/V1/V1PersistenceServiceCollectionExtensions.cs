using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Application.Inventory;
using EFactura.Application.Organizations;
using EFactura.Application.Parties;
using EFactura.Application.Payments;
using EFactura.Application.Receivables;
using EFactura.Application.Sales;
using EFactura.Application.Taxation;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Infrastructure.Fiscal;
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

        services.AddScoped<EfOrganizationRepository>();
        services.AddScoped<ICompanyFiscalProfileRepository>(sp => sp.GetRequiredService<EfOrganizationRepository>());
        services.AddScoped<IFiscalLocationRepository>(sp => sp.GetRequiredService<EfOrganizationRepository>());
        services.AddScoped<GetCurrentCompanyUseCase>();
        services.AddScoped<UpsertCompanyFiscalProfileUseCase>();
        services.AddScoped<ListFiscalLocationsUseCase>();
        services.AddScoped<GetFiscalLocationUseCase>();
        services.AddScoped<CreateFiscalLocationUseCase>();
        services.AddScoped<UpdateFiscalLocationUseCase>();

        services.AddScoped<EfPartyRepository>();
        services.AddScoped<IPartyRepository>(sp => sp.GetRequiredService<EfPartyRepository>());
        services.AddScoped<IPartyMaintenanceRepository>(sp => sp.GetRequiredService<EfPartyRepository>());
        services.AddScoped<ListPartiesWithChannelsUseCase>();
        services.AddScoped<GetPartyWithChannelsUseCase>();
        services.AddScoped<UpdatePartyWithChannelsUseCase>();

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

        services.AddScoped<ICaeRepository, EfCaeRepository>();
        services.AddSingleton<ICaeArtifactVerifier, Release1CaeMetadataVerifier>();
        services.AddScoped<IFiscalNumberAllocator, FiscalNumberAllocator>();
        services.AddScoped<IFiscalizationRequestRepository, EfFiscalizationRequestRepository>();
        services.AddScoped<IFiscalDocumentRepository, EfFiscalDocumentRepository>();
        services.AddScoped<IFiscalContentSnapshotRepository, EfFiscalContentSnapshotRepository>();
        services.AddScoped<IFiscalContentSnapshotFactory, FiscalContentSnapshotFactory>();
        services.AddSingleton<IFiscalXmlBuilder, DeterministicUnsignedCfeBuilder>();
        services.AddSingleton<IFiscalSigningPayloadBuilder, DeterministicFiscalSigningPayloadBuilder>();
        services.AddSingleton<IFiscalSignedCfeSchemaValidator, DgiFeV1_44_2SignedCfeSchemaValidator>();
        services.AddSingleton<IFiscalDailyReportXmlBuilder, DeterministicUnsignedDailyReportXmlBuilder>();
        services.AddSingleton<IFiscalDailyReportUnsignedSchemaValidator, DgiFeV1_44_2UnsignedDailyReportSchemaValidator>();
        services.AddSingleton<IFiscalDailyReportSignedSchemaValidator, DgiFeV1_44_2SignedDailyReportSchemaValidator>();
        services.AddSingleton<IFiscalSigningTimeSource, UruguayFiscalSigningTimeSource>();
        services.AddSingleton<IFiscalSigningCertificateSource, PfxFiscalSigningCertificateSource>();
        services.AddSingleton<IFiscalDailyReportSigningCertificateSource>(sp =>
            (PfxFiscalSigningCertificateSource)sp.GetRequiredService<IFiscalSigningCertificateSource>());
        services.AddSingleton<IFiscalDailyReportTransportCertificateSource>(sp =>
            (PfxFiscalSigningCertificateSource)sp.GetRequiredService<IFiscalSigningCertificateSource>());
        services.AddSingleton<IFiscalSignatureProvider, XmlDsigFiscalSignatureProvider>();
        services.AddSingleton<IFiscalDailyReportSignatureProvider, XmlDsigFiscalDailyReportSignatureProvider>();
        services.AddSingleton<IFiscalDailyReportTransportClock, SystemFiscalDailyReportTransportClock>();
        services.AddSingleton<IFiscalDailyReportTransportGateway, DgiWsSecurityFiscalDailyReportTransportGateway>();
        services.AddSingleton<IFiscalDailyReportResponseConsultationGateway, DgiWsSecurityFiscalDailyReportResponseConsultationGateway>();
        services.AddSingleton<IFiscalDailyReportReceiverDiscoveryGateway, DgiWsSecurityFiscalDailyReportReceiverDiscoveryGateway>();
        services.AddSingleton<IFiscalDailyReportBrAckEvidenceParser, DgiFiscalDailyReportBrAckEvidenceParser>();
        services.AddScoped<IFiscalSigningEvidenceRepository, EfFiscalSigningEvidenceRepository>();
        services.AddScoped<IFiscalSignedArtifactRepository, EfFiscalSignedArtifactRepository>();
        services.AddScoped<IFiscalDailyReportVersionRepository, EfFiscalDailyReportVersionRepository>();
        services.AddScoped<IFiscalDailyReportSigningEvidenceRepository, EfFiscalDailyReportSigningEvidenceRepository>();
        services.AddScoped<IFiscalDailyReportSignedArtifactRepository, EfFiscalDailyReportSignedArtifactRepository>();
        services.AddScoped<IFiscalDailyReportSubmissionRepository, EfFiscalDailyReportSubmissionRepository>();
        services.AddScoped<EfFiscalDailyReportBrCorrectionRepository>();
        services.AddScoped<IFiscalDailyReportBrCorrectionRepository>(sp =>
            sp.GetRequiredService<EfFiscalDailyReportBrCorrectionRepository>());
        services.AddScoped<IFiscalDailyReportSameSequenceReceiptReader>(sp =>
            sp.GetRequiredService<EfFiscalDailyReportBrCorrectionRepository>());
        services.AddScoped<EfFiscalDailyReportResponseConsultationRepository>();
        services.AddScoped<IFiscalDailyReportResponseConsultationRepository>(sp =>
            sp.GetRequiredService<EfFiscalDailyReportResponseConsultationRepository>());
        services.AddScoped<IFiscalDailyReportConsultationTargetReader>(sp =>
            sp.GetRequiredService<EfFiscalDailyReportResponseConsultationRepository>());
        services.AddScoped<EfFiscalDailyReportReceiverDiscoveryRepository>();
        services.AddScoped<IFiscalDailyReportReceiverDiscoveryRepository>(sp =>
            sp.GetRequiredService<EfFiscalDailyReportReceiverDiscoveryRepository>());
        services.AddScoped<IFiscalDailyReportReceiverDiscoveryTargetReader>(sp =>
            sp.GetRequiredService<EfFiscalDailyReportReceiverDiscoveryRepository>());
        services.AddScoped<EfFiscalDailyReportLaterStateObservationRepository>();
        services.AddScoped<IFiscalDailyReportLaterStateObservationRepository>(sp =>
            sp.GetRequiredService<EfFiscalDailyReportLaterStateObservationRepository>());
        services.AddScoped<IFiscalDailyReportLatestObservationReader>(sp =>
            sp.GetRequiredService<EfFiscalDailyReportLaterStateObservationRepository>());
        services.AddScoped<PrepareFiscalDocumentIdentityUseCase>();
        services.AddScoped<CreateFiscalContentSnapshotUseCase>();
        services.AddScoped<PrepareFiscalSigningEvidenceUseCase>();
        services.AddScoped<SignFiscalDocumentUseCase>();
        services.AddScoped<AllocateFiscalDailyReportVersionUseCase>();
        services.AddScoped<PrepareFiscalDailyReportSigningEvidenceUseCase>();
        services.AddScoped<SignFiscalDailyReportUseCase>();
        services.AddScoped<PrepareFiscalDailyReportSubmissionUseCase>();
        services.AddScoped<DispatchFiscalDailyReportSubmissionUseCase>();
        services.AddScoped<PrepareFiscalDailyReportBrCorrectionUseCase>();
        services.AddScoped<DispatchFiscalDailyReportBrCorrectionUseCase>();
        services.AddScoped<ConsultFiscalDailyReportResponseUseCase>();
        services.AddScoped<DiscoverFiscalDailyReportReceiverUseCase>();
        services.AddScoped<ObserveFiscalDailyReportLaterStateUseCase>();
        services.AddScoped<AssessFiscalDailyReportReconciliationUseCase>();
        services.AddScoped<ListCaeAuthorizationsUseCase>();
        services.AddScoped<GetCaeAuthorizationUseCase>();
        services.AddScoped<ImportCaeAuthorizationUseCase>();
        services.AddScoped<ActivateCaeAuthorizationUseCase>();
        services.AddScoped<ListCaeAllocationsUseCase>();
        services.AddScoped<CreateCaeAllocationUseCase>();
        services.AddScoped<CloseCaeAllocationUseCase>();

        services.AddScoped<IInventoryRepository, EfInventoryRepository>();
        services.AddScoped<IInventoryAvailabilityChecker, InventoryAvailabilityChecker>();
        services.AddScoped<SaleStockConsumer>();
        services.AddScoped<ListInventoryPositionsUseCase>();
        services.AddScoped<GetInventoryPositionUseCase>();
        services.AddScoped<ListStockMovementsUseCase>();
        services.AddScoped<CreateStockAdjustmentUseCase>();

        services.AddScoped<IPaymentMethodRepository, EfPaymentMethodRepository>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IReceivableRepository, EfReceivableRepository>();

        services.AddScoped<ISaleRepository, EfSaleRepository>();
        services.AddScoped<SaleDraftBuilder>();
        services.AddScoped<CreateSaleUseCase>();
        services.AddScoped<UpdateSaleDraftUseCase>();
        services.AddScoped<GetSaleUseCase>();
        services.AddScoped<ListSalesUseCase>();
        services.AddScoped<IUiAmountConverter, Release1UiAmountConverter>();
        services.AddScoped<GetSaleFiscalPreviewUseCase>();
        services.AddScoped<ValidateSaleUseCase>();
        services.AddSingleton<SaleConfirmationPlanner>();
        services.AddSingleton<SaleSettlementPlanner>();
        services.AddScoped<ISaleConfirmationEvidenceResolver, SaleConfirmationEvidenceResolver>();
        services.AddScoped<ConfirmSaleUseCase>();

        return services;
    }
}
