using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeDocumentResponseEvidenceCycleTests
{
    [Fact]
    public async Task Explicit_cycle_runs_once_in_order_reuses_one_normalized_operation_and_keeps_non_finality_flags()
    {
        var steps = new FakeSteps(replayed: false);
        var useCase = new CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(steps);

        var result = await useCase.ExecuteAsync(new CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand(
            " company-cycle ",
            " 219999820013 ",
            " 214844360018 ",
            3009,
            " ackcfe-cycle-1 "));

        Assert.Equal(new[] { "consult", "signature", "trust", "coverage" }, steps.Calls);
        Assert.NotNull(steps.ConsultCommand);
        Assert.Equal("company-cycle", steps.ConsultCommand!.OrganizationId);
        Assert.Equal("219999820013", steps.ConsultCommand.IssuerRuc);
        Assert.Equal("214844360018", steps.ConsultCommand.ReceiverRut);
        Assert.Equal("ackcfe-cycle-1", steps.ConsultCommand.OperationId);
        Assert.Equal("company-cycle", steps.SignatureOrganizationId);
        Assert.Equal("ackcfe-cycle-1", steps.SignatureConsultationOperationId);
        Assert.Equal("company-cycle", steps.TrustOrganizationId);
        Assert.Equal("ackcfe-cycle-1", steps.TrustConsultationOperationId);
        Assert.Equal("ackcfe-cycle-1", steps.TrustOperationId);
        Assert.Equal("ackcfe-cycle-1", steps.CoverageCommand!.OperationId);

        Assert.False(result.ConsultationReplayed);
        Assert.False(result.SignatureVerificationReplayed);
        Assert.False(result.TrustValidationReplayed);
        Assert.False(result.FullyReplayed);
        Assert.True(result.TrustValidation.PkiUruguayTrustValidated);
        Assert.Equal(FiscalCfeEnvelopeDocumentResponseCoverageStatus.FullDocumentCoverage, result.Coverage.CoverageStatus);
        Assert.False(result.DgiIdentityValidated);
        Assert.False(result.ProtocolFinalityProven);
        Assert.False(result.TokenExhaustionProven);
        Assert.False(result.AutomaticReconsultationAuthorized);
    }

    [Fact]
    public async Task Same_operation_can_surface_fully_replayed_checkpoint_chain_without_changing_semantics()
    {
        var steps = new FakeSteps(replayed: true);
        var useCase = new CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(steps);

        var result = await useCase.ExecuteAsync(Command("ackcfe-cycle-replay"));

        Assert.Equal(new[] { "consult", "signature", "trust", "coverage" }, steps.Calls);
        Assert.True(result.ConsultationReplayed);
        Assert.True(result.SignatureVerificationReplayed);
        Assert.True(result.TrustValidationReplayed);
        Assert.True(result.FullyReplayed);
        Assert.False(result.DgiIdentityValidated);
        Assert.False(result.ProtocolFinalityProven);
        Assert.False(result.TokenExhaustionProven);
        Assert.False(result.AutomaticReconsultationAuthorized);
    }

    [Fact]
    public async Task Any_child_overclaim_of_protocol_finality_is_rejected_after_exact_lineage_checks()
    {
        var steps = new FakeSteps(replayed: false, overclaimFinality: true);
        var useCase = new CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(steps);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("ackcfe-cycle-overclaim")));

        Assert.Equal("fiscal.envelope.document_response.evidence_cycle.lineage_mismatch", error.Code);
        Assert.Equal(new[] { "consult", "signature", "trust", "coverage" }, steps.Calls);
    }

    [Fact]
    public async Task Invalid_operation_id_fails_before_any_evidence_step()
    {
        var steps = new FakeSteps(replayed: false);
        var useCase = new CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(steps);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("   ")));

        Assert.Equal("fiscal.envelope.document_response.evidence_cycle.operation_id_invalid", error.Code);
        Assert.Empty(steps.Calls);
    }

    private static CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand Command(string operationId) =>
        new("company-cycle", "219999820013", "214844360018", 3009, operationId);

    private sealed class FakeSteps : IFiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps
    {
        private readonly bool _replayed;
        private readonly bool _overclaimFinality;
        private readonly Guid _consultationId = Guid.NewGuid();
        private readonly Guid _ackObservationId = Guid.NewGuid();
        private readonly Guid _submissionId = Guid.NewGuid();
        private readonly Guid _envelopeId = Guid.NewGuid();
        private readonly Guid _verificationId = Guid.NewGuid();
        private readonly Guid _validationId = Guid.NewGuid();
        private readonly string _responseSha256 = new('a', 64);
        private readonly string _certificateSha256 = new('b', 64);
        private readonly DateTimeOffset _now = new(2026, 9, 15, 3, 45, 0, TimeSpan.Zero);

        public FakeSteps(bool replayed, bool overclaimFinality = false)
        {
            _replayed = replayed;
            _overclaimFinality = overclaimFinality;
        }

        public List<string> Calls { get; } = [];
        public CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand? ConsultCommand { get; private set; }
        public string? SignatureOrganizationId { get; private set; }
        public string? SignatureConsultationOperationId { get; private set; }
        public string? TrustOrganizationId { get; private set; }
        public string? TrustConsultationOperationId { get; private set; }
        public string? TrustOperationId { get; private set; }
        public CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand? CoverageCommand { get; private set; }

        public Task<FiscalCfeEnvelopeDocumentResponseConsultationResult> ConsultAsync(
            CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("consult");
            ConsultCommand = command;
            return Task.FromResult(new FiscalCfeEnvelopeDocumentResponseConsultationResult(
                ConsultationId: _consultationId,
                AckObservationId: _ackObservationId,
                SubmissionId: _submissionId,
                EnvelopeId: _envelopeId,
                OrganizationId: command.OrganizationId,
                OperationId: command.OperationId,
                DgiReceiverId: 1516,
                DgiResponseId: 7001,
                EnvelopeCfeCount: 1,
                RespondedCount: 1,
                AcceptedCount: 1,
                RejectedCount: 0,
                ObservedCount: 0,
                OtherRejectedCount: 0,
                Details: new[] { new FiscalCfeEnvelopeDocumentResponseDetail(1, 101, "A", 123, "AE") },
                ResponseXml: "<ACKCFE />",
                ResponseSha256: _responseSha256,
                ConsultedAtUtc: _now,
                Replayed: _replayed));
        }

        public Task<FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult> VerifySignatureAsync(
            string organizationId,
            string consultationOperationId,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("signature");
            SignatureOrganizationId = organizationId;
            SignatureConsultationOperationId = consultationOperationId;
            return Task.FromResult(new FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult(
                VerificationId: _verificationId,
                ConsultationId: _consultationId,
                AckObservationId: _ackObservationId,
                SubmissionId: _submissionId,
                EnvelopeId: _envelopeId,
                OrganizationId: organizationId,
                ResponseSha256: _responseSha256,
                VerificationProfileId: "ackcfe-xmldsig-v1",
                CertificateSha256: _certificateSha256,
                CertificateThumbprint: "AABBCCDD",
                CertificateSerialNumber: "01020304",
                CertificateSubject: "CN=ACKCFE signer",
                CertificateIssuer: "CN=PKI Uruguay issuer",
                CanonicalizationMethod: "http://www.w3.org/TR/2001/REC-xml-c14n-20010315",
                SignatureMethod: "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
                DigestMethod: "http://www.w3.org/2001/04/xmlenc#sha256",
                ReferenceUri: string.Empty,
                ReferenceTransforms: new[] { "http://www.w3.org/2000/09/xmldsig#enveloped-signature" },
                SignatureValid: true,
                CertificateTrustValidated: false,
                VerifiedAtUtc: _now.AddSeconds(1),
                Replayed: _replayed));
        }

        public Task<FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult> ValidateTrustAsync(
            string organizationId,
            string consultationOperationId,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("trust");
            TrustOrganizationId = organizationId;
            TrustConsultationOperationId = consultationOperationId;
            TrustOperationId = operationId;
            var root = new string('c', 64);
            return Task.FromResult(new FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult(
                ValidationId: _validationId,
                SignatureVerificationId: _verificationId,
                ConsultationId: _consultationId,
                AckObservationId: _ackObservationId,
                SubmissionId: _submissionId,
                EnvelopeId: _envelopeId,
                OrganizationId: organizationId,
                OperationId: operationId,
                ResponseSha256: _responseSha256,
                ValidationProfileId: "pki-uruguay-online-v1",
                CertificateSha256: _certificateSha256,
                TrustedRootSha256: root,
                ChainCertificateSha256: new[] { _certificateSha256, root },
                RevocationMode: "Online",
                PkiUruguayTrustValidated: true,
                DgiIdentityValidated: false,
                ValidatedAtUtc: _now.AddSeconds(2),
                Replayed: _replayed));
        }

        public Task<FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult> AssessCoverageAsync(
            CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("coverage");
            CoverageCommand = command;
            return Task.FromResult(new FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult(
                AckObservationId: _ackObservationId,
                SubmissionId: _submissionId,
                EnvelopeId: _envelopeId,
                OrganizationId: command.OrganizationId,
                DgiReceiverId: 1516,
                ConsultationTokenSha256: new string('d', 64),
                EnvelopeCfeCount: 1,
                ConsultationObservationCount: 1,
                DistinctResponseMessageCount: 1,
                CoveredDocumentCount: 1,
                MissingDocumentCount: 0,
                CoverageStatus: FiscalCfeEnvelopeDocumentResponseCoverageStatus.FullDocumentCoverage,
                CoveredDocuments: new[]
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
                MissingDocuments: Array.Empty<FiscalCfeEnvelopeDocumentResponseMissingDocument>(),
                PkiUruguayTrustValidated: true,
                DgiIdentityValidated: false,
                ProtocolFinalityProven: _overclaimFinality,
                TokenExhaustionProven: false,
                AutomaticReconsultationAuthorized: false));
        }
    }
}
