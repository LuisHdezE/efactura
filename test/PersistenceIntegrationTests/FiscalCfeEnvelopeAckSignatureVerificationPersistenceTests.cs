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

public sealed class FiscalCfeEnvelopeAckSignatureVerificationPersistenceTests
{
    private const string OrganizationId = "company-envelope-ack-signature";
    private const string IssuerRuc = "214748364700";
    private const string ReceiverRut = "219999830019";
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 12, 10, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_persists_valid_signature_verification_and_replays_without_rewriting_source(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, 6101);

        FiscalCfeEnvelopeAckSignatureVerificationResult first;
        FiscalCfeEnvelopeAckSignatureVerificationResult replay;
        await using (var context = database.CreateContext())
        {
            var useCase = UseCase(context, new ValidVerifier());
            first = await useCase.ExecuteAsync(Command(seed.Envelope));
            replay = await useCase.ExecuteAsync(Command(seed.Envelope));
        }

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.VerificationId, replay.VerificationId);
        Assert.True(first.SignatureValid);
        Assert.False(first.CertificateTrustValidated);
        Assert.Equal("test-acksobre-xmldsig-math-v1", first.VerificationProfileId);
        Assert.Equal(string.Empty, first.ReferenceUri);

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>().CountAsync());
        var submission = await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync();
        Assert.Equal(seed.ResponseXml, submission.ResponseXml);
        Assert.Equal(seed.ResponseSha256, submission.ResponseSha256);
        var observation = await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().AsNoTracking().SingleAsync();
        Assert.Equal(seed.ObservationId, observation.Id);
        Assert.Equal(seed.ResponseSha256, observation.ResponseSha256);
        var envelope = await verify.Set<V1FiscalCfeEnvelopeRecord>().AsNoTracking().SingleAsync();
        Assert.Equal(seed.Envelope.EnvelopeSha256, envelope.EnvelopeSha256);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Invalid_signature_verification_fails_closed_without_persisting_evidence(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, 6102);
        await using var context = database.CreateContext();

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(context, new InvalidVerifier()).ExecuteAsync(Command(seed.Envelope)));

        Assert.Equal("fiscal.envelope.ack.signature.check_failed", error.Code);
        await using var verify = database.CreateContext();
        Assert.Equal(0, await verify.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_verifiers_converge_to_one_append_only_verification(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, 6103);
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();

        var firstTask = UseCase(firstContext, new ValidVerifier()).ExecuteAsync(Command(seed.Envelope));
        var secondTask = UseCase(secondContext, new ValidVerifier()).ExecuteAsync(Command(seed.Envelope));
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(results[0].VerificationId, results[1].VerificationId);
        Assert.Contains(results, x => !x.Replayed);
        Assert.Contains(results, x => x.Replayed);
        Assert.All(results, x => Assert.False(x.CertificateTrustValidated));

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().CountAsync());
    }

    private static VerifyFiscalCfeEnvelopeAckSignatureUseCase UseCase(
        V1PersistenceDbContext context,
        IFiscalCfeEnvelopeAckSignatureVerifier verifier) =>
        new(
            new EfFiscalCfeEnvelopeRepository(context),
            new EfFiscalCfeEnvelopeSubmissionRepository(context),
            new EfFiscalCfeEnvelopeAckObservationRepository(context),
            new EfFiscalCfeEnvelopeAckSignatureVerificationRepository(context),
            verifier,
            new EfFiscalCfeEnvelopePersistenceConflictClassifier(),
            new FixedClock(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context));

    private static VerifyFiscalCfeEnvelopeAckSignatureCommand Command(StoredFiscalCfeEnvelope envelope) =>
        new(envelope.OrganizationId, envelope.IssuerRuc, envelope.ReceiverRut, envelope.SenderEnvelopeId);

    private static async Task<SeededSource> SeedAsync(TestDatabase database, long senderEnvelopeId)
    {
        var documentId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var observationId = Guid.NewGuid();
        var envelopeXml = $"<EnvioCFE xmlns=\"http://cfe.dgi.gub.uy\" version=\"1.0\"><Caratula version=\"1.0\"><RUCEmisor>{IssuerRuc}</RUCEmisor><RutReceptor>{ReceiverRut}</RutReceptor><Idemisor>{senderEnvelopeId}</Idemisor><CantCFE>1</CantCFE></Caratula></EnvioCFE>";
        var responseXml = $"<ACKSobre xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><RUCReceptor>{ReceiverRut}</RUCReceptor><RUCEmisor>{IssuerRuc}</RUCEmisor><IDRespuesta>7001</IDRespuesta><NomArch/><FecHRecibido>2026-09-13T12:00:00</FecHRecibido><IdEmisor>{senderEnvelopeId}</IdEmisor><IDReceptor>8001</IDReceptor><CantidadCFE>1</CantidadCFE><Tmst>2026-09-13T12:00:01</Tmst></Caratula><Detalle><Estado>AS</Estado></Detalle><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"/></ACKSobre>";
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
            CertificateThumbprint = "thumb-signature-verification",
            CertificateSerialNumber = "serial-signature-verification",
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
            ResponseSha256 = responseHash
        });
        context.Set<V1FiscalCfeEnvelopeAckObservationRecord>().Add(new V1FiscalCfeEnvelopeAckObservationRecord
        {
            Id = observationId,
            SubmissionId = submissionId,
            EnvelopeId = envelopeId,
            OrganizationId = OrganizationId,
            IssuerRuc = IssuerRuc,
            ReceiverRut = ReceiverRut,
            SenderEnvelopeId = senderEnvelopeId,
            ResponseSha256 = responseHash,
            DgiResponseId = 7001,
            DgiReceiverId = 8001,
            CfeCount = 1,
            State = (int)FiscalCfeEnvelopeAckState.Received,
            ReceptionTimestampText = "2026-09-13T12:00:00",
            SigningTimestampText = "2026-09-13T12:00:01",
            ConsultationToken = null,
            ConsultationAvailableAtText = null,
            RejectionReasonsJson = "[]",
            ObservedAtUtc = Now
        });
        await context.SaveChangesAsync();

        var envelope = (await new EfFiscalCfeEnvelopeRepository(context).GetByIdentityAsync(
            OrganizationId,
            IssuerRuc,
            ReceiverRut,
            senderEnvelopeId))!;
        return new(envelope, observationId, responseXml, responseHash);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedClock : IFiscalCfeEnvelopeTransportClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class ValidVerifier : IFiscalCfeEnvelopeAckSignatureVerifier
    {
        public FiscalCfeEnvelopeAckSignatureVerificationEvidence Verify(string responseXml) =>
            new(
                true,
                "test-acksobre-xmldsig-math-v1",
                new string('b', 64),
                "AABBCCDD",
                "01020304",
                "CN=DGI ACK Test",
                "CN=Test CA",
                "http://www.w3.org/TR/2001/REC-xml-c14n-20010315",
                "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
                "http://www.w3.org/2001/04/xmlenc#sha256",
                string.Empty,
                new[] { "http://www.w3.org/2000/09/xmldsig#enveloped-signature" },
                CertificateTrustValidated: false,
                FailureCode: null);
    }

    private sealed class InvalidVerifier : IFiscalCfeEnvelopeAckSignatureVerifier
    {
        public FiscalCfeEnvelopeAckSignatureVerificationEvidence Verify(string responseXml) =>
            new(
                false,
                "test-acksobre-xmldsig-math-v1",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                CertificateTrustValidated: false,
                FailureCode: "fiscal.envelope.ack.signature.check_failed");
    }

    private sealed record SeededSource(
        StoredFiscalCfeEnvelope Envelope,
        Guid ObservationId,
        string ResponseXml,
        string ResponseSha256);
}
