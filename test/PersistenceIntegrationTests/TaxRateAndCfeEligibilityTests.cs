using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Fiscal;
using EFactura.Application.Taxation;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class TaxRateAndCfeEligibilityTests
{
    private static readonly DateOnly EffectiveOn = new(2026, 8, 29);

    [Fact]
    public async Task Domestic_basic_profile_resolves_authoritative_22_percent()
    {
        var profile = Profile("VAT_BASIC", 22m);
        var useCase = RateUseCase(profile);

        var result = await useCase.ExecuteAsync(new ResolveTaxRateRequest(
            "company-1",
            EffectiveOn,
            DomesticDecision(),
            profile.Id));

        Assert.Equal(TaxRateResolutionStatus.Resolved, result.Status);
        Assert.Equal(VatLiabilityKind.VatDue, result.Liability);
        Assert.Equal(VatRateKind.Basic, result.RateKind);
        Assert.Equal(22m, result.AppliedRatePercent);
    }

    [Fact]
    public async Task Domestic_minimum_profile_resolves_authoritative_10_percent()
    {
        var profile = Profile("VAT_MINIMUM", 10m);
        var result = await RateUseCase(profile).ExecuteAsync(new ResolveTaxRateRequest(
            "company-1",
            EffectiveOn,
            DomesticDecision(),
            profile.Id));

        Assert.Equal(TaxRateResolutionStatus.Resolved, result.Status);
        Assert.Equal(VatRateKind.Minimum, result.RateKind);
        Assert.Equal(10m, result.AppliedRatePercent);
    }

    [Fact]
    public async Task Domestic_basic_profile_with_wrong_rate_fails_closed()
    {
        var profile = Profile("VAT_BASIC", 10m);
        var result = await RateUseCase(profile).ExecuteAsync(new ResolveTaxRateRequest(
            "company-1",
            EffectiveOn,
            DomesticDecision(),
            profile.Id));

        Assert.Equal(TaxRateResolutionStatus.RequiresReview, result.Status);
        Assert.Contains("tax.rate.profile_rate_mismatch", result.Reasons);
        Assert.Null(result.AppliedRatePercent);
    }

    [Fact]
    public async Task Domestic_exempt_profile_requires_specific_exemption_rule()
    {
        var profile = Profile("VAT_EXEMPT", 0m);
        var result = await RateUseCase(profile).ExecuteAsync(new ResolveTaxRateRequest(
            "company-1",
            EffectiveOn,
            DomesticDecision(),
            profile.Id));

        Assert.Equal(TaxRateResolutionStatus.RequiresReview, result.Status);
        Assert.Equal(VatRateKind.Exempt, result.RateKind);
        Assert.Contains("specific_effective_exemption_rule", result.MissingFacts);
    }

    [Fact]
    public async Task Export_goods_has_no_vat_due_and_zero_is_only_computational()
    {
        var result = await RateUseCase(null).ExecuteAsync(new ResolveTaxRateRequest(
            "company-1",
            EffectiveOn,
            ExportGoodsDecision(),
            null));

        Assert.Equal(TaxRateResolutionStatus.Resolved, result.Status);
        Assert.Equal(VatLiabilityKind.NoVatDue, result.Liability);
        Assert.Equal(VatRateKind.Export, result.RateKind);
        Assert.Equal(0m, result.AppliedRatePercent);
        Assert.Contains("tax.rate.zero_is_computational_not_zero_rate_vat", result.Reasons);
    }

    [Fact]
    public async Task Cfe_domestic_taxpayer_invoice_is_eligible_with_uruguayan_ruc()
    {
        var result = await CfeUseCase().ExecuteAsync(new PrepareCfeEligibilityRequest(
            EffectiveOn,
            DomesticDecision(),
            ReceiverWithUruguayanRuc(),
            FiscalOperationIntent.TaxpayerInvoice,
            100m,
            false));

        Assert.Equal(CfeEligibilityStatus.EligibleCandidateSet, result.Status);
        Assert.Single(result.Candidates);
        Assert.Equal(CfeFamily.EFactura, result.Candidates.Single().Family);
    }

    [Fact]
    public async Task Cfe_domestic_taxpayer_invoice_without_uruguayan_ruc_is_ineligible()
    {
        var result = await CfeUseCase().ExecuteAsync(new PrepareCfeEligibilityRequest(
            EffectiveOn,
            DomesticDecision(),
            ForeignReceiver(new ReceiverFiscalIdentityFact("6", "AR")),
            FiscalOperationIntent.TaxpayerInvoice,
            100m,
            false));

        Assert.Equal(CfeEligibilityStatus.Ineligible, result.Status);
        Assert.Contains("fiscal.efactura_requires_uruguayan_ruc", result.Reasons);
    }

    [Fact]
    public async Task Cfe_eticket_over_5000_ui_requires_receiver_identity()
    {
        var result = await CfeUseCase().ExecuteAsync(new PrepareCfeEligibilityRequest(
            EffectiveOn,
            DomesticDecision(),
            ForeignReceiver(),
            FiscalOperationIntent.ConsumerFinal,
            5000.01m,
            false));

        Assert.Equal(CfeEligibilityStatus.RequiresReview, result.Status);
        Assert.Contains("format_compatible_receiver_identity", result.MissingFacts);
    }

    [Fact]
    public async Task Cfe_eticket_accepts_argentine_dni_when_identification_is_required()
    {
        var result = await CfeUseCase().ExecuteAsync(new PrepareCfeEligibilityRequest(
            EffectiveOn,
            DomesticDecision(),
            ForeignReceiver(new ReceiverFiscalIdentityFact("6", "AR")),
            FiscalOperationIntent.ConsumerFinal,
            6000m,
            false));

        Assert.Equal(CfeEligibilityStatus.EligibleCandidateSet, result.Status);
        Assert.Equal(CfeFamily.ETicket, result.Candidates.Single().Family);
        Assert.Equal(ReceiverIdentificationRequirement.Required, result.Candidates.Single().ReceiverIdentification);
    }

    [Fact]
    public async Task Cfe_eticket_rejects_dni_type_from_country_not_allowed_by_format()
    {
        var result = await CfeUseCase().ExecuteAsync(new PrepareCfeEligibilityRequest(
            EffectiveOn,
            DomesticDecision(),
            ForeignReceiver(new ReceiverFiscalIdentityFact("6", "US")),
            FiscalOperationIntent.ConsumerFinal,
            6000m,
            false));

        Assert.Equal(CfeEligibilityStatus.RequiresReview, result.Status);
        Assert.Contains("format_compatible_receiver_identity", result.MissingFacts);
    }

    [Fact]
    public async Task Cfe_export_goods_prepares_export_invoice_candidate()
    {
        var result = await CfeUseCase().ExecuteAsync(new PrepareCfeEligibilityRequest(
            EffectiveOn,
            ExportGoodsDecision(),
            ForeignReceiver(new ReceiverFiscalIdentityFact("7", "AR")),
            FiscalOperationIntent.Export,
            2000m,
            false));

        Assert.Equal(CfeEligibilityStatus.EligibleCandidateSet, result.Status);
        Assert.Equal(CfeFamily.EFacturaExportacion, result.Candidates.Single().Family);
    }

    [Fact]
    public async Task Cfe_export_services_remains_review_until_latest_faq_strategy_is_revalidated()
    {
        var result = await CfeUseCase().ExecuteAsync(new PrepareCfeEligibilityRequest(
            EffectiveOn,
            ExportServicesDecision(),
            ReceiverWithUruguayanRuc(),
            FiscalOperationIntent.Export,
            2000m,
            false));

        Assert.Equal(CfeEligibilityStatus.RequiresReview, result.Status);
        Assert.Contains("current_export_service_cfe_strategy_confirmation", result.MissingFacts);
        Assert.Contains(result.Candidates, candidate => candidate.Family == CfeFamily.EFacturaExportacion);
        Assert.Contains(result.Candidates, candidate => candidate.Family == CfeFamily.EFactura);
    }

    [Fact]
    public async Task Cfe_25_2_provider_refuses_dates_before_production_support_boundary()
    {
        var provider = new UruguayCfe25_2EligibilityRulePackProvider();

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            provider.GetAsync(new DateOnly(2026, 6, 29)));

        Assert.Equal("fiscal.cfe_format_date_unsupported", error.Code);
    }

    private static ResolveTaxRateUseCase RateUseCase(TaxProfile? profile) =>
        new(new FakeTaxProfileRepository(profile), new UruguayRelease1VatRateRulePackProvider());

    private static PrepareCfeEligibilityUseCase CfeUseCase() =>
        new(new UruguayCfe25_2EligibilityRulePackProvider(), new CfeEligibilityPolicy());

    private static TaxProfile Profile(string treatmentCode, decimal rate) =>
        TaxProfile.Create(
            Guid.NewGuid(),
            "company-1",
            $"PROFILE-{treatmentCode}",
            treatmentCode,
            treatmentCode,
            rate,
            new DateOnly(2026, 1, 1),
            null,
            "DGI/IMPO verified profile",
            "https://www.impo.com.uy/bases/todgi2023/101-2024/34_T10",
            "reviewed 2026-08-29");

    private static TaxTreatmentDecision DomesticDecision() =>
        Decision(TaxTreatmentClassification.Domestic, "DOMESTIC");

    private static TaxTreatmentDecision ExportGoodsDecision() =>
        Decision(TaxTreatmentClassification.ExportGoods, "EXPORT_GOODS");

    private static TaxTreatmentDecision ExportServicesDecision() =>
        Decision(TaxTreatmentClassification.ExportServices, "EXPORT_SERVICES");

    private static TaxTreatmentDecision Decision(TaxTreatmentClassification classification, string code) =>
        new(
            TaxDecisionStatus.Resolved,
            classification,
            code,
            new[] { "test.reason" },
            Array.Empty<string>(),
            new[]
            {
                new RegulatoryRuleEvidence(
                    "TEST-RULE",
                    "Test regulatory evidence",
                    "https://example.invalid/regulatory-evidence",
                    "test",
                    new DateOnly(2024, 5, 16))
            },
            "test-pack");

    private static ReceiverTaxFacts ReceiverWithUruguayanRuc() =>
        new("UY", "UY", new[] { new ReceiverFiscalIdentityFact("2", "UY") });

    private static ReceiverTaxFacts ForeignReceiver(params ReceiverFiscalIdentityFact[] identities) =>
        new("AR", "AR", identities);

    private sealed class FakeTaxProfileRepository : ITaxProfileRepository
    {
        private readonly TaxProfile? _profile;

        public FakeTaxProfileRepository(TaxProfile? profile) => _profile = profile;

        public Task<TaxProfile?> GetAsync(
            string organizationId,
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _profile is not null
                && _profile.OrganizationId == organizationId
                && _profile.Id == profileId
                    ? _profile
                    : null);

        public Task<PageResult<TaxProfile>> SearchAsync(
            TaxProfileSearchRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(TaxProfile profile, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
