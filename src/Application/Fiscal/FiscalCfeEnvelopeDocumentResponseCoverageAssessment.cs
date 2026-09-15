using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;

namespace EFactura.Application.Fiscal;

/// <summary>
/// Read-only enumeration port for all durable ACKCFE consultations that originate from one exact ACKSobre observation.
/// Returned collection order is never treated as protocol order or fiscal truth.
/// </summary>
public interface IFiscalCfeEnvelopeDocumentResponseConsultationHistoryReader
{
    Task<IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseConsultation>> ListByAckObservationIdAsync(
        Guid ackObservationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only enumeration port for all durable PKI Uruguay trust validations attached to one ACKCFE consultation.
/// </summary>
public interface IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationHistoryReader
{
    Task<IReadOnlyList<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation>> ListByConsultationIdAsync(
        Guid consultationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Coverage of CFE identities demonstrated by the currently durable, verified and PKI-trusted ACKCFE evidence set.
/// FullDocumentCoverage means every CFE in the durable Sobre has at least one non-contradictory trusted detail response.
/// It does not mean DGI has emitted its last message and does not prove token exhaustion or protocol finality.
/// </summary>
public enum FiscalCfeEnvelopeDocumentResponseCoverageStatus
{
    NoDocumentCoverage = 1,
    PartialDocumentCoverage = 2,
    FullDocumentCoverage = 3
}

public sealed record FiscalCfeEnvelopeDocumentResponseCoverageDocument(
    int CfeType,
    string Series,
    long Number,
    string StateCode,
    FiscalCfeEnvelopeDocumentResponseSemanticState State,
    int EvidenceMessageCount,
    IReadOnlyList<long> DgiResponseIds);

public sealed record FiscalCfeEnvelopeDocumentResponseMissingDocument(
    int CfeType,
    string Series,
    long Number);

public sealed record AssessFiscalCfeEnvelopeDocumentResponseCoverageCommand(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId);

public sealed record FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult(
    Guid AckObservationId,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    long DgiReceiverId,
    string ConsultationTokenSha256,
    int EnvelopeCfeCount,
    int ConsultationObservationCount,
    int DistinctResponseMessageCount,
    int CoveredDocumentCount,
    int MissingDocumentCount,
    FiscalCfeEnvelopeDocumentResponseCoverageStatus CoverageStatus,
    IReadOnlyList<FiscalCfeEnvelopeDocumentResponseCoverageDocument> CoveredDocuments,
    IReadOnlyList<FiscalCfeEnvelopeDocumentResponseMissingDocument> MissingDocuments,
    bool PkiUruguayTrustValidated,
    bool DgiIdentityValidated,
    bool ProtocolFinalityProven,
    bool TokenExhaustionProven,
    bool AutomaticReconsultationAuthorized);

/// <summary>
/// Aggregates every currently durable ACKCFE consultation for one exact accepted ACKSobre source.
/// Each consultation must have durable XMLDSig verification and PKI Uruguay trust evidence.
/// Reconsultations that return the exact same response XML are duplicate observations of the same message and are deduplicated by response SHA-256.
/// ACKCFE_det ordinal remains message-scoped and is never promoted to cross-message identity.
/// Contradictory response ids, source identity, counters or per-CFE states fail closed.
/// The result states document coverage only. It never claims last-message finality, token exhaustion, polling cadence or automatic local lifecycle mutation.
/// </summary>
public sealed class AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase
{
    private readonly IFiscalCfeEnvelopeRepository _envelopes;
    private readonly IFiscalCfeEnvelopeSubmissionRepository _submissions;
    private readonly IFiscalCfeEnvelopeAckObservationRepository _ackObservations;
    private readonly IFiscalDocumentRepository _documents;
    private readonly IFiscalCfeEnvelopeDocumentResponseConsultationHistoryReader _consultations;
    private readonly IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository _signatureVerifications;
    private readonly IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationHistoryReader _trustValidations;

    public AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase(
        IFiscalCfeEnvelopeRepository envelopes,
        IFiscalCfeEnvelopeSubmissionRepository submissions,
        IFiscalCfeEnvelopeAckObservationRepository ackObservations,
        IFiscalDocumentRepository documents,
        IFiscalCfeEnvelopeDocumentResponseConsultationHistoryReader consultations,
        IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository signatureVerifications,
        IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationHistoryReader trustValidations)
    {
        _envelopes = envelopes ?? throw new ArgumentNullException(nameof(envelopes));
        _submissions = submissions ?? throw new ArgumentNullException(nameof(submissions));
        _ackObservations = ackObservations ?? throw new ArgumentNullException(nameof(ackObservations));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _consultations = consultations ?? throw new ArgumentNullException(nameof(consultations));
        _signatureVerifications = signatureVerifications ?? throw new ArgumentNullException(nameof(signatureVerifications));
        _trustValidations = trustValidations ?? throw new ArgumentNullException(nameof(trustValidations));
    }

    public async Task<FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult> ExecuteAsync(
        AssessFiscalCfeEnvelopeDocumentResponseCoverageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        PrepareFiscalCfeEnvelopeSubmissionUseCase.Validate(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId);

        var normalized = command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            IssuerRuc = command.IssuerRuc.Trim(),
            ReceiverRut = command.ReceiverRut.Trim()
        };

        var source = await RequiredSourceAsync(normalized, cancellationToken);
        var expectedDocuments = await ExpectedDocumentsAsync(source.Envelope, cancellationToken);
        var expectedByIdentity = expectedDocuments.ToDictionary(x => x.Identity);

        var observations = await _consultations.ListByAckObservationIdAsync(
            source.Observation.Id,
            cancellationToken);
        observations ??= Array.Empty<StoredFiscalCfeEnvelopeDocumentResponseConsultation>();

        var distinctMessages = new Dictionary<string, MessageEvidence>(StringComparer.Ordinal);
        var responseIds = new Dictionary<long, string>();

        foreach (var consultation in observations)
        {
            ConsultFiscalCfeEnvelopeDocumentResponseUseCase.EnsureStoredIntegrity(consultation);
            EnsureConsultationSource(consultation, source);

            if (responseIds.TryGetValue(consultation.DgiResponseId, out var responseHash)
                && !string.Equals(responseHash, consultation.ResponseSha256, StringComparison.Ordinal))
            {
                throw Conflict(
                    "fiscal.envelope.document_response.coverage.response_id_contradiction",
                    "The same DGI ACKCFE response id is attached to different response bytes.",
                    "contradictory_external_evidence");
            }
            responseIds[consultation.DgiResponseId] = consultation.ResponseSha256;

            var signature = await _signatureVerifications.GetByConsultationIdAsync(
                consultation.Id,
                cancellationToken)
                ?? throw Conflict(
                    "fiscal.envelope.document_response.coverage.signature_verification_required",
                    "Every ACKCFE consultation included in coverage assessment requires durable XMLDSig verification.",
                    "missing_prerequisite");
            VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase.EnsureVerificationIntegrity(signature);
            EnsureSignatureSource(consultation, signature);

            var trusts = await _trustValidations.ListByConsultationIdAsync(
                consultation.Id,
                cancellationToken);
            if (trusts is null || trusts.Count == 0)
            {
                throw Conflict(
                    "fiscal.envelope.document_response.coverage.trust_validation_required",
                    "Every ACKCFE consultation included in coverage assessment requires durable PKI Uruguay trust evidence.",
                    "missing_prerequisite");
            }
            foreach (var trust in trusts)
            {
                ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase.EnsureValidationIntegrity(trust);
                EnsureTrustSource(consultation, signature, trust);
            }

            var interpreted = InterpretMessage(consultation, expectedByIdentity);
            if (distinctMessages.TryGetValue(consultation.ResponseSha256, out var existing))
            {
                if (!EquivalentMessage(existing.Consultation, consultation))
                {
                    throw Conflict(
                        "fiscal.envelope.document_response.coverage.duplicate_message_metadata_contradiction",
                        "Duplicate ACKCFE response bytes are attached to contradictory persisted metadata.",
                        "contradictory_persisted_evidence");
                }
                continue;
            }

            distinctMessages.Add(
                consultation.ResponseSha256,
                new MessageEvidence(consultation, interpreted));
        }

        var coveredByIdentity = new Dictionary<DocumentIdentity, MutableCoverageDocument>();
        foreach (var message in distinctMessages.Values.OrderBy(x => x.Consultation.ResponseSha256, StringComparer.Ordinal))
        {
            foreach (var detail in message.Details)
            {
                var identity = new DocumentIdentity(detail.CfeType, detail.Series, detail.Number);
                if (!coveredByIdentity.TryGetValue(identity, out var aggregate))
                {
                    aggregate = new MutableCoverageDocument(
                        detail.StateCode,
                        detail.State,
                        new HashSet<long>());
                    coveredByIdentity.Add(identity, aggregate);
                }
                else if (!string.Equals(aggregate.StateCode, detail.StateCode, StringComparison.Ordinal)
                    || aggregate.State != detail.State)
                {
                    throw Conflict(
                        "fiscal.envelope.document_response.coverage.document_state_contradiction",
                        "The durable ACKCFE evidence set contains contradictory responses for the same CFE identity.",
                        "contradictory_external_evidence");
                }

                aggregate.DgiResponseIds.Add(message.Consultation.DgiResponseId);
                aggregate.EvidenceMessageCount++;
            }
        }

        var covered = new List<FiscalCfeEnvelopeDocumentResponseCoverageDocument>();
        var missing = new List<FiscalCfeEnvelopeDocumentResponseMissingDocument>();
        foreach (var expected in expectedDocuments)
        {
            if (coveredByIdentity.TryGetValue(expected.Identity, out var aggregate))
            {
                covered.Add(new FiscalCfeEnvelopeDocumentResponseCoverageDocument(
                    expected.Identity.CfeType,
                    expected.Identity.Series,
                    expected.Identity.Number,
                    aggregate.StateCode,
                    aggregate.State,
                    aggregate.EvidenceMessageCount,
                    aggregate.DgiResponseIds.OrderBy(x => x).ToArray()));
            }
            else
            {
                missing.Add(new FiscalCfeEnvelopeDocumentResponseMissingDocument(
                    expected.Identity.CfeType,
                    expected.Identity.Series,
                    expected.Identity.Number));
            }
        }

        var status = covered.Count switch
        {
            0 => FiscalCfeEnvelopeDocumentResponseCoverageStatus.NoDocumentCoverage,
            var count when count == source.Envelope.CfeCount => FiscalCfeEnvelopeDocumentResponseCoverageStatus.FullDocumentCoverage,
            _ => FiscalCfeEnvelopeDocumentResponseCoverageStatus.PartialDocumentCoverage
        };

        return new FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult(
            source.Observation.Id,
            source.Submission.Id,
            source.Envelope.Id,
            source.Envelope.OrganizationId,
            source.Observation.DgiReceiverId,
            Sha256(source.Observation.ConsultationToken!),
            source.Envelope.CfeCount,
            observations.Count,
            distinctMessages.Count,
            covered.Count,
            missing.Count,
            status,
            covered,
            missing,
            PkiUruguayTrustValidated: observations.Count > 0,
            DgiIdentityValidated: false,
            ProtocolFinalityProven: false,
            TokenExhaustionProven: false,
            AutomaticReconsultationAuthorized: false);
    }

    private async Task<SourceEvidence> RequiredSourceAsync(
        AssessFiscalCfeEnvelopeDocumentResponseCoverageCommand command,
        CancellationToken cancellationToken)
    {
        var envelope = await _envelopes.GetByIdentityAsync(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.coverage.envelope_required",
                "A durable Sobre is required before ACKCFE coverage assessment.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureEnvelopeIntegrity(envelope);

        var submission = await _submissions.GetByEnvelopeIdAsync(envelope.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.coverage.submission_required",
                "A durable Sobre submission is required before ACKCFE coverage assessment.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity(submission);
        if (submission.State != FiscalCfeEnvelopeSubmissionState.ResponseReceived)
        {
            throw Conflict(
                "fiscal.envelope.document_response.coverage.response_required",
                "ACKCFE coverage assessment requires a durable ResponseReceived ACKSobre source.",
                "missing_prerequisite");
        }

        var observation = await _ackObservations.GetBySubmissionIdAsync(submission.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.coverage.ack_required",
                "A durable ACKSobre observation is required before ACKCFE coverage assessment.",
                "missing_prerequisite");
        ObserveFiscalCfeEnvelopeAckUseCase.EnsureObservationIntegrity(observation);
        if (observation.State != FiscalCfeEnvelopeAckState.Received
            || string.IsNullOrWhiteSpace(observation.ConsultationToken)
            || string.IsNullOrWhiteSpace(observation.ConsultationAvailableAtText))
        {
            throw Conflict(
                "fiscal.envelope.document_response.coverage.accepted_ack_required",
                "Coverage assessment requires accepted ACKSobre evidence carrying IdReceptor + Token consultation parameters.",
                "missing_prerequisite");
        }

        return new SourceEvidence(envelope, submission, observation);
    }

    private async Task<IReadOnlyList<ExpectedDocument>> ExpectedDocumentsAsync(
        StoredFiscalCfeEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var expected = new List<ExpectedDocument>(envelope.FiscalDocumentIds.Count);
        var identities = new HashSet<DocumentIdentity>();
        foreach (var documentId in envelope.FiscalDocumentIds)
        {
            var document = await _documents.GetAsync(envelope.OrganizationId, documentId, cancellationToken)
                ?? throw Conflict(
                    "fiscal.envelope.document_response.coverage.document_required",
                    "Every CFE referenced by the durable Sobre must resolve to immutable fiscal identity evidence.",
                    "invalid_persisted_evidence");
            var identity = new DocumentIdentity((int)document.CfeType, document.Series, document.Number);
            if (!identities.Add(identity))
            {
                throw Conflict(
                    "fiscal.envelope.document_response.coverage.document_identity_duplicate",
                    "The durable Sobre resolves to duplicate CFE identities.",
                    "invalid_persisted_evidence");
            }
            expected.Add(new ExpectedDocument(identity));
        }
        return expected;
    }

    private static IReadOnlyList<FiscalCfeEnvelopeDocumentResponseSemanticDetail> InterpretMessage(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation,
        IReadOnlyDictionary<DocumentIdentity, ExpectedDocument> expectedDocuments)
    {
        IReadOnlyList<FiscalCfeEnvelopeDocumentResponseDetail>? rawDetails;
        try
        {
            rawDetails = JsonSerializer.Deserialize<List<FiscalCfeEnvelopeDocumentResponseDetail>>(consultation.DetailsJson);
        }
        catch (JsonException)
        {
            rawDetails = null;
        }
        if (rawDetails is null || rawDetails.Count != consultation.RespondedCount)
        {
            throw Conflict(
                "fiscal.envelope.document_response.coverage.details_invalid",
                "Persisted ACKCFE details cannot be aggregated safely.",
                "invalid_persisted_evidence");
        }

        var accepted = 0;
        var rejected = 0;
        var observed = 0;
        var ordinals = new HashSet<int>();
        var identities = new HashSet<DocumentIdentity>();
        var interpreted = new List<FiscalCfeEnvelopeDocumentResponseSemanticDetail>(rawDetails.Count);
        foreach (var detail in rawDetails)
        {
            if (detail.Ordinal is < 1 or > 250
                || detail.CfeType <= 0
                || string.IsNullOrWhiteSpace(detail.Series)
                || detail.Series.Trim().Length > 20
                || detail.Number <= 0
                || string.IsNullOrWhiteSpace(detail.StateCode)
                || detail.StateCode.Trim().Length > 40)
            {
                throw Conflict(
                    "fiscal.envelope.document_response.coverage.detail_identity_invalid",
                    "ACKCFE detail evidence is incomplete or outside the governed bounds.",
                    "invalid_persisted_evidence");
            }

            var normalizedSeries = detail.Series.Trim();
            var normalizedStateCode = detail.StateCode.Trim();
            var identity = new DocumentIdentity(detail.CfeType, normalizedSeries, detail.Number);
            if (!ordinals.Add(detail.Ordinal)
                || !identities.Add(identity)
                || !expectedDocuments.ContainsKey(identity))
            {
                throw Conflict(
                    "fiscal.envelope.document_response.coverage.detail_identity_invalid",
                    "ACKCFE detail evidence does not map uniquely to the durable Sobre.",
                    "invalid_persisted_evidence");
            }

            var state = normalizedStateCode switch
            {
                "AE" => FiscalCfeEnvelopeDocumentResponseSemanticState.Received,
                "BE" => FiscalCfeEnvelopeDocumentResponseSemanticState.Rejected,
                "CE" => FiscalCfeEnvelopeDocumentResponseSemanticState.ObservedContingency,
                _ => throw Conflict(
                    "fiscal.envelope.document_response.coverage.state_code_unsupported",
                    $"ACKCFE detail state '{normalizedStateCode}' is not in the governed DGI AE/BE/CE taxonomy.",
                    "unsupported_external_semantics")
            };

            switch (state)
            {
                case FiscalCfeEnvelopeDocumentResponseSemanticState.Received:
                    accepted++;
                    break;
                case FiscalCfeEnvelopeDocumentResponseSemanticState.Rejected:
                    rejected++;
                    break;
                case FiscalCfeEnvelopeDocumentResponseSemanticState.ObservedContingency:
                    observed++;
                    break;
            }

            interpreted.Add(new FiscalCfeEnvelopeDocumentResponseSemanticDetail(
                detail.Ordinal,
                detail.CfeType,
                normalizedSeries,
                detail.Number,
                normalizedStateCode,
                state));
        }

        if (accepted != consultation.AcceptedCount
            || rejected != consultation.RejectedCount
            || observed != consultation.ObservedCount)
        {
            throw Conflict(
                "fiscal.envelope.document_response.coverage.count_mismatch",
                "ACKCFE message counters do not match the governed per-document AE/BE/CE details.",
                "invalid_external_semantics");
        }

        return interpreted;
    }

    private static void EnsureConsultationSource(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation value,
        SourceEvidence source)
    {
        if (value.AckObservationId != source.Observation.Id
            || value.SubmissionId != source.Submission.Id
            || value.EnvelopeId != source.Envelope.Id
            || !string.Equals(value.OrganizationId, source.Envelope.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(value.SourceAckResponseSha256, source.Observation.ResponseSha256, StringComparison.Ordinal)
            || value.DgiReceiverId != source.Observation.DgiReceiverId
            || !string.Equals(value.ConsultationTokenSha256, Sha256(source.Observation.ConsultationToken!), StringComparison.Ordinal)
            || !string.Equals(value.IssuerRuc, source.Envelope.IssuerRuc, StringComparison.Ordinal)
            || !string.Equals(value.ReceiverRut, source.Envelope.ReceiverRut, StringComparison.Ordinal)
            || value.SenderEnvelopeId != source.Envelope.SenderEnvelopeId
            || value.EnvelopeCfeCount != source.Envelope.CfeCount)
        {
            throw Conflict(
                "fiscal.envelope.document_response.coverage.consultation_source_mismatch",
                "Persisted ACKCFE consultation evidence does not belong to the exact durable ACKSobre source.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSignatureSource(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation,
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification signature)
    {
        if (signature.ConsultationId != consultation.Id
            || signature.AckObservationId != consultation.AckObservationId
            || signature.SubmissionId != consultation.SubmissionId
            || signature.EnvelopeId != consultation.EnvelopeId
            || !string.Equals(signature.OrganizationId, consultation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(signature.ResponseSha256, consultation.ResponseSha256, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.document_response.coverage.signature_source_mismatch",
                "ACKCFE signature verification does not match its exact consultation source.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureTrustSource(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation,
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification signature,
        StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation trust)
    {
        if (trust.SignatureVerificationId != signature.Id
            || trust.ConsultationId != consultation.Id
            || trust.AckObservationId != consultation.AckObservationId
            || trust.SubmissionId != consultation.SubmissionId
            || trust.EnvelopeId != consultation.EnvelopeId
            || !string.Equals(trust.OrganizationId, consultation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(trust.ResponseSha256, consultation.ResponseSha256, StringComparison.Ordinal)
            || !string.Equals(trust.CertificateSha256, signature.CertificateSha256, StringComparison.OrdinalIgnoreCase)
            || !trust.PkiUruguayTrustValidated
            || trust.DgiIdentityValidated)
        {
            throw Conflict(
                "fiscal.envelope.document_response.coverage.trust_source_mismatch",
                "ACKCFE PKI trust evidence does not match its exact consultation/signature source.",
                "invalid_persisted_evidence");
        }
    }

    private static bool EquivalentMessage(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation left,
        StoredFiscalCfeEnvelopeDocumentResponseConsultation right) =>
        left.DgiResponseId == right.DgiResponseId
        && left.EnvelopeCfeCount == right.EnvelopeCfeCount
        && left.RespondedCount == right.RespondedCount
        && left.AcceptedCount == right.AcceptedCount
        && left.RejectedCount == right.RejectedCount
        && left.ObservedCount == right.ObservedCount
        && left.OtherRejectedCount == right.OtherRejectedCount
        && string.Equals(left.DetailsJson, right.DetailsJson, StringComparison.Ordinal)
        && string.Equals(left.ResponseXml, right.ResponseXml, StringComparison.Ordinal);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);

    private sealed record SourceEvidence(
        StoredFiscalCfeEnvelope Envelope,
        StoredFiscalCfeEnvelopeSubmission Submission,
        StoredFiscalCfeEnvelopeAckObservation Observation);

    private sealed record DocumentIdentity(int CfeType, string Series, long Number);
    private sealed record ExpectedDocument(DocumentIdentity Identity);
    private sealed record MessageEvidence(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation Consultation,
        IReadOnlyList<FiscalCfeEnvelopeDocumentResponseSemanticDetail> Details);

    private sealed class MutableCoverageDocument(
        string stateCode,
        FiscalCfeEnvelopeDocumentResponseSemanticState state,
        HashSet<long> dgiResponseIds)
    {
        public string StateCode { get; } = stateCode;
        public FiscalCfeEnvelopeDocumentResponseSemanticState State { get; } = state;
        public HashSet<long> DgiResponseIds { get; } = dgiResponseIds;
        public int EvidenceMessageCount { get; set; }
    }
}
