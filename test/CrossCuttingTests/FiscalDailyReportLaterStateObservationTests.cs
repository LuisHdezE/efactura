using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportLaterStateObservationTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);

    [Theory]
    [InlineData("DR", FiscalDailyReportLaterState.Processed)]
    [InlineData("ER", FiscalDailyReportLaterState.InManagement)]
    [InlineData("FR", FiscalDailyReportLaterState.Reliquidated)]
    public async Task Known_receiver_persists_governed_later_state_and_replays_without_network(
        string stateCode,
        FiscalDailyReportLaterState expected)
    {
        var target = RootTarget("receiver-root", "AR");
        var repository = new ObservationRepository();
        var gateway = new FakeGateway(Response(
            Item("emitter-1", "other-receiver", "AR"),
            Item("emitter-1", "receiver-root", stateCode)));
        var useCase = UseCase(new TargetReader(target), repository, gateway);

        var first = await useCase.ExecuteAsync(Command("receiver-root", "later-op-1"));
        var replay = await useCase.ExecuteAsync(Command("receiver-root", "later-op-1"));

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(expected, first.State);
        Assert.Equal(stateCode, first.DgiStateCode);
        Assert.Equal(target.TargetId, first.TargetId);
        Assert.Equal(64, first.EvidenceXmlHash.Length);
        Assert.Equal(1, gateway.Calls);
        Assert.Single(repository.Values);
        Assert.Equal("AR", target.ImmediateAckStateCode);
    }

    [Fact]
    public async Task Known_BR_correction_receiver_can_store_ER_without_rewriting_original_ACK_evidence()
    {
        var target = CorrectionTarget("receiver-correction", "AR", 2);
        var repository = new ObservationRepository();
        var useCase = UseCase(
            new TargetReader(target),
            repository,
            new FakeGateway(Response(Item("emitter-br", "receiver-correction", "ER"))));

        var result = await useCase.ExecuteAsync(Command("receiver-correction", "later-br"));

        Assert.Equal(FiscalDailyReportConsultationTargetKind.BrCorrectionRevision, result.TargetKind);
        Assert.Equal(2, result.LocalRevision);
        Assert.Equal(FiscalDailyReportLaterState.InManagement, result.State);
        var stored = Assert.Single(repository.Values);
        Assert.Null(stored.RootSubmissionId);
        Assert.Equal(target.TargetId, stored.BrCorrectionRevisionId);
        Assert.Equal("AR", target.ImmediateAckStateCode);
    }

    [Theory]
    [InlineData("AR")]
    [InlineData("BR")]
    public async Task Immediate_states_are_not_persisted_as_later_state_observations(string stateCode)
    {
        var target = RootTarget("receiver-immediate", stateCode);
        var repository = new ObservationRepository();
        var useCase = UseCase(
            new TargetReader(target),
            repository,
            new FakeGateway(Response(Item("emitter", "receiver-immediate", stateCode))));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("receiver-immediate", "later-immediate")));

        Assert.Equal("fiscal.daily_report.later_state.not_available", error.Code);
        Assert.Empty(repository.Values);
    }

    [Fact]
    public async Task Unsupported_DGI_state_fails_closed_without_persisting()
    {
        var repository = new ObservationRepository();
        var useCase = UseCase(
            new TargetReader(RootTarget("receiver-unsupported", "AR")),
            repository,
            new FakeGateway(Response(Item("emitter", "receiver-unsupported", "ZZ"))));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("receiver-unsupported", "later-unsupported")));

        Assert.Equal("fiscal.daily_report.later_state.unsupported_state", error.Code);
        Assert.Empty(repository.Values);
    }

    [Fact]
    public async Task Unknown_local_receiver_fails_before_network()
    {
        var gateway = new FakeGateway(Response(Item("emitter", "unused", "DR")));
        var useCase = UseCase(new TargetReader(), new ObservationRepository(), gateway);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("missing-receiver", "later-missing")));

        Assert.Equal("fiscal.daily_report.later_state.receiver_not_known", error.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Ambiguous_local_receiver_fails_before_network()
    {
        var gateway = new FakeGateway(Response(Item("emitter", "shared", "DR")));
        var useCase = UseCase(
            new TargetReader(RootTarget("shared", "AR"), CorrectionTarget("shared", "AR", 2)),
            new ObservationRepository(),
            gateway);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("shared", "later-ambiguous-local")));

        Assert.Equal("fiscal.daily_report.later_state.receiver_ambiguous", error.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task DGI_must_return_exactly_one_row_for_the_requested_receiver()
    {
        var target = RootTarget("receiver-exact", "AR");
        var missingUseCase = UseCase(
            new TargetReader(target),
            new ObservationRepository(),
            new FakeGateway(Response(Item("emitter", "other", "DR"))));

        var missing = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            missingUseCase.ExecuteAsync(Command("receiver-exact", "later-no-row")));
        Assert.Equal("fiscal.daily_report.later_state.receiver_not_returned", missing.Code);

        var duplicateUseCase = UseCase(
            new TargetReader(target),
            new ObservationRepository(),
            new FakeGateway(Response(
                Item("emitter", "receiver-exact", "DR"),
                Item("emitter", "receiver-exact", "DR"))));

        var duplicate = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            duplicateUseCase.ExecuteAsync(Command("receiver-exact", "later-duplicate")));
        Assert.Equal("fiscal.daily_report.later_state.receiver_duplicate", duplicate.Code);
    }

    [Fact]
    public async Task Operation_id_reuse_for_another_receiver_fails_before_network()
    {
        var repository = new ObservationRepository();
        repository.Values.Add(Stored("receiver-a", "same-op", FiscalDailyReportLaterState.Processed, "DR"));
        var gateway = new FakeGateway(Response(Item("emitter", "receiver-b", "ER")));
        var useCase = UseCase(new TargetReader(RootTarget("receiver-b", "AR")), repository, gateway);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("receiver-b", "same-op")));

        Assert.Equal("fiscal.daily_report.later_state.operation_replay_mismatch", error.Code);
        Assert.Equal(0, gateway.Calls);
    }

    private static ObserveFiscalDailyReportLaterStateUseCase UseCase(
        IFiscalDailyReportConsultationTargetReader targets,
        IFiscalDailyReportLaterStateObservationRepository repository,
        IFiscalDailyReportReceiverDiscoveryGateway gateway) =>
        new(
            targets,
            repository,
            gateway,
            new FixedClock(new DateTimeOffset(2026, 9, 13, 1, 20, 0, TimeSpan.Zero)),
            new InlineTransactionManager(),
            new CountingUnitOfWork());

    private static ObserveFiscalDailyReportLaterStateCommand Command(string receiver, string operationId) =>
        new("company-later", receiver, operationId);

    private static FiscalDailyReportConsultationTarget RootTarget(string receiver, string? immediateState) =>
        new(
            FiscalDailyReportConsultationTargetKind.RootSubmission,
            Guid.NewGuid(),
            "company-later",
            "214748364700",
            SummaryDate,
            1,
            null,
            receiver,
            immediateState);

    private static FiscalDailyReportConsultationTarget CorrectionTarget(
        string receiver,
        string? immediateState,
        int revision) =>
        new(
            FiscalDailyReportConsultationTargetKind.BrCorrectionRevision,
            Guid.NewGuid(),
            "company-later",
            "214748364700",
            SummaryDate,
            1,
            revision,
            receiver,
            immediateState);

    private static FiscalDailyReportReceiverDiscoveryItem Item(
        string emitter,
        string receiver,
        string state) =>
        new(emitter, receiver, state, "2026-09-13T01:00:00-03:00");

    private static FiscalDailyReportReceiverDiscoveryResponse Response(
        params FiscalDailyReportReceiverDiscoveryItem[] items) =>
        new("<Ackconsultaenviosreporte><ColeccionDatosReporte /></Ackconsultaenviosreporte>", items);

    private static StoredFiscalDailyReportLaterStateObservation Stored(
        string receiver,
        string operationId,
        FiscalDailyReportLaterState state,
        string stateCode) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "company-later",
            "214748364700",
            SummaryDate,
            1,
            null,
            operationId,
            "emitter",
            receiver,
            state,
            stateCode,
            "2026-09-13T01:00:00-03:00",
            "<Ackconsultaenviosreporte />",
            new string('a', 64),
            new DateTimeOffset(2026, 9, 13, 1, 20, 0, TimeSpan.Zero));

    private sealed class TargetReader(params FiscalDailyReportConsultationTarget[] values) :
        IFiscalDailyReportConsultationTargetReader
    {
        private readonly IReadOnlyList<FiscalDailyReportConsultationTarget> _values = values;

        public Task<IReadOnlyList<FiscalDailyReportConsultationTarget>> FindByReceiverIdAsync(
            string organizationId,
            string dgiReceiverId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FiscalDailyReportConsultationTarget>>(
                _values
                    .Where(x => x.OrganizationId == organizationId && x.DgiReceiverId == dgiReceiverId)
                    .ToArray());
    }

    private sealed class ObservationRepository : IFiscalDailyReportLaterStateObservationRepository
    {
        public List<StoredFiscalDailyReportLaterStateObservation> Values { get; } = [];

        public Task<StoredFiscalDailyReportLaterStateObservation?> GetByOperationIdAsync(
            string organizationId,
            string operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x =>
                x.OrganizationId == organizationId && x.OperationId == operationId));

        public Task AddAsync(
            StoredFiscalDailyReportLaterStateObservation observation,
            CancellationToken cancellationToken = default)
        {
            Values.Add(observation);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGateway(params FiscalDailyReportReceiverDiscoveryResponse[] responses) :
        IFiscalDailyReportReceiverDiscoveryGateway
    {
        private readonly Queue<FiscalDailyReportReceiverDiscoveryResponse> _responses = new(responses);
        public int Calls { get; private set; }

        public Task<FiscalDailyReportReceiverDiscoveryResponse> QueryAsync(
            FiscalDailyReportReceiverDiscoveryRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IFiscalDailyReportTransportClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class InlineTransactionManager : ITransactionManager
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
