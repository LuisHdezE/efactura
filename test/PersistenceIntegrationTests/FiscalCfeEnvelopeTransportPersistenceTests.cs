using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalCfeEnvelopeTransportPersistenceTests
{
    private const string OrganizationId = "company-envelope-transport";
    private const string IssuerRuc = "214748364700";
    private const string ReceiverRut = "219999830019";
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 2, 10, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_persists_opaque_DGI_response_and_replays_without_second_network_call(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var envelope = await SeedEnvelopeAsync(database, 4101);
        await PrepareAsync(database, envelope, "sobre-transport-op-1");

        var gateway = new FakeGateway("<ACKSobre><opaque>evidence</opaque></ACKSobre>");
        FiscalCfeEnvelopeSubmissionResult first;
        FiscalCfeEnvelopeSubmissionResult replay;
        await using (var context = database.CreateContext())
        {
            var dispatcher = Dispatcher(context, gateway);
            first = await dispatcher.ExecuteAsync(Command(envelope));
            replay = await dispatcher.ExecuteAsync(Command(envelope));
        }

        Assert.Equal(FiscalCfeEnvelopeSubmissionState.ResponseReceived, first.State);
        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(1, gateway.CallCount);
        Assert.Equal(envelope.EnvelopeXml, gateway.LastRequest!.EnvelopeXml);
        Assert.Equal(envelope.EnvelopeSha256, gateway.LastRequest.EnvelopeSha256);
        Assert.Equal("<ACKSobre><opaque>evidence</opaque></ACKSobre>", first.ResponseXml);
        Assert.Equal(Sha256(first.ResponseXml!), first.ResponseSha256);

        await using var verify = database.CreateContext();
        var row = await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync();
        Assert.Equal((int)FiscalCfeEnvelopeSubmissionState.ResponseReceived, row.State);
        Assert.Equal(1, row.AttemptCount);
        Assert.Equal(first.ResponseXml, row.ResponseXml);
        Assert.Equal(first.ResponseSha256, row.ResponseSha256);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Ambiguous_delivery_becomes_Unknown_and_is_not_automatically_retried(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var envelope = await SeedEnvelopeAsync(database, 4102);
        await PrepareAsync(database, envelope, "sobre-transport-op-unknown");
        var gateway = new AmbiguousGateway();

        await using var context = database.CreateContext();
        var dispatcher = Dispatcher(context, gateway);
        var first = await dispatcher.ExecuteAsync(Command(envelope));
        Assert.Equal(FiscalCfeEnvelopeSubmissionState.Unknown, first.State);
        Assert.Equal("fiscal.envelope.transport.test_ambiguous", first.FailureCode);
        Assert.Equal(1, gateway.CallCount);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => dispatcher.ExecuteAsync(Command(envelope)));
        Assert.Equal("fiscal.envelope.transport.reconciliation_required", error.Code);
        Assert.Equal(1, gateway.CallCount);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_dispatchers_cross_network_boundary_only_once(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var envelope = await SeedEnvelopeAsync(database, 4103);
        await PrepareAsync(database, envelope, "sobre-transport-op-concurrent");
        var gateway = new CoordinatedGateway();

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstDispatcher = Dispatcher(firstContext, gateway);
        var secondDispatcher = Dispatcher(secondContext, gateway);

        var firstTask = firstDispatcher.ExecuteAsync(Command(envelope));
        await gateway.WaitUntilEnteredAsync();

        try
        {
            var secondError = await Assert.ThrowsAsync<ApplicationProblemException>(() => secondDispatcher.ExecuteAsync(Command(envelope)));
            Assert.Equal("fiscal.envelope.transport.reconciliation_required", secondError.Code);
            Assert.Equal(1, gateway.CallCount);
        }
        finally
        {
            gateway.Release();
        }

        var first = await firstTask;
        Assert.Equal(FiscalCfeEnvelopeSubmissionState.ResponseReceived, first.State);
        Assert.Equal(1, gateway.CallCount);

        await using var verify = database.CreateContext();
        var row = await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync();
        Assert.Equal(1, row.AttemptCount);
        Assert.Equal((int)FiscalCfeEnvelopeSubmissionState.ResponseReceived, row.State);
    }

    private static async Task<StoredFiscalCfeEnvelope> SeedEnvelopeAsync(TestDatabase database, long senderEnvelopeId)
    {
        var documentId = Guid.NewGuid();
        var xml = $"<EnvioCFE xmlns=\"http://cfe.dgi.gub.uy\" version=\"1.0\"><Caratula version=\"1.0\"><Idemisor>{senderEnvelopeId}</Idemisor></Caratula></EnvioCFE>";
        var id = Guid.NewGuid();
        await using var context = database.CreateContext();
        context.Set<V1FiscalCfeEnvelopeRecord>().Add(new V1FiscalCfeEnvelopeRecord
        {
            Id = id,
            OrganizationId = OrganizationId,
            ReceiverRut = ReceiverRut,
            IssuerRuc = IssuerRuc,
            SenderEnvelopeId = senderEnvelopeId,
            CreatedAtUtc = Now,
            CreatedAtOffsetMinutes = 0,
            FiscalDocumentIdsJson = JsonSerializer.Serialize(new[] { documentId }),
            OperationId = $"persist-{senderEnvelopeId}",
            CfeCount = 1,
            CertificateThumbprint = "thumb-transport",
            CertificateSerialNumber = "serial-transport",
            EnvelopeXml = xml,
            EnvelopeSha256 = Sha256(xml),
            SchemaSetId = "dgi-fe-envelope-xsd-v1.44.2",
            SchemaVersion = "1.44.2",
            SchemaSetFingerprint = new string('a', 64)
        });
        await context.SaveChangesAsync();
        return (await new EfFiscalCfeEnvelopeRepository(context).GetByIdentityAsync(
            OrganizationId, IssuerRuc, ReceiverRut, senderEnvelopeId))!;
    }

    private static async Task PrepareAsync(TestDatabase database, StoredFiscalCfeEnvelope envelope, string operationId)
    {
        await using var context = database.CreateContext();
        var useCase = new PrepareFiscalCfeEnvelopeSubmissionUseCase(
            new EfFiscalCfeEnvelopeRepository(context),
            new EfFiscalCfeEnvelopeSubmissionRepository(context),
            new EfFiscalCfeEnvelopePersistenceConflictClassifier(),
            new FixedClock(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context));
        var result = await useCase.ExecuteAsync(new(
            envelope.OrganizationId,
            envelope.IssuerRuc,
            envelope.ReceiverRut,
            envelope.SenderEnvelopeId,
            operationId));
        Assert.Equal(FiscalCfeEnvelopeSubmissionState.Prepared, result.State);
    }

    private static DispatchFiscalCfeEnvelopeSubmissionUseCase Dispatcher(
        V1PersistenceDbContext context,
        IFiscalCfeEnvelopeTransportGateway gateway) =>
        new(
            new EfFiscalCfeEnvelopeRepository(context),
            new EfFiscalCfeEnvelopeSubmissionRepository(context),
            gateway,
            new FixedClock(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context));

    private static DispatchFiscalCfeEnvelopeSubmissionCommand Command(StoredFiscalCfeEnvelope envelope) =>
        new(envelope.OrganizationId, envelope.IssuerRuc, envelope.ReceiverRut, envelope.SenderEnvelopeId);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedClock : IFiscalCfeEnvelopeTransportClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeGateway(string responseXml) : IFiscalCfeEnvelopeTransportGateway
    {
        public int CallCount { get; private set; }
        public FiscalCfeEnvelopeTransportRequest? LastRequest { get; private set; }

        public Task<FiscalCfeEnvelopeTransportResponse> SendAsync(
            FiscalCfeEnvelopeTransportRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(new FiscalCfeEnvelopeTransportResponse(responseXml));
        }
    }

    private sealed class AmbiguousGateway : IFiscalCfeEnvelopeTransportGateway
    {
        public int CallCount { get; private set; }

        public Task<FiscalCfeEnvelopeTransportResponse> SendAsync(
            FiscalCfeEnvelopeTransportRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            throw new FiscalCfeEnvelopeTransportException(
                "fiscal.envelope.transport.test_ambiguous",
                "test ambiguous delivery",
                deliveryAmbiguous: true);
        }
    }

    private sealed class CoordinatedGateway : IFiscalCfeEnvelopeTransportGateway
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public async Task<FiscalCfeEnvelopeTransportResponse> SendAsync(
            FiscalCfeEnvelopeTransportRequest request,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new FiscalCfeEnvelopeTransportResponse("<ACKSobre><opaque>concurrent</opaque></ACKSobre>");
        }

        public Task WaitUntilEnteredAsync() => _entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
        public void Release() => _release.TrySetResult();
    }
}