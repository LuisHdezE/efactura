using System.Security.Cryptography;
using System.Text;
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

public sealed class FiscalCfeEnvelopeDocumentResponseSignatureVerificationPersistenceTests
{
    private const string OrganizationId = "company-ackcfe-signature";
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 1, 10, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_persists_valid_ACKCFE_signature_verification_and_replays_without_rewriting_source(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, "ackcfe-signature-source-1");

        FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult first;
        FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult replay;
        await using (var context = database.CreateContext())
        {
            var useCase = UseCase(context, new ValidVerifier());
            first = await useCase.ExecuteAsync(Command(seed.OperationId));
            replay = await useCase.ExecuteAsync(Command(seed.OperationId));
        }

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.VerificationId, replay.VerificationId);
        Assert.Equal(seed.ConsultationId, first.ConsultationId);
        Assert.True(first.SignatureValid);
        Assert.False(first.CertificateTrustValidated);
        Assert.Equal("test-ackcfe-xmldsig-math-v1", first.VerificationProfileId);
        Assert.Equal(seed.ResponseSha256, first.ResponseSha256);

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>().CountAsync());
        var consultation = await verify.Set<V1FiscalCfeDocumentResponseConsultationRecord>()
            .AsNoTracking().SingleAsync(x => x.Id == seed.ConsultationId);
        Assert.Equal(seed.ResponseXml, consultation.ResponseXml);
        Assert.Equal(seed.ResponseSha256, consultation.ResponseSha256);
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeRecord>().CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Invalid_ACKCFE_signature_fails_closed_without_persisting_verification(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, "ackcfe-signature-source-2");
        await using var context = database.CreateContext();

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(context, new InvalidVerifier()).ExecuteAsync(Command(seed.OperationId)));

        Assert.Equal("fiscal.envelope.document_response.signature.check_failed", error.Code);
        await using var verify = database.CreateContext();
        Assert.Equal(0, await verify.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseConsultationRecord>().CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_ACKCFE_verifiers_converge_to_one_append_only_verification(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, "ackcfe-signature-source-3");
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();

        var firstTask = UseCase(firstContext, new ValidVerifier()).ExecuteAsync(Command(seed.OperationId));
        var secondTask = UseCase(secondContext, new ValidVerifier()).ExecuteAsync(Command(seed.OperationId));
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(results[0].VerificationId, results[1].VerificationId);
        Assert.Contains(results, x => !x.Replayed);
        Assert.Contains(results, x => x.Replayed);
        Assert.All(results, x => Assert.False(x.CertificateTrustValidated));

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseConsultationRecord>().CountAsync());
    }

    private static VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase UseCase(
        V1PersistenceDbContext context,
        IFiscalCfeEnvelopeDocumentResponseSignatureVerifier verifier) =>
        new(
            new EfFiscalCfeEnvelopeDocumentResponseConsultationRepository(context),
            new EfFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository(context),
            verifier,
            new EfFiscalCfeEnvelopePersistenceConflictClassifier(),
            new FixedClock(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context));

    private static VerifyFiscalCfeEnvelopeDocumentResponseSignatureCommand Command(string operationId) =>
        new(OrganizationId, operationId);

    private static async Task<SeededSource> SeedAsync(TestDatabase database, string operationId)
    {
        var envelopeId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var observationId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var responseXml = "<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><RUCReceptor>214844360018</RUCReceptor><RUCEmisor>219999820013</RUCEmisor><IDRespuesta>1516</IDRespuesta><IDEmisor>3009</IDEmisor><IDReceptor>1516</IDReceptor><CantenSobre>1</CantenSobre><CantResponden>1</CantResponden><CantCFEAceptados>1</CantCFEAceptados><CantCFERechazados>0</CantCFERechazados><CantCFEObservados>0</CantCFEObservados><CantOtrosRechazados>0</CantOtrosRechazados></Caratula><ACKCFE_det ordinal=\"1\"><TipoCFE>101</TipoCFE><Serie>A</Serie><NroCFE>123</NroCFE><Estado>A</Estado></ACKCFE_det><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"/></ACKCFE>";
        var responseSha = Sha256(responseXml);

        await using var context = database.CreateContext();
        context.Set<V1FiscalCfeEnvelopeRecord>().Add(new V1FiscalCfeEnvelopeRecord
        {
            Id = envelopeId,
            OrganizationId = OrganizationId,
            ReceiverRut = "214844360018",
            IssuerRuc = "219999820013",
            SenderEnvelopeId = 3009,
            CreatedAtUtc = Now.AddMinutes(-10),
            CreatedAtOffsetMinutes = -180,
            FiscalDocumentIdsJson = "[\"11111111-1111-1111-1111-111111111111\"]",
            OperationId = $"envelope-{operationId}",
            CfeCount = 1,
            CertificateThumbprint = "thumbprint-ackcfe-signature",
            CertificateSerialNumber = "serial-ackcfe-signature",
            EnvelopeXml = "<EnvioCFE />",
            EnvelopeSha256 = new string('a', 64),
            SchemaSetId = "dgi-fe-v1.44.2",
            SchemaVersion = "05",
            SchemaSetFingerprint = new string('1', 64)
        });
        context.Set<V1FiscalCfeEnvelopeSubmissionRecord>().Add(new V1FiscalCfeEnvelopeSubmissionRecord
        {
            Id = submissionId,
            EnvelopeId = envelopeId,
            OrganizationId = OrganizationId,
            IssuerRuc = "219999820013",
            ReceiverRut = "214844360018",
            SenderEnvelopeId = 3009,
            OperationId = $"submission-{operationId}",
            EnvelopeSha256 = new string('a', 64),
            State = (int)FiscalCfeEnvelopeSubmissionState.ResponseReceived,
            AttemptCount = 1,
            PreparedAtUtc = Now.AddMinutes(-9),
            LastAttemptAtUtc = Now.AddMinutes(-8),
            CompletedAtUtc = Now.AddMinutes(-8),
            ResponseXml = "<ACKSobre />",
            ResponseSha256 = new string('b', 64)
        });
        context.Set<V1FiscalCfeEnvelopeAckObservationRecord>().Add(new V1FiscalCfeEnvelopeAckObservationRecord
        {
            Id = observationId,
            SubmissionId = submissionId,
            EnvelopeId = envelopeId,
            OrganizationId = OrganizationId,
            IssuerRuc = "219999820013",
            ReceiverRut = "214844360018",
            SenderEnvelopeId = 3009,
            ResponseSha256 = new string('b', 64),
            DgiResponseId = 1516,
            DgiReceiverId = 1516,
            CfeCount = 1,
            State = (int)FiscalCfeEnvelopeAckState.Received,
            ReceptionTimestampText = "2026-09-13T22:58:00-03:00",
            SigningTimestampText = "2026-09-13T22:58:01-03:00",
            ConsultationToken = "token-ackcfe-signature",
            ConsultationAvailableAtText = "2026-09-13T22:58:02-03:00",
            RejectionReasonsJson = "[]",
            ObservedAtUtc = Now.AddMinutes(-7)
        });
        context.Set<V1FiscalCfeDocumentResponseConsultationRecord>().Add(new V1FiscalCfeDocumentResponseConsultationRecord
        {
            Id = consultationId,
            AckObservationId = observationId,
            SubmissionId = submissionId,
            EnvelopeId = envelopeId,
            OrganizationId = OrganizationId,
            OperationId = operationId,
            SourceAckResponseSha256 = new string('b', 64),
            DgiReceiverId = 1516,
            ConsultationTokenSha256 = new string('c', 64),
            DgiResponseId = 1516,
            IssuerRuc = "219999820013",
            ReceiverRut = "214844360018",
            SenderEnvelopeId = 3009,
            EnvelopeCfeCount = 1,
            RespondedCount = 1,
            AcceptedCount = 1,
            RejectedCount = 0,
            ObservedCount = 0,
            OtherRejectedCount = 0,
            DetailsJson = "[{\"Ordinal\":1,\"CfeType\":101,\"Series\":\"A\",\"Number\":123,\"StateCode\":\"A\"}]",
            ResponseXml = responseXml,
            ResponseSha256 = responseSha,
            ConsultedAtUtc = Now.AddMinutes(-5)
        });
        await context.SaveChangesAsync();
        return new(consultationId, operationId, responseXml, responseSha);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedClock : IFiscalCfeEnvelopeTransportClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class ValidVerifier : IFiscalCfeEnvelopeDocumentResponseSignatureVerifier
    {
        public FiscalCfeEnvelopeDocumentResponseSignatureVerificationEvidence Verify(string responseXml) =>
            new(
                true,
                "test-ackcfe-xmldsig-math-v1",
                new string('d', 64),
                "AABBCCDD",
                "01020304",
                "CN=DGI ACKCFE Test",
                "CN=Test CA",
                "http://www.w3.org/TR/2001/REC-xml-c14n-20010315",
                "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
                "http://www.w3.org/2001/04/xmlenc#sha256",
                string.Empty,
                new[] { "http://www.w3.org/2000/09/xmldsig#enveloped-signature" },
                CertificateTrustValidated: false,
                FailureCode: null);
    }

    private sealed class InvalidVerifier : IFiscalCfeEnvelopeDocumentResponseSignatureVerifier
    {
        public FiscalCfeEnvelopeDocumentResponseSignatureVerificationEvidence Verify(string responseXml) =>
            new(
                false,
                "test-ackcfe-xmldsig-math-v1",
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
                FailureCode: "fiscal.envelope.document_response.signature.check_failed");
    }

    private sealed record SeededSource(
        Guid ConsultationId,
        string OperationId,
        string ResponseXml,
        string ResponseSha256);
}
