using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeDocumentResponseEvidenceApiTests
{
    private const string OrganizationId = "company-api-cycle";
    private static readonly Guid EnvelopeId = Guid.Parse("b1000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Envelope_id_wrapper_uses_server_owned_submission_identity_and_preserves_cycle_safety_flags()
    {
        var steps = new FakeSteps(EnvelopeId);
        var canonical = new CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(steps);
        var useCase = new CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdUseCase(
            new SubmissionRepository(Submission(OrganizationId)),
            canonical);

        var result = await useCase.ExecuteAsync(new(
            OrganizationId,
            EnvelopeId,
            "API-FIS-010:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));

        Assert.NotNull(steps.Command);
        Assert.Equal(OrganizationId, steps.Command!.OrganizationId);
        Assert.Equal("219999820013", steps.Command.IssuerRuc);
        Assert.Equal("214844360018", steps.Command.ReceiverRut);
        Assert.Equal(3009, steps.Command.SenderEnvelopeId);
        Assert.Equal(EnvelopeId, result.Consultation.EnvelopeId);
        Assert.False(result.DgiIdentityValidated);
        Assert.False(result.ProtocolFinalityProven);
        Assert.False(result.TokenExhaustionProven);
        Assert.False(result.AutomaticReconsultationAuthorized);
    }

    [Fact]
    public async Task Envelope_from_another_organization_is_hidden_as_not_found_before_any_DGI_cycle_step()
    {
        var steps = new FakeSteps(EnvelopeId);
        var canonical = new CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(steps);
        var useCase = new CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdUseCase(
            new SubmissionRepository(Submission("another-company")),
            canonical);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => useCase.ExecuteAsync(new(
            OrganizationId,
            EnvelopeId,
            "API-FIS-010:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")));

        Assert.Equal(ApplicationProblemKind.NotFound, error.Kind);
        Assert.Equal("fiscal.envelope.document_response.evidence_api.envelope_not_found", error.Code);
        Assert.Null(steps.Command);
    }

    private static StoredFiscalCfeEnvelopeSubmission Submission(string organizationId)
    {
        const string responseXml = "<ACKSobre />";
        return new StoredFiscalCfeEnvelopeSubmission(
            Guid.Parse("b2000000-0000-0000-0000-000000000001"),
            EnvelopeId,
            organizationId,
            "219999820013",
            "214844360018",
            3009,
            "transport-op",
            new string('e', 64),
            FiscalCfeEnvelopeSubmissionState.ResponseReceived,
            1,
            new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 15, 12, 0, 1, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 15, 12, 0, 2, TimeSpan.Zero),
            responseXml,
            Sha256(responseXml),
            null);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class SubmissionRepository(StoredFiscalCfeEnvelopeSubmission submission)
        : IFiscalCfeEnvelopeSubmissionRepository
    {
        public Task<StoredFiscalCfeEnvelopeSubmission?> GetByOperationIdAsync(
            string organizationId,
            string operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalCfeEnvelopeSubmission?>(null);

        public Task<StoredFiscalCfeEnvelopeSubmission?> GetByEnvelopeIdAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalCfeEnvelopeSubmission?>(
                envelopeId == submission.EnvelopeId ? submission : null);

        public Task AddAsync(StoredFiscalCfeEnvelopeSubmission value, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(StoredFiscalCfeEnvelopeSubmission value, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSteps(Guid envelopeId) : IFiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps
    {
        private readonly Guid _consultationId = Guid.Parse("b3000000-0000-0000-0000-000000000001");
        private readonly Guid _ackId = Guid.Parse("b4000000-0000-0000-0000-000000000001");
        private readonly Guid _submissionId = Guid.Parse("b2000000-0000-0000-0000-000000000001");
        private readonly Guid _verificationId = Guid.Parse("b5000000-0000-0000-0000-000000000001");
        private readonly string _responseHash = new('a', 64);
        private readonly string _certificateHash = new('b', 64);

        public CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand? Command { get; private set; }

        public Task<FiscalCfeEnvelopeDocumentResponseConsultationResult> ConsultAsync(
            CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
            CancellationToken cancellationToken = default)
        {
            Command = command;
            return Task.FromResult(new FiscalCfeEnvelopeDocumentResponseConsultationResult(
                _consultationId,
                _ackId,
                _submissionId,
                envelopeId,
                command.OrganizationId,
                command.OperationId,
                1500,
                7001,
                1,
                1,
                1,
                0,
                0,
                0,
                new[] { new FiscalCfeEnvelopeDocumentResponseDetail(1, 101, "A", 123, "AE") },
                "<ACKCFE />",
                _responseHash,
                new DateTimeOffset(2026, 9, 15, 12, 1, 0, TimeSpan.Zero),
                false));
        }

        public Task<FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult> VerifySignatureAsync(
            string organizationId,
            string consultationOperationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult(
                _verificationId,
                _consultationId,
                _ackId,
                _submissionId,
                envelopeId,
                organizationId,
                _responseHash,
                "ackcfe-xmldsig-v1",
                _certificateHash,
                "AABBCC",
                "010203",
                "CN=Signer",
                "CN=Issuer",
                "c14n",
                "rsa-sha256",
                "sha256",
                string.Empty,
                new[] { "enveloped-signature" },
                true,
                false,
                new DateTimeOffset(2026, 9, 15, 12, 1, 1, TimeSpan.Zero),
                false));

        public Task<FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult> ValidateTrustAsync(
            string organizationId,
            string consultationOperationId,
            string operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult(
                Guid.Parse("b6000000-0000-0000-0000-000000000001"),
                _verificationId,
                _consultationId,
                _ackId,
                _submissionId,
                envelopeId,
                organizationId,
                operationId,
                _responseHash,
                "pki-uruguay-online-v1",
                _certificateHash,
                new string('c', 64),
                new[] { _certificateHash, new string('c', 64) },
                "Online",
                true,
                false,
                new DateTimeOffset(2026, 9, 15, 12, 1, 2, TimeSpan.Zero),
                false));

        public Task<FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult> AssessCoverageAsync(
            CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult(
                _ackId,
                _submissionId,
                envelopeId,
                command.OrganizationId,
                1500,
                new string('d', 64),
                1,
                1,
                1,
                1,
                0,
                FiscalCfeEnvelopeDocumentResponseCoverageStatus.FullDocumentCoverage,
                new[]
                {
                    new FiscalCfeEnvelopeDocumentResponseCoverageDocument(
                        101,
                        "A",
                        123,
                        "AE",
                        FiscalCfeEnvelopeDocumentResponseSemanticState.Received,
                        1,
                        new long[] { 7001 })
                },
                Array.Empty<FiscalCfeEnvelopeDocumentResponseMissingDocument>(),
                true,
                false,
                false,
                false,
                false));
    }
}
