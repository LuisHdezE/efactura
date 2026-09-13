using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Infrastructure.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalCfeEnvelopeAckObservationPersistenceTests
{
    private const string OrganizationId = "company-envelope-ack";
    private const string IssuerRuc = "214748364700";
    private const string ReceiverRut = "219999830019";
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 6, 50, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_persists_AS_and_replays_without_mutating_transport(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedResponseReceivedAsync(database, 5101, Ack("AS", 5101, null, includeConsultation: true));

        FiscalCfeEnvelopeAckObservationResult first;
        FiscalCfeEnvelopeAckObservationResult replay;
        await using (var context = database.CreateContext())
        {
            var useCase = UseCase(context);
            first = await useCase.ExecuteAsync(Command(seed.Envelope));
            replay = await useCase.ExecuteAsync(Command(seed.Envelope));
        }

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.ObservationId, replay.ObservationId);
        Assert.Equal(FiscalCfeEnvelopeAckState.Received, first.State);
        Assert.Equal(5001, first.DgiResponseId);
        Assert.Equal(9001, first.DgiReceiverId);
        Assert.Equal("dGVzdC10b2tlbg==", first.ConsultationToken);
        Assert.Equal("2026-09-13T03:20:00", first.ConsultationAvailableAtText);
        Assert.Empty(first.RejectionReasons);

        await using var verify = database.CreateContext();
        var observations = await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().AsNoTracking().ToListAsync();
        Assert.Single(observations);
        var submission = await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync();
        Assert.Equal((int)FiscalCfeEnvelopeSubmissionState.ResponseReceived, submission.State);
        Assert.Equal(seed.ResponseXml, submission.ResponseXml);
        Assert.Equal(seed.ResponseSha256, submission.ResponseSha256);
        var envelope = await verify.Set<V1FiscalCfeEnvelopeRecord>().AsNoTracking().SingleAsync();
        Assert.Equal(seed.Envelope.EnvelopeXml, envelope.EnvelopeXml);
        Assert.Equal(seed.Envelope.EnvelopeSha256, envelope.EnvelopeSha256);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_persists_BS_S08_as_evidence_without_recovery_or_transport_mutation(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var response = Ack(
            "BS",
            5102,
            "<MotivosRechazo><Motivo>S08</Motivo><Glosa>Sobre enviado ya existe</Glosa><Detalle>Duplicado DGI</Detalle></MotivosRechazo>",
            includeConsultation: false);
        var seed = await SeedResponseReceivedAsync(database, 5102, response);

        await using var context = database.CreateContext();
        var result = await UseCase(context).ExecuteAsync(Command(seed.Envelope));

        Assert.Equal(FiscalCfeEnvelopeAckState.Rejected, result.State);
        var reason = Assert.Single(result.RejectionReasons);
        Assert.Equal("S08", reason.Code);
        Assert.Equal("Sobre enviado ya existe", reason.Glosa);
        Assert.Equal("Duplicado DGI", reason.Detail);

        await using var verify = database.CreateContext();
        var submission = await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync();
        Assert.Equal((int)FiscalCfeEnvelopeSubmissionState.ResponseReceived, submission.State);
        Assert.Equal(1, submission.AttemptCount);
        Assert.Equal(seed.ResponseXml, submission.ResponseXml);
        Assert.Equal(seed.ResponseSha256, submission.ResponseSha256);
        Assert.Single(await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_fails_closed_when_ACK_IdEmisor_does_not_correlate(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedResponseReceivedAsync(database, 5103, Ack("AS", 9999, null, includeConsultation: true));

        await using var context = database.CreateContext();
        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => UseCase(context).ExecuteAsync(Command(seed.Envelope)));

        Assert.Equal("fiscal.envelope.ack.correlation_mismatch", error.Code);
        await using var verify = database.CreateContext();
        Assert.Equal(0, await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().CountAsync());
        Assert.Equal((int)FiscalCfeEnvelopeSubmissionState.ResponseReceived,
            (await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync()).State);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_observers_converge_to_one_append_only_observation(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedResponseReceivedAsync(database, 5104, Ack("AS", 5104, null, includeConsultation: true));

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstTask = UseCase(firstContext).ExecuteAsync(Command(seed.Envelope));
        var secondTask = UseCase(secondContext).ExecuteAsync(Command(seed.Envelope));
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(results[0].ObservationId, results[1].ObservationId);
        Assert.Contains(results, x => !x.Replayed);

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().CountAsync());
        var submission = await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync();
        Assert.Equal((int)FiscalCfeEnvelopeSubmissionState.ResponseReceived, submission.State);
        Assert.Equal(seed.ResponseSha256, submission.ResponseSha256);
    }

    private static ObserveFiscalCfeEnvelopeAckUseCase UseCase(V1PersistenceDbContext context) =>
        new(
            new EfFiscalCfeEnvelopeRepository(context),
            new EfFiscalCfeEnvelopeSubmissionRepository(context),
            new EfFiscalCfeEnvelopeAckObservationRepository(context),
            new DgiFiscalCfeEnvelopeAckParser(),
            new EfFiscalCfeEnvelopePersistenceConflictClassifier(),
            new FixedClock(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context));

    private static ObserveFiscalCfeEnvelopeAckCommand Command(StoredFiscalCfeEnvelope envelope) =>
        new(envelope.OrganizationId, envelope.IssuerRuc, envelope.ReceiverRut, envelope.SenderEnvelopeId);

    private static async Task<SeededSource> SeedResponseReceivedAsync(
        TestDatabase database,
        long senderEnvelopeId,
        string responseXml)
    {
        var documentId = Guid.NewGuid();
        var envelopeXml = $"<EnvioCFE xmlns=\"http://cfe.dgi.gub.uy\" version=\"1.0\"><Caratula version=\"1.0\"><RUCEmisor>{IssuerRuc}</RUCEmisor><RutReceptor>{ReceiverRut}</RutReceptor><Idemisor>{senderEnvelopeId}</Idemisor><CantCFE>1</CantCFE></Caratula></EnvioCFE>";
        var envelopeId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var envelopeHash = Sha256(envelopeXml);
        var responseHash = Sha256(responseXml);

        await using var context = database.CreateContext();
        context.Set<V1FiscalCfeEnvelopeRecord>().Add(new V1FiscalCfeEnvelopeRecord
        {
            Id = envelopeId,
            OrganizationId = OrganizationId,
            ReceiverRut = ReceiverRut,
            IssuerRuc = IssuerRuc,
            SenderEnvelopeId = senderEnvelopeId,
            CreatedAtUtc = Now,
            CreatedAtOffsetMinutes = 0,
            FiscalDocumentIdsJson = JsonSerializer.Serialize(new[] { documentId }),
            OperationId = $"persist-{senderEnvelopeId}",
            CfeCount = 1,
            CertificateThumbprint = "thumb-ack-observation",
            CertificateSerialNumber = "serial-ack-observation",
            EnvelopeXml = envelopeXml,
            EnvelopeSha256 = envelopeHash,
            SchemaSetId = "dgi-fe-envelope-xsd-v1.44.2",
            SchemaVersion = "1.44.2",
            SchemaSetFingerprint = new string('a', 64)
        });
        context.Set<V1FiscalCfeEnvelopeSubmissionRecord>().Add(new V1FiscalCfeEnvelopeSubmissionRecord
        {
            Id = submissionId,
            EnvelopeId = envelopeId,
            OrganizationId = OrganizationId,
            IssuerRuc = IssuerRuc,
            ReceiverRut = ReceiverRut,
            SenderEnvelopeId = senderEnvelopeId,
            OperationId = $"transport-{senderEnvelopeId}",
            EnvelopeSha256 = envelopeHash,
            State = (int)FiscalCfeEnvelopeSubmissionState.ResponseReceived,
            AttemptCount = 1,
            PreparedAtUtc = Now.AddMinutes(-2),
            LastAttemptAtUtc = Now.AddMinutes(-1),
            CompletedAtUtc = Now,
            ResponseXml = responseXml,
            ResponseSha256 = responseHash,
            FailureCode = null
        });
        await context.SaveChangesAsync();

        var envelope = (await new EfFiscalCfeEnvelopeRepository(context).GetByIdentityAsync(
            OrganizationId,
            IssuerRuc,
            ReceiverRut,
            senderEnvelopeId))!;
        return new(envelope, responseXml, responseHash);
    }

    private static string Ack(string state, long senderEnvelopeId, string? reasons, bool includeConsultation)
    {
        var consultation = includeConsultation
            ? "<ParamConsulta><Token>dGVzdC10b2tlbg==</Token><FechaHora>2026-09-13T03:20:00</FechaHora></ParamConsulta>"
            : string.Empty;
        return $"<ACKSobre xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><RUCReceptor>{ReceiverRut}</RUCReceptor><RUCEmisor>{IssuerRuc}</RUCEmisor><IDRespuesta>5001</IDRespuesta><NomArch/><FecHRecibido>2026-09-13T03:15:00</FecHRecibido><IdEmisor>{senderEnvelopeId}</IdEmisor><IDReceptor>9001</IDReceptor><CantidadCFE>1</CantidadCFE><Tmst>2026-09-13T03:15:01</Tmst></Caratula><Detalle><Estado>{state}</Estado>{consultation}{reasons}</Detalle><ds:Signature xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\"><ds:SignedInfo /></ds:Signature></ACKSobre>";
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedClock : IFiscalCfeEnvelopeTransportClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed record SeededSource(
        StoredFiscalCfeEnvelope Envelope,
        string ResponseXml,
        string ResponseSha256);
}
