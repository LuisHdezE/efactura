using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportReconciliationPolicyTests
{
    [Theory]
    [InlineData(
        FiscalDailyReportLaterState.Processed,
        "DR",
        FiscalDailyReportReconciliationDisposition.Consistent,
        "dgi_processed_no_inconsistencies",
        false)]
    [InlineData(
        FiscalDailyReportLaterState.InManagement,
        "ER",
        FiscalDailyReportReconciliationDisposition.ManualReviewRequired,
        "dgi_inconsistencies_require_analysis",
        true)]
    [InlineData(
        FiscalDailyReportLaterState.Reliquidated,
        "FR",
        FiscalDailyReportReconciliationDisposition.ReliquidatedExternally,
        "dgi_prior_report_reliquidated",
        false)]
    public async Task Latest_observation_maps_to_bounded_reconciliation_disposition(
        FiscalDailyReportLaterState state,
        string stateCode,
        FiscalDailyReportReconciliationDisposition expectedDisposition,
        string expectedInterpretation,
        bool expectedManualReview)
    {
        var observation = Observation(state, stateCode);
        var useCase = new AssessFiscalDailyReportReconciliationUseCase(new FakeReader(observation));

        var result = await useCase.ExecuteAsync(new(
            observation.OrganizationId,
            observation.DgiReceiverId));

        Assert.Equal(observation.Id, result.ObservationId);
        Assert.Equal(expectedDisposition, result.Disposition);
        Assert.Equal(expectedInterpretation, result.InterpretationCode);
        Assert.Equal(expectedManualReview, result.RequiresManualReview);
        Assert.False(result.AutomaticReliquidationAuthorized);
        Assert.False(result.AutomaticLocalMutationAuthorized);
    }

    [Fact]
    public async Task ER_requires_analysis_and_never_authorizes_automatic_reliquidation()
    {
        var observation = Observation(FiscalDailyReportLaterState.InManagement, "ER");
        var useCase = new AssessFiscalDailyReportReconciliationUseCase(new FakeReader(observation));

        var result = await useCase.ExecuteAsync(new(
            observation.OrganizationId,
            observation.DgiReceiverId));

        Assert.Equal(FiscalDailyReportReconciliationDisposition.ManualReviewRequired, result.Disposition);
        Assert.True(result.RequiresManualReview);
        Assert.False(result.AutomaticReliquidationAuthorized);
        Assert.False(result.AutomaticLocalMutationAuthorized);
    }

    [Fact]
    public async Task Missing_later_state_evidence_fails_closed()
    {
        var useCase = new AssessFiscalDailyReportReconciliationUseCase(new FakeReader());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new("company-policy", "receiver-missing")));

        Assert.Equal("fiscal.daily_report.reconciliation.observation_required", error.Code);
    }

    [Fact]
    public async Task Invalid_persisted_state_fails_closed()
    {
        var observation = Observation((FiscalDailyReportLaterState)99, "ZZ");
        var useCase = new AssessFiscalDailyReportReconciliationUseCase(new FakeReader(observation));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new(observation.OrganizationId, observation.DgiReceiverId)));

        Assert.Equal("fiscal.daily_report.reconciliation.unsupported_observation", error.Code);
    }

    [Fact]
    public async Task Persisted_state_and_original_DGI_code_must_agree()
    {
        var observation = Observation(FiscalDailyReportLaterState.Processed, "ER");
        var useCase = new AssessFiscalDailyReportReconciliationUseCase(new FakeReader(observation));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new(observation.OrganizationId, observation.DgiReceiverId)));

        Assert.Equal("fiscal.daily_report.reconciliation.state_code_mismatch", error.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Persisted_observation_must_reference_exactly_one_local_target(bool bothTargets)
    {
        var observation = Observation(FiscalDailyReportLaterState.Processed, "DR") with
        {
            RootSubmissionId = bothTargets ? Guid.NewGuid() : null,
            BrCorrectionRevisionId = bothTargets ? Guid.NewGuid() : null
        };
        var useCase = new AssessFiscalDailyReportReconciliationUseCase(new FakeReader(observation));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new(observation.OrganizationId, observation.DgiReceiverId)));

        Assert.Equal("fiscal.daily_report.reconciliation.target_invalid", error.Code);
    }

    [Fact]
    public async Task Root_and_BR_revision_targets_are_preserved_without_mutation()
    {
        var root = Observation(FiscalDailyReportLaterState.Processed, "DR");
        var correction = root with
        {
            Id = Guid.NewGuid(),
            RootSubmissionId = null,
            BrCorrectionRevisionId = Guid.NewGuid(),
            LocalRevision = 2,
            State = FiscalDailyReportLaterState.Reliquidated,
            DgiStateCode = "FR"
        };

        var rootResult = await new AssessFiscalDailyReportReconciliationUseCase(new FakeReader(root))
            .ExecuteAsync(new(root.OrganizationId, root.DgiReceiverId));
        var correctionResult = await new AssessFiscalDailyReportReconciliationUseCase(new FakeReader(correction))
            .ExecuteAsync(new(correction.OrganizationId, correction.DgiReceiverId));

        Assert.Equal(FiscalDailyReportConsultationTargetKind.RootSubmission, rootResult.TargetKind);
        Assert.Equal(root.RootSubmissionId!.Value, rootResult.TargetId);
        Assert.Equal(FiscalDailyReportConsultationTargetKind.BrCorrectionRevision, correctionResult.TargetKind);
        Assert.Equal(correction.BrCorrectionRevisionId!.Value, correctionResult.TargetId);
        Assert.Equal(2, correctionResult.LocalRevision);
    }

    private static StoredFiscalDailyReportLaterStateObservation Observation(
        FiscalDailyReportLaterState state,
        string stateCode) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "company-policy",
            "214748364700",
            new DateOnly(2026, 9, 12),
            1,
            null,
            "policy-source-op",
            "emitter-policy",
            "receiver-policy",
            state,
            stateCode,
            "2026-09-13T01:00:00-03:00",
            "<Ackconsultaenviosreporte />",
            new string('a', 64),
            new DateTimeOffset(2026, 9, 13, 2, 0, 0, TimeSpan.Zero));

    private sealed class FakeReader(params StoredFiscalDailyReportLaterStateObservation[] values) :
        IFiscalDailyReportLatestObservationReader
    {
        private readonly IReadOnlyList<StoredFiscalDailyReportLaterStateObservation> _values = values;

        public Task<StoredFiscalDailyReportLaterStateObservation?> GetLatestByReceiverIdAsync(
            string organizationId,
            string dgiReceiverId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values
                .Where(x => x.OrganizationId == organizationId && x.DgiReceiverId == dgiReceiverId)
                .OrderByDescending(x => x.ObservedAtUtc)
                .FirstOrDefault());
    }
}
