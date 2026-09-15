using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeDocumentResponseCoverageAssessmentTests
{
    [Fact]
    public async Task Two_trusted_messages_can_prove_full_document_coverage_without_proving_protocol_finality()
    {
        var source = Source();
        var first = Consultation(source, "consult-1", 6001, Detail(1, 123, "AE"));
        var second = Consultation(source, "consult-2", 6002, Detail(1, 124, "BE"));
        var evidence = Evidence(first, second);

        var result = await UseCase(source, evidence).ExecuteAsync(Command(source));

        Assert.Equal(FiscalCfeEnvelopeDocumentResponseCoverageStatus.FullDocumentCoverage, result.CoverageStatus);
        Assert.Equal(2, result.ConsultationObservationCount);
        Assert.Equal(2, result.DistinctResponseMessageCount);
        Assert.Equal(2, result.CoveredDocumentCount);
        Assert.Equal(0, result.MissingDocumentCount);
        Assert.Collection(
            result.CoveredDocuments,
            document =>
            {
                Assert.Equal(123, document.Number);
                Assert.Equal("AE", document.StateCode);
                Assert.Equal(FiscalCfeEnvelopeDocumentResponseSemanticState.Received, document.State);
            },
            document =>
            {
                Assert.Equal(124, document.Number);
                Assert.Equal("BE", document.StateCode);
                Assert.Equal(FiscalCfeEnvelopeDocumentResponseSemanticState.Rejected, document.State);
            });
        Assert.True(result.PkiUruguayTrustValidated);
        Assert.False(result.DgiIdentityValidated);
        Assert.False(result.ProtocolFinalityProven);
        Assert.False(result.TokenExhaustionProven);
        Assert.False(result.AutomaticReconsultationAuthorized);
    }

    [Fact]
    public async Task One_trusted_message_reports_partial_known_coverage_and_exact_missing_CFE()
    {
        var source = Source();
        var first = Consultation(source, "consult-partial", 6101, Detail(1, 123, "CE"));
        var evidence = Evidence(first);

        var result = await UseCase(source, evidence).ExecuteAsync(Command(source));

        Assert.Equal(FiscalCfeEnvelopeDocumentResponseCoverageStatus.PartialDocumentCoverage, result.CoverageStatus);
        Assert.Single(result.CoveredDocuments);
        Assert.Equal(123, result.CoveredDocuments[0].Number);
        Assert.Equal(FiscalCfeEnvelopeDocumentResponseSemanticState.ObservedContingency, result.CoveredDocuments[0].State);
        Assert.Single(result.MissingDocuments);
        Assert.Equal(124, result.MissingDocuments[0].Number);
        Assert.False(result.ProtocolFinalityProven);
    }

    [Fact]
    public async Task No_consultations_report_no_known_coverage_without_inventing_polling_or_finality()
    {
        var source = Source();
        var evidence = Evidence();

        var result = await UseCase(source, evidence).ExecuteAsync(Command(source));

        Assert.Equal(FiscalCfeEnvelopeDocumentResponseCoverageStatus.NoDocumentCoverage, result.CoverageStatus);
        Assert.Equal(0, result.ConsultationObservationCount);
        Assert.Equal(0, result.DistinctResponseMessageCount);
        Assert.Equal(0, result.CoveredDocumentCount);
        Assert.Equal(2, result.MissingDocumentCount);
        Assert.False(result.PkiUruguayTrustValidated);
        Assert.False(result.ProtocolFinalityProven);
        Assert.False(result.TokenExhaustionProven);
        Assert.False(result.AutomaticReconsultationAuthorized);
    }

    [Fact]
    public async Task Exact_reconsultation_replay_is_deduplicated_as_one_distinct_response_message()
    {
        var source = Source();
        var first = Consultation(source, "consult-replay-1", 6201, Detail(1, 123, "AE"));
        var replay = first with
        {
            Id = Guid.NewGuid(),
            OperationId = "consult-replay-2",
            ConsultedAtUtc = first.ConsultedAtUtc.AddMinutes(2)
        };
        var evidence = Evidence(first, replay);

        var result = await UseCase(source, evidence).ExecuteAsync(Command(source));

        Assert.Equal(2, result.ConsultationObservationCount);
        Assert.Equal(1, result.DistinctResponseMessageCount);
        Assert.Equal(1, result.CoveredDocumentCount);
        Assert.Single(result.CoveredDocuments);
        Assert.Equal(1, result.CoveredDocuments[0].EvidenceMessageCount);
        Assert.Equal(new long[] { 6201 }, result.CoveredDocuments[0].DgiResponseIds);
    }

    [Fact]
    public async Task Same_CFE_with_conflicting_trusted_states_fails_closed()
    {
        var source = Source();
        var first = Consultation(source, "consult-state-1", 6301, Detail(1, 123, "AE"));
        var second = Consultation(source, "consult-state-2", 6302, Detail(2, 123, "BE"));
        var evidence = Evidence(first, second);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(source, evidence).ExecuteAsync(Command(source)));

        Assert.Equal("fiscal.envelope.document_response.coverage.document_state_contradiction", error.Code);
    }

    [Fact]
    public async Task Same_DGI_response_id_with_different_bytes_fails_closed()
    {
        var source = Source();
        var first = Consultation(source, "consult-id-1", 6401, Detail(1, 123, "AE"));
        var second = Consultation(source, "consult-id-2", 6401, Detail(1, 124, "AE"));
        var evidence = Evidence(first, second);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(source, evidence).ExecuteAsync(Command(source)));

        Assert.Equal("fiscal.envelope.document_response.coverage.response_id_contradiction", error.Code);
    }

    [Fact]
    public async Task Any_consultation_without_PKI_trust_blocks_the_aggregate()
    {
        var source = Source();
        var first = Consultation(source, "consult-trusted", 6501, Detail(1, 123, "AE"));
        var second = Consultation(source, "consult-untrusted", 6502, Detail(1, 124, "AE"));
        var evidence = Evidence(first, second);
        evidence.Trusts.Remove(second.Id);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(source, evidence).ExecuteAsync(Command(source)));

        Assert.Equal("fiscal.envelope.document_response.coverage.trust_validation_required", error.Code);
    }

    [Fact]
    public async Task Per_message_counter_mismatch_fails_closed_before_aggregation()
    {
        var source = Source();
        var first = Consultation(source, "consult-counts", 6601, Detail(1, 123, "AE")) with
        {
            AcceptedCount = 0,
            RejectedCount = 1
        };
        var evidence = Evidence(first);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            UseCase(source, evidence).ExecuteAsync(Command(source)));

        Assert.Equal("fiscal.envelope.document_response.coverage.count_mismatch", error.Code);
    }

    private static AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase UseCase(SourceBundle source, EvidenceBundle evidence) =>
        new(
            new EnvelopeRepository(source.Envelope),
            new SubmissionRepository(source.Submission),
            new AckRepository(source.Observation),
            new DocumentRepository(source.Documents),
            new ConsultationHistoryReader(evidence.Consultations),
            new SignatureRepository(evidence.Signatures),
            new TrustHistoryReader(evidence.Trusts));

    private static AssessFiscalCfeEnvelopeDocumentResponseCoverageCommand Command(SourceBundle source) =>
        new(
            source.Envelope.OrganizationId,
            source.Envelope.IssuerRuc,
            source.Envelope.ReceiverRut,
            source.Envelope.SenderEnvelopeId);

    private static SourceBundle Source()
    {
        var documents = new[] { Document(123), Document(124) };
        const string envelopeXml = "<EnvioCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula /></EnvioCFE>";
        const string ackXml = "<ACKSobre xmlns=\"http://cfe.dgi.gub.uy\"><Caratula /></ACKSobre>";
        var envelope = new StoredFiscalCfeEnvelope(
            Guid.NewGuid(),
            "company-coverage",
            "214844360018",
            "219999820013",
            3009,
            new DateTimeOffset(2026, 9, 14, 19, 0, 0, TimeSpan.FromHours(-3)),
            documents.Select(x => x.Id).ToArray(),
            "envelope-coverage-op",
            documents.Length,
            "thumbprint-coverage",
            "serial-coverage",
            envelopeXml,
            Sha256(envelopeXml),
            "dgi-fe-v1.44.2",
            "05",
            new string('a', 64));
        var submission = new StoredFiscalCfeEnvelopeSubmission(
            Guid.NewGuid(),
            envelope.Id,
            envelope.OrganizationId,
            envelope.IssuerRuc,
            envelope.ReceiverRut,
            envelope.SenderEnvelopeId,
            "submission-coverage-op",
            envelope.EnvelopeSha256,
            FiscalCfeEnvelopeSubmissionState.ResponseReceived,
            1,
            new DateTimeOffset(2026, 9, 14, 22, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 14, 22, 1, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 14, 22, 1, 1, TimeSpan.Zero),
            ackXml,
            Sha256(ackXml),
            null);
        var observation = new StoredFiscalCfeEnvelopeAckObservation(
            Guid.NewGuid(),
            submission.Id,
            envelope.Id,
            envelope.OrganizationId,
            envelope.IssuerRuc,
            envelope.ReceiverRut,
            envelope.SenderEnvelopeId,
            submission.ResponseSha256!,
            5001,
            5002,
            documents.Length,
            FiscalCfeEnvelopeAckState.Received,
            "2026-09-14T19:01:00-03:00",
            "2026-09-14T19:01:01-03:00",
            "token-coverage",
            "2026-09-14T19:01:02-03:00",
            "[]",
            new DateTimeOffset(2026, 9, 14, 22, 2, 0, TimeSpan.Zero));
        return new(envelope, submission, observation, documents);
    }

    private static StoredFiscalCfeEnvelopeDocumentResponseConsultation Consultation(
        SourceBundle source,
        string operationId,
        long dgiResponseId,
        params FiscalCfeEnvelopeDocumentResponseDetail[] details)
    {
        var accepted = details.Count(x => x.StateCode == "AE");
        var rejected = details.Count(x => x.StateCode == "BE");
        var observed = details.Count(x => x.StateCode == "CE");
        var detailsJson = JsonSerializer.Serialize(details);
        var responseXml = $"<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\" response=\"{dgiResponseId}\">{detailsJson}</ACKCFE>";
        return new StoredFiscalCfeEnvelopeDocumentResponseConsultation(
            Guid.NewGuid(),
            source.Observation.Id,
            source.Submission.Id,
            source.Envelope.Id,
            source.Envelope.OrganizationId,
            operationId,
            source.Observation.ResponseSha256,
            source.Observation.DgiReceiverId,
            Sha256(source.Observation.ConsultationToken!),
            dgiResponseId,
            source.Envelope.IssuerRuc,
            source.Envelope.ReceiverRut,
            source.Envelope.SenderEnvelopeId,
            source.Envelope.CfeCount,
            details.Length,
            accepted,
            rejected,
            observed,
            0,
            detailsJson,
            responseXml,
            Sha256(responseXml),
            new DateTimeOffset(2026, 9, 14, 22, 10, 0, TimeSpan.Zero));
    }

    private static EvidenceBundle Evidence(params StoredFiscalCfeEnvelopeDocumentResponseConsultation[] consultations)
    {
        var signatures = new Dictionary<Guid, StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification>();
        var trusts = new Dictionary<Guid, IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation>>();
        foreach (var consultation in consultations)
        {
            var signature = Signature(consultation);
            signatures[consultation.Id] = signature;
            trusts[consultation.Id] = new[] { Trust(consultation, signature) };
        }
        return new(consultations, signatures, trusts);
    }

    private static StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification Signature(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation) =>
        new(
            Guid.NewGuid(),
            consultation.Id,
            consultation.AckObservationId,
            consultation.SubmissionId,
            consultation.EnvelopeId,
            consultation.OrganizationId,
            consultation.ResponseSha256,
            "ackcfe-xmldsig-v1",
            new string('c', 64),
            "thumbprint-ackcfe",
            "serial-ackcfe",
            "CN=ACKCFE signer",
            "CN=PKI Uruguay issuer",
            "http://www.w3.org/TR/2001/REC-xml-c14n-20010315",
            "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
            "http://www.w3.org/2001/04/xmlenc#sha256",
            string.Empty,
            JsonSerializer.Serialize(new[] { "http://www.w3.org/2000/09/xmldsig#enveloped-signature" }),
            CertificateTrustValidated: false,
            new DateTimeOffset(2026, 9, 14, 22, 11, 0, TimeSpan.Zero));

    private static StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation Trust(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation,
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification signature) =>
        new(
            Guid.NewGuid(),
            signature.Id,
            consultation.Id,
            consultation.AckObservationId,
            consultation.SubmissionId,
            consultation.EnvelopeId,
            consultation.OrganizationId,
            $"trust-{consultation.OperationId}",
            consultation.ResponseSha256,
            "pki-uruguay-online-v1",
            signature.CertificateSha256,
            new string('d', 64),
            JsonSerializer.Serialize(new[] { signature.CertificateSha256, new string('d', 64) }),
            "Online",
            PkiUruguayTrustValidated: true,
            DgiIdentityValidated: false,
            new DateTimeOffset(2026, 9, 14, 22, 12, 0, TimeSpan.Zero));

    private static FiscalCfeEnvelopeDocumentResponseDetail Detail(int ordinal, long number, string stateCode) =>
        new(ordinal, 101, "A", number, stateCode);

    private static FiscalDocument Document(long number) =>
        FiscalDocument.CreateIdentity(
            Guid.NewGuid(),
            "company-coverage",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CfeFamily.ETicket,
            "A",
            number,
            "CAE-COVERAGE-001",
            1,
            1000,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            new DateOnly(2026, 9, 14),
            "loc-1",
            "term-1",
            null,
            "25.2",
            new string('b', 64),
            new string('c', 64),
            "UYU",
            100m,
            22m,
            122m,
            new DateTimeOffset(2026, 9, 14, 21, 0, 0, TimeSpan.Zero));

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record SourceBundle(
        StoredFiscalCfeEnvelope Envelope,
        StoredFiscalCfeEnvelopeSubmission Submission,
        StoredFiscalCfeEnvelopeAckObservation Observation,
        FiscalDocument[] Documents);

    private sealed record EvidenceBundle(
        IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseConsultation> Consultations,
        Dictionary<Guid, StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification> Signatures,
        Dictionary<Guid, IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation>> Trusts);

    private sealed class EnvelopeRepository(StoredFiscalCfeEnvelope value) : IFiscalCfeEnvelopeRepository
    {
        public Task<StoredFiscalCfeEnvelope?> GetByOperationIdAsync(string organizationId, string operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalCfeEnvelope?>(value.OrganizationId == organizationId && value.OperationId == operationId ? value : null);

        public Task<StoredFiscalCfeEnvelope?> GetByIdentityAsync(string organizationId, string issuerRuc, string receiverRut, long senderEnvelopeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalCfeEnvelope?>(value.OrganizationId == organizationId && value.IssuerRuc == issuerRuc && value.ReceiverRut == receiverRut && value.SenderEnvelopeId == senderEnvelopeId ? value : null);

        public Task AddAsync(StoredFiscalCfeEnvelope envelope, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class SubmissionRepository(StoredFiscalCfeEnvelopeSubmission value) : IFiscalCfeEnvelopeSubmissionRepository
    {
        public Task<StoredFiscalCfeEnvelopeSubmission?> GetByOperationIdAsync(string organizationId, string operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalCfeEnvelopeSubmission?>(value.OrganizationId == organizationId && value.OperationId == operationId ? value : null);

        public Task<StoredFiscalCfeEnvelopeSubmission?> GetByEnvelopeIdAsync(Guid envelopeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalCfeEnvelopeSubmission?>(value.EnvelopeId == envelopeId ? value : null);

        public Task AddAsync(StoredFiscalCfeEnvelopeSubmission submission, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(StoredFiscalCfeEnvelopeSubmission submission, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class AckRepository(StoredFiscalCfeEnvelopeAckObservation value) : IFiscalCfeEnvelopeAckObservationRepository
    {
        public Task<StoredFiscalCfeEnvelopeAckObservation?> GetBySubmissionIdAsync(Guid submissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalCfeEnvelopeAckObservation?>(value.SubmissionId == submissionId ? value : null);

        public Task AddAsync(StoredFiscalCfeEnvelopeAckObservation observation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DocumentRepository(IReadOnlyList<FiscalDocument> values) : IFiscalDocumentRepository
    {
        public Task<FiscalDocument?> GetAsync(string organizationId, Guid fiscalDocumentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(values.SingleOrDefault(x => x.OrganizationId == organizationId && x.Id == fiscalDocumentId));

        public Task<FiscalDocument?> GetByFiscalizationRequestAsync(string organizationId, Guid fiscalizationRequestId, CancellationToken cancellationToken = default) =>
            Task.FromResult(values.SingleOrDefault(x => x.OrganizationId == organizationId && x.FiscalizationRequestId == fiscalizationRequestId));

        public Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ConsultationHistoryReader(IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseConsultation> values) :
        IFiscalCfeEnvelopeDocumentResponseConsultationHistoryReader
    {
        public Task<IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseConsultation>> ListByAckObservationIdAsync(
            Guid ackObservationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseConsultation>>(
                values.Where(x => x.AckObservationId == ackObservationId).Reverse().ToArray());
    }

    private sealed class SignatureRepository(Dictionary<Guid, StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification> values) :
        IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository
    {
        public Task<StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification?> GetByConsultationIdAsync(
            Guid consultationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(values.TryGetValue(consultationId, out var value) ? value : null);

        public Task AddAsync(StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification verification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TrustHistoryReader(Dictionary<Guid, IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation>> values) :
        IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationHistoryReader
    {
        public Task<IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation>> ListByConsultationIdAsync(
            Guid consultationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(values.TryGetValue(consultationId, out var found)
                ? found
                : (IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation>)Array.Empty<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation>());
    }
}
