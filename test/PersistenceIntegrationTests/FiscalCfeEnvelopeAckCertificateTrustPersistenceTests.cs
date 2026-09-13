using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalCfeEnvelopeAckCertificateTrustPersistenceTests
{
    private const string OrganizationId = "company-envelope-ack-trust";
    private const string IssuerRuc = "214748364700";
    private const string ReceiverRut = "219999830019";
    private const string LeafSha256 = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string RootSha256 = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 17, 40, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_persists_append_only_PKI_Uruguay_trust_and_replays_same_operation(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, 6201);
        var validator = new TrustedValidator();
        FiscalCfeEnvelopeAckCertificateTrustValidationResult first;
        FiscalCfeEnvelopeAckCertificateTrustValidationResult replay;

        await using (var context = database.CreateContext())
        {
            var useCase = UseCase(context, validator);
            first = await useCase.ExecuteAsync(Command(seed.Envelope, "trust-op-6201"));
            replay = await useCase.ExecuteAsync(Command(seed.Envelope, "trust-op-6201"));
        }

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.ValidationId, replay.ValidationId);
        Assert.True(first.PkiUruguayTrustValidated);
        Assert.False(first.DgiIdentityValidated);
        Assert.Equal("Online", first.RevocationMode);
        Assert.Equal(RootSha256, first.TrustedRootSha256);
        Assert.Equal(1, validator.CallCount);

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckCertificateTrustValidationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeRecord>().CountAsync());

        var submission = await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync();
        Assert.Equal(seed.ResponseXml, submission.ResponseXml);
        Assert.Equal(seed.ResponseSha256, submission.ResponseSha256);
        var signature = await verify.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>().AsNoTracking().SingleAsync();
        Assert.False(signature.CertificateTrustValidated);
        Assert.Equal(LeafSha256, signature.CertificateSha256);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_same_operation_validations_converge_to_one_trust_record(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, 6202);
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstValidator = new TrustedValidator();
        var secondValidator = new TrustedValidator();

        var firstTask = UseCase(firstContext, firstValidator)
            .ExecuteAsync(Command(seed.Envelope, "trust-concurrent-6202"));
        var secondTask = UseCase(secondContext, secondValidator)
            .ExecuteAsync(Command(seed.Envelope, "trust-concurrent-6202"));
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(results[0].ValidationId, results[1].ValidationId);
        Assert.Contains(results, x => !x.Replayed);
        Assert.Contains(results, x => x.Replayed);
        Assert.All(results, x => Assert.True(x.PkiUruguayTrustValidated));
        Assert.All(results, x => Assert.False(x.DgiIdentityValidated));

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckCertificateTrustValidationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().CountAsync());
    }

    private static ValidateFiscalCfeEnvelopeAckCertificateTrustUseCase UseCase(
        V1PersistenceDbContext context,
        IFiscalCfeEnvelopeAckCertificateTrustValidator validator) =>
        new(
            new EfFiscalCfeEnvelopeRepository(context),
            new EfFiscalCfeEnvelopeSubmissionRepository(context),
            new EfFiscalCfeEnvelopeAckObservationRepository(context),
            new EfFiscalCfeEnvelopeAckSignatureVerificationRepository(context),
            new EfFiscalCfeEnvelopeAckCertificateTrustValidationRepository(context),
            validator,
            new EfFiscalCfeEnvelopePersistenceConflictClassifier(),
            new FixedClock(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context));

    private static ValidateFiscalCfeEnvelopeAckCertificateTrustCommand Command(
        StoredFiscalCfeEnvelope envelope,
        string operationId) =>
        new(
            envelope.OrganizationId,
            envelope.IssuerRuc,
            envelope.ReceiverRut,
            envelope.SenderEnvelopeId,
            operationId);

    private static async Task<SeededSource> SeedAsync(TestDatabase database, long senderEnvelopeId)
    {
        var documentId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var observationId = Guid.NewGuid();
        var signatureVerificationId = Guid.NewGuid();
        var envelopeXml = $"<EnvioCFE xmlns=\"http://cfe.dgi.gub.uy\" version=\"1.0\"><Caratula version=\"1.0\"><RUCEmisor>{IssuerRuc}</RUCEmisor><RutReceptor>{ReceiverRut}</RutReceptor><Idemisor>{senderEnvelopeId}</Idemisor><CantCFE>1</CantCFE></Caratula></EnvioCFE>";
        var responseXml = $"<ACKSobre xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><RUCReceptor>{ReceiverRut}</RUCReceptor><RUCEmisor>{IssuerRuc}</RUCEmisor><IDRespuesta>7201</IDRespuesta><NomArch/><FecHRecibido>2026-09-13T17:35:00</FecHRecibido><IdEmisor>{senderEnvelopeId}</IdEmisor><IDReceptor>8201</IDReceptor><CantidadCFE>1</CantidadCFE><Tmst>2026-09-13T17:35:01</Tmst></Caratula><Detalle><Estado>AS</Estado></Detalle><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"/></ACKSobre>";
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
            CertificateThumbprint = "thumb-ack-trust",
            CertificateSerialNumber = "serial-ack-trust",
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
            DgiResponseId = 7201,
            DgiReceiverId = 8201,
            CfeCount = 1,
            State = (int)FiscalCfeEnvelopeAckState.Received,
            ReceptionTimestampText = "2026-09-13T17:35:00",
            SigningTimestampText = "2026-09-13T17:35:01",
            ConsultationToken = null,
            ConsultationAvailableAtText = null,
            RejectionReasonsJson = "[]",
            ObservedAtUtc = Now
        });
        context.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>().Add(new V1FiscalCfeEnvelopeAckSignatureVerificationRecord
        {
            Id = signatureVerificationId,
            AckObservationId = observationId,
            SubmissionId = submissionId,
            EnvelopeId = envelopeId,
            OrganizationId = OrganizationId,
            ResponseSha256 = responseHash,
            VerificationProfileId = "test-acksobre-xmldsig-math-v1",
            CertificateSha256 = LeafSha256,
            CertificateThumbprint = "AABBCCDD",
            CertificateSerialNumber = "01020304",
            CertificateSubject = "CN=DGI ACK Test",
            CertificateIssuer = "CN=Test Uruguay CA",
            CanonicalizationMethod = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315",
            SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
            DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256",
            ReferenceUri = string.Empty,
            ReferenceTransformsJson = JsonSerializer.Serialize(new[] { "http://www.w3.org/2000/09/xmldsig#enveloped-signature" }),
            CertificateTrustValidated = false,
            VerifiedAtUtc = Now.AddMinutes(-1)
        });
        await context.SaveChangesAsync();

        var envelope = (await new EfFiscalCfeEnvelopeRepository(context).GetByIdentityAsync(
            OrganizationId,
            IssuerRuc,
            ReceiverRut,
            senderEnvelopeId))!;
        return new(envelope, responseXml, responseHash);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedClock : IFiscalCfeEnvelopeTransportClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TrustedValidator : IFiscalCfeEnvelopeAckCertificateTrustValidator
    {
        private int _callCount;
        public int CallCount => _callCount;

        public FiscalCfeEnvelopeAckCertificateTrustEvidence Validate(
            string responseXml,
            string expectedCertificateSha256,
            DateTimeOffset validationTimeUtc)
        {
            Interlocked.Increment(ref _callCount);
            return new(
                IsTrusted: true,
                ValidationProfileId: "test-pki-uruguay-online-v1",
                CertificateSha256: expectedCertificateSha256,
                TrustedRootSha256: RootSha256,
                ChainCertificateSha256: new[] { expectedCertificateSha256, RootSha256 },
                ChainBuilt: true,
                RevocationChecked: true,
                RevocationMode: "Online",
                DgiIdentityValidated: false,
                FailureCode: null);
        }
    }

    private sealed record SeededSource(
        StoredFiscalCfeEnvelope Envelope,
        string ResponseXml,
        string ResponseSha256);
}
