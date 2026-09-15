using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalCfeEnvelopeDocumentResponseCoverageReaderPersistenceTests
{
    private const string OrganizationId = "company-ackcfe-coverage-reader";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_reads_all_consultations_for_exact_ACKSobre_and_trust_for_exact_consultation(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var seed = await SeedAsync(database);

        await using var context = database.CreateContext();
        var consultations = await new EfFiscalCfeEnvelopeDocumentResponseConsultationRepository(context)
            .ListByAckObservationIdAsync(seed.AckObservationId);
        var trusts = await new EfFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository(context)
            .ListByConsultationIdAsync(seed.FirstConsultationId);

        Assert.Equal(2, consultations.Count);
        Assert.Equal(
            new[] { seed.FirstConsultationId, seed.SecondConsultationId }.OrderBy(x => x),
            consultations.Select(x => x.Id).OrderBy(x => x));
        Assert.All(consultations, value => Assert.Equal(seed.AckObservationId, value.AckObservationId));
        Assert.All(consultations, value => Assert.Equal(seed.EnvelopeId, value.EnvelopeId));
        Assert.All(consultations, value => Assert.Equal(seed.SubmissionId, value.SubmissionId));
        Assert.Single(trusts);
        Assert.Equal(seed.FirstConsultationId, trusts[0].ConsultationId);
        Assert.Equal(seed.FirstSignatureId, trusts[0].SignatureVerificationId);
        Assert.True(trusts[0].PkiUruguayTrustValidated);
        Assert.False(trusts[0].DgiIdentityValidated);

        Assert.Equal(2, await context.Set<V1FiscalCfeDocumentResponseConsultationRecord>()
            .AsNoTracking().CountAsync(x => x.AckObservationId == seed.AckObservationId));
        Assert.Equal(1, await context.Set<V1FiscalCfeDocumentResponseCertificateTrustValidationRecord>()
            .AsNoTracking().CountAsync(x => x.ConsultationId == seed.FirstConsultationId));
    }

    private static async Task<Seed> SeedAsync(TestDatabase database)
    {
        var envelopeId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var observationId = Guid.NewGuid();
        var firstConsultationId = Guid.NewGuid();
        var secondConsultationId = Guid.NewGuid();
        var firstSignatureId = Guid.NewGuid();
        var sourceHash = new string('b', 64);
        var tokenHash = new string('c', 64);
        var firstXml = "<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDRespuesta>7001</IDRespuesta></Caratula></ACKCFE>";
        var secondXml = "<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDRespuesta>7002</IDRespuesta></Caratula></ACKCFE>";
        var firstHash = Sha256(firstXml);
        var secondHash = Sha256(secondXml);
        var certificateHash = new string('d', 64);
        var rootHash = new string('e', 64);
        var now = new DateTimeOffset(2026, 9, 15, 2, 30, 0, TimeSpan.Zero);

        await using var context = database.CreateContext();
        context.Set<V1FiscalCfeEnvelopeRecord>().Add(new V1FiscalCfeEnvelopeRecord
        {
            Id = envelopeId,
            OrganizationId = OrganizationId,
            ReceiverRut = "214844360018",
            IssuerRuc = "219999820013",
            SenderEnvelopeId = 3009,
            CreatedAtUtc = now.AddMinutes(-20),
            CreatedAtOffsetMinutes = -180,
            FiscalDocumentIdsJson = "[\"11111111-1111-1111-1111-111111111111\",\"22222222-2222-2222-2222-222222222222\"]",
            OperationId = "coverage-reader-envelope",
            CfeCount = 2,
            CertificateThumbprint = "thumbprint-coverage-reader",
            CertificateSerialNumber = "serial-coverage-reader",
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
            OperationId = "coverage-reader-submission",
            EnvelopeSha256 = new string('a', 64),
            State = (int)FiscalCfeEnvelopeSubmissionState.ResponseReceived,
            AttemptCount = 1,
            PreparedAtUtc = now.AddMinutes(-19),
            LastAttemptAtUtc = now.AddMinutes(-18),
            CompletedAtUtc = now.AddMinutes(-18),
            ResponseXml = "<ACKSobre />",
            ResponseSha256 = sourceHash
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
            ResponseSha256 = sourceHash,
            DgiResponseId = 5001,
            DgiReceiverId = 5002,
            CfeCount = 2,
            State = (int)FiscalCfeEnvelopeAckState.Received,
            ReceptionTimestampText = "2026-09-14T23:11:00-03:00",
            SigningTimestampText = "2026-09-14T23:11:01-03:00",
            ConsultationToken = "token-coverage-reader",
            ConsultationAvailableAtText = "2026-09-14T23:11:02-03:00",
            RejectionReasonsJson = "[]",
            ObservedAtUtc = now.AddMinutes(-17)
        });

        context.Set<V1FiscalCfeDocumentResponseConsultationRecord>().AddRange(
            ConsultationRecord(
                firstConsultationId,
                observationId,
                submissionId,
                envelopeId,
                "coverage-reader-consult-1",
                sourceHash,
                tokenHash,
                7001,
                firstXml,
                firstHash,
                now.AddMinutes(-10)),
            ConsultationRecord(
                secondConsultationId,
                observationId,
                submissionId,
                envelopeId,
                "coverage-reader-consult-2",
                sourceHash,
                tokenHash,
                7002,
                secondXml,
                secondHash,
                now.AddMinutes(-8)));

        context.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>().Add(
            new V1FiscalCfeDocumentResponseSignatureVerificationRecord
            {
                Id = firstSignatureId,
                ConsultationId = firstConsultationId,
                AckObservationId = observationId,
                SubmissionId = submissionId,
                EnvelopeId = envelopeId,
                OrganizationId = OrganizationId,
                ResponseSha256 = firstHash,
                VerificationProfileId = "ackcfe-xmldsig-v1",
                CertificateSha256 = certificateHash,
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
                VerifiedAtUtc = now.AddMinutes(-7)
            });
        context.Set<V1FiscalCfeDocumentResponseCertificateTrustValidationRecord>().Add(
            new V1FiscalCfeDocumentResponseCertificateTrustValidationRecord
            {
                Id = Guid.NewGuid(),
                SignatureVerificationId = firstSignatureId,
                ConsultationId = firstConsultationId,
                AckObservationId = observationId,
                SubmissionId = submissionId,
                EnvelopeId = envelopeId,
                OrganizationId = OrganizationId,
                OperationId = "coverage-reader-trust-1",
                ResponseSha256 = firstHash,
                ValidationProfileId = "pki-uruguay-online-v1",
                CertificateSha256 = certificateHash,
                TrustedRootSha256 = rootHash,
                ChainCertificateSha256Json = $"[\"{certificateHash}\",\"{rootHash}\"]",
                RevocationMode = "Online",
                PkiUruguayTrustValidated = true,
                DgiIdentityValidated = false,
                ValidatedAtUtc = now.AddMinutes(-6)
            });

        await context.SaveChangesAsync();
        return new(envelopeId, submissionId, observationId, firstConsultationId, secondConsultationId, firstSignatureId);
    }

    private static V1FiscalCfeDocumentResponseConsultationRecord ConsultationRecord(
        Guid id,
        Guid observationId,
        Guid submissionId,
        Guid envelopeId,
        string operationId,
        string sourceHash,
        string tokenHash,
        long responseId,
        string responseXml,
        string responseHash,
        DateTimeOffset consultedAtUtc) => new()
    {
        Id = id,
        AckObservationId = observationId,
        SubmissionId = submissionId,
        EnvelopeId = envelopeId,
        OrganizationId = OrganizationId,
        OperationId = operationId,
        SourceAckResponseSha256 = sourceHash,
        DgiReceiverId = 5002,
        ConsultationTokenSha256 = tokenHash,
        DgiResponseId = responseId,
        IssuerRuc = "219999820013",
        ReceiverRut = "214844360018",
        SenderEnvelopeId = 3009,
        EnvelopeCfeCount = 2,
        RespondedCount = 1,
        AcceptedCount = 1,
        RejectedCount = 0,
        ObservedCount = 0,
        OtherRejectedCount = 0,
        DetailsJson = "[{\"Ordinal\":1,\"CfeType\":101,\"Series\":\"A\",\"Number\":123,\"StateCode\":\"AE\"}]",
        ResponseXml = responseXml,
        ResponseSha256 = responseHash,
        ConsultedAtUtc = consultedAtUtc
    };

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record Seed(
        Guid EnvelopeId,
        Guid SubmissionId,
        Guid AckObservationId,
        Guid FirstConsultationId,
        Guid SecondConsultationId,
        Guid FirstSignatureId);
}
