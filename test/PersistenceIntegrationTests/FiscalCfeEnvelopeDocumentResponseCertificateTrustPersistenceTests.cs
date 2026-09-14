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

public sealed class FiscalCfeEnvelopeDocumentResponseCertificateTrustPersistenceTests
{
    private const string OrganizationId = "company-ackcfe-trust";
    private const string CertificateSha256 = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private const string RootSha256 = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_persists_append_only_ACKCFE_PKI_Uruguay_trust_and_replays_same_operation(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, "ackcfe-trust-source-1");

        FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult first;
        FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult replay;
        await using (var context = database.CreateContext())
        {
            var useCase = UseCase(context, new ValidValidator());
            first = await useCase.ExecuteAsync(Command(seed.ConsultationOperationId, "trust-operation-1"));
            replay = await useCase.ExecuteAsync(Command(seed.ConsultationOperationId, "trust-operation-1"));
        }

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.ValidationId, replay.ValidationId);
        Assert.Equal(seed.SignatureVerificationId, first.SignatureVerificationId);
        Assert.Equal(seed.ConsultationId, first.ConsultationId);
        Assert.Equal(seed.ResponseSha256, first.ResponseSha256);
        Assert.Equal(CertificateSha256, first.CertificateSha256);
        Assert.Equal(RootSha256, first.TrustedRootSha256);
        Assert.True(first.PkiUruguayTrustValidated);
        Assert.False(first.DgiIdentityValidated);
        Assert.Equal("Online", first.RevocationMode);

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseCertificateTrustValidationRecord>().CountAsync());
        var signature = await verify.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>()
            .AsNoTracking().SingleAsync(x => x.Id == seed.SignatureVerificationId);
        Assert.False(signature.CertificateTrustValidated);
        var consultation = await verify.Set<V1FiscalCfeDocumentResponseConsultationRecord>()
            .AsNoTracking().SingleAsync(x => x.Id == seed.ConsultationId);
        Assert.Equal(seed.ResponseXml, consultation.ResponseXml);
        Assert.Equal(seed.ResponseSha256, consultation.ResponseSha256);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Untrusted_ACKCFE_certificate_fails_closed_without_persisting_trust_evidence(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, "ackcfe-trust-source-2");
        await using var context = database.CreateContext();

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(context, new InvalidValidator()).ExecuteAsync(
                Command(seed.ConsultationOperationId, "trust-operation-2")));

        Assert.Equal("fiscal.envelope.document_response.trust.revocation_unavailable", error.Code);
        await using var verify = database.CreateContext();
        Assert.Equal(0, await verify.Set<V1FiscalCfeDocumentResponseCertificateTrustValidationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseConsultationRecord>().CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_same_operation_validations_converge_to_one_ACKCFE_trust_record(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database, "ackcfe-trust-source-3");
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();

        var firstTask = UseCase(firstContext, new ValidValidator()).ExecuteAsync(
            Command(seed.ConsultationOperationId, "trust-operation-concurrent"));
        var secondTask = UseCase(secondContext, new ValidValidator()).ExecuteAsync(
            Command(seed.ConsultationOperationId, "trust-operation-concurrent"));
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(results[0].ValidationId, results[1].ValidationId);
        Assert.Contains(results, x => !x.Replayed);
        Assert.Contains(results, x => x.Replayed);
        Assert.All(results, x => Assert.True(x.PkiUruguayTrustValidated));
        Assert.All(results, x => Assert.False(x.DgiIdentityValidated));

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseCertificateTrustValidationRecord>().CountAsync());
        Assert.Equal(1, await verify.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>().CountAsync());
    }

    private static ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase UseCase(
        V1PersistenceDbContext context,
        IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator validator) =>
        new(
            new EfFiscalCfeEnvelopeDocumentResponseConsultationRepository(context),
            new EfFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository(context),
            new EfFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository(context),
            validator,
            new EfFiscalCfeEnvelopePersistenceConflictClassifier(),
            new FixedClock(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context));

    private static ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustCommand Command(
        string consultationOperationId,
        string operationId) =>
        new(OrganizationId, consultationOperationId, operationId);

    private static async Task<SeededSource> SeedAsync(TestDatabase database, string consultationOperationId)
    {
        var envelopeId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var observationId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var signatureVerificationId = Guid.NewGuid();
        var responseXml = "<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><RUCReceptor>214844360018</RUCReceptor><RUCEmisor>219999820013</RUCEmisor><IDRespuesta>1516</IDRespuesta><IDEmisor>3009</IDEmisor><IDReceptor>1516</IDReceptor><CantenSobre>1</CantenSobre><CantResponden>1</CantResponden><CantCFEAceptados>1</CantCFEAceptados><CantCFERechazados>0</CantCFERechazados><CantCFEObservados>0</CantCFEObservados><CantOtrosRechazados>0</CantOtrosRechazados></Caratula><ACKCFE_det ordinal=\"1\"><TipoCFE>101</TipoCFE><Serie>A</Serie><NroCFE>123</NroCFE><Estado>A</Estado></ACKCFE_det><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"/></ACKCFE>";
        var responseSha256 = Sha256(responseXml);

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
            OperationId = $"envelope-{consultationOperationId}",
            CfeCount = 1,
            CertificateThumbprint = "thumbprint-ackcfe-trust",
            CertificateSerialNumber = "serial-ackcfe-trust",
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
            OperationId = $"submission-{consultationOperationId}",
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
            ReceptionTimestampText = "2026-09-14T09:20:00-03:00",
            SigningTimestampText = "2026-09-14T09:20:01-03:00",
            ConsultationToken = "token-ackcfe-trust",
            ConsultationAvailableAtText = "2026-09-14T09:20:02-03:00",
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
            OperationId = consultationOperationId,
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
            ResponseSha256 = responseSha256,
            ConsultedAtUtc = Now.AddMinutes(-5)
        });
        context.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>().Add(
            new V1FiscalCfeDocumentResponseSignatureVerificationRecord
            {
                Id = signatureVerificationId,
                ConsultationId = consultationId,
                AckObservationId = observationId,
                SubmissionId = submissionId,
                EnvelopeId = envelopeId,
                OrganizationId = OrganizationId,
                ResponseSha256 = responseSha256,
                VerificationProfileId = "test-ackcfe-xmldsig-math-v1",
                CertificateSha256 = CertificateSha256,
                CertificateThumbprint = "AABBCCDD",
                CertificateSerialNumber = "01020304",
                CertificateSubject = "CN=DGI ACKCFE Test",
                CertificateIssuer = "CN=PKI Uruguay Test CA",
                CanonicalizationMethod = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315",
                SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
                DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256",
                ReferenceUri = string.Empty,
                ReferenceTransformsJson = "[\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\"]",
                CertificateTrustValidated = false,
                VerifiedAtUtc = Now.AddMinutes(-4)
            });
        await context.SaveChangesAsync();

        return new(
            consultationId,
            signatureVerificationId,
            consultationOperationId,
            responseXml,
            responseSha256);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedClock : IFiscalCfeEnvelopeTransportClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class ValidValidator : IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator
    {
        public FiscalCfeEnvelopeDocumentResponseCertificateTrustEvidence Validate(
            string responseXml,
            string expectedCertificateSha256,
            DateTimeOffset validationTimeUtc) =>
            new(
                IsTrusted: true,
                ValidationProfileId: "test-pki-uruguay-custom-root-online-v1",
                CertificateSha256,
                TrustedRootSha256: RootSha256,
                ChainCertificateSha256: new[] { CertificateSha256, RootSha256 },
                ChainBuilt: true,
                RevocationChecked: true,
                RevocationMode: "Online",
                DgiIdentityValidated: false,
                FailureCode: null);
    }

    private sealed class InvalidValidator : IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator
    {
        public FiscalCfeEnvelopeDocumentResponseCertificateTrustEvidence Validate(
            string responseXml,
            string expectedCertificateSha256,
            DateTimeOffset validationTimeUtc) =>
            new(
                IsTrusted: false,
                ValidationProfileId: "test-pki-uruguay-custom-root-online-v1",
                CertificateSha256,
                TrustedRootSha256: string.Empty,
                ChainCertificateSha256: Array.Empty<string>(),
                ChainBuilt: false,
                RevocationChecked: false,
                RevocationMode: "Online",
                DgiIdentityValidated: false,
                FailureCode: "fiscal.envelope.document_response.trust.revocation_unavailable");
    }

    private sealed record SeededSource(
        Guid ConsultationId,
        Guid SignatureVerificationId,
        string ConsultationOperationId,
        string ResponseXml,
        string ResponseSha256);
}
