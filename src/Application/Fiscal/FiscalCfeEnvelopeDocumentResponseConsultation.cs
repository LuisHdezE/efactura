using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public sealed record FiscalCfeEnvelopeDocumentResponseConsultationRequest(
    string OrganizationId,
    long DgiReceiverId,
    string ConsultationToken);

public sealed record FiscalCfeEnvelopeDocumentResponseDetail(
    int Ordinal,
    int CfeType,
    string Series,
    long Number,
    string StateCode);

public sealed record FiscalCfeEnvelopeDocumentResponseConsultationResponse(
    string IssuerRuc,
    string ReceiverRut,
    long DgiResponseId,
    long SenderEnvelopeId,
    long DgiReceiverId,
    int EnvelopeCfeCount,
    int RespondedCount,
    int AcceptedCount,
    int RejectedCount,
    int ObservedCount,
    int OtherRejectedCount,
    IReadOnlyList<FiscalCfeEnvelopeDocumentResponseDetail> Details,
    string ResponseXml);

public interface IFiscalCfeEnvelopeDocumentResponseConsultationGateway
{
    Task<FiscalCfeEnvelopeDocumentResponseConsultationResponse> QueryAsync(
        FiscalCfeEnvelopeDocumentResponseConsultationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record StoredFiscalCfeEnvelopeDocumentResponseConsultation(
    Guid Id,
    Guid AckObservationId,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string OperationId,
    string SourceAckResponseSha256,
    long DgiReceiverId,
    string ConsultationTokenSha256,
    long DgiResponseId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    int EnvelopeCfeCount,
    int RespondedCount,
    int AcceptedCount,
    int RejectedCount,
    int ObservedCount,
    int OtherRejectedCount,
    string DetailsJson,
    string ResponseXml,
    string ResponseSha256,
    DateTimeOffset ConsultedAtUtc);

public interface IFiscalCfeEnvelopeDocumentResponseConsultationRepository
{
    Task<StoredFiscalCfeEnvelopeDocumentResponseConsultation?> GetByOperationAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation,
        CancellationToken cancellationToken = default);
}

public sealed record ConsultFiscalCfeEnvelopeDocumentResponseCommand(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string OperationId);

public sealed record FiscalCfeEnvelopeDocumentResponseConsultationResult(
    Guid ConsultationId,
    Guid AckObservationId,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string OperationId,
    long DgiReceiverId,
    long DgiResponseId,
    int EnvelopeCfeCount,
    int RespondedCount,
    int AcceptedCount,
    int RejectedCount,
    int ObservedCount,
    int OtherRejectedCount,
    IReadOnlyList<FiscalCfeEnvelopeDocumentResponseDetail> Details,
    string ResponseXml,
    string ResponseSha256,
    DateTimeOffset ConsultedAtUtc,
    bool Replayed);

/// <summary>
/// Queries the DGI ws_efactura / EFACCONSULTARESTADOENVIO contract using the durable IdReceptor +
/// Token carried by an accepted ACKSobre. Each returned ACKCFE is persisted append-only as one
/// response observation. DGI permits per-CFE results to be produced in one or multiple messages,
/// therefore one successful query never implies complete resolution of every CFE in the Sobre.
/// No local fiscal-document, sale, accounting or inventory state is mutated by this boundary.
/// </summary>
public sealed class ConsultFiscalCfeEnvelopeDocumentResponseUseCase
{
    private readonly IFiscalCfeEnvelopeRepository _envelopes;
    private readonly IFiscalCfeEnvelopeSubmissionRepository _submissions;
    private readonly IFiscalCfeEnvelopeAckObservationRepository _ackObservations;
    private readonly IFiscalDocumentRepository _documents;
    private readonly IFiscalCfeEnvelopeDocumentResponseConsultationRepository _consultations;
    private readonly IFiscalCfeEnvelopeDocumentResponseConsultationGateway _gateway;
    private readonly IFiscalCfeEnvelopePersistenceConflictClassifier _conflicts;
    private readonly IFiscalCfeEnvelopeTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public ConsultFiscalCfeEnvelopeDocumentResponseUseCase(
        IFiscalCfeEnvelopeRepository envelopes,
        IFiscalCfeEnvelopeSubmissionRepository submissions,
        IFiscalCfeEnvelopeAckObservationRepository ackObservations,
        IFiscalDocumentRepository documents,
        IFiscalCfeEnvelopeDocumentResponseConsultationRepository consultations,
        IFiscalCfeEnvelopeDocumentResponseConsultationGateway gateway,
        IFiscalCfeEnvelopePersistenceConflictClassifier conflicts,
        IFiscalCfeEnvelopeTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _envelopes = envelopes;
        _submissions = submissions;
        _ackObservations = ackObservations;
        _documents = documents;
        _consultations = consultations;
        _gateway = gateway;
        _conflicts = conflicts;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeEnvelopeDocumentResponseConsultationResult> ExecuteAsync(
        ConsultFiscalCfeEnvelopeDocumentResponseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        PrepareFiscalCfeEnvelopeSubmissionUseCase.Validate(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId);
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
        {
            throw Conflict(
                "fiscal.envelope.document_response.operation_id_invalid",
                "Operation id is required and must contain at most 120 characters.",
                "invalid_request");
        }

        var normalized = command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            IssuerRuc = command.IssuerRuc.Trim(),
            ReceiverRut = command.ReceiverRut.Trim(),
            OperationId = command.OperationId.Trim()
        };

        var source = await RequiredSourceAsync(normalized, cancellationToken);
        var replay = await _consultations.GetByOperationAsync(
            normalized.OrganizationId,
            normalized.OperationId,
            cancellationToken);
        if (replay is not null)
        {
            EnsureStoredIntegrity(replay);
            EnsureSameSource(replay, source);
            return Result(replay, replayed: true);
        }

        var expectedDocuments = await ExpectedDocumentsAsync(source.Envelope, cancellationToken);
        var response = await _gateway.QueryAsync(
            new FiscalCfeEnvelopeDocumentResponseConsultationRequest(
                normalized.OrganizationId,
                source.Observation.DgiReceiverId,
                source.Observation.ConsultationToken!),
            cancellationToken);
        EnsureExternalEvidence(response, source, expectedDocuments);

        var consultedAt = WholeSecond(_clock.UtcNow);
        var detailsJson = JsonSerializer.Serialize(response.Details);
        var consultation = new StoredFiscalCfeEnvelopeDocumentResponseConsultation(
            Guid.NewGuid(),
            source.Observation.Id,
            source.Submission.Id,
            source.Envelope.Id,
            source.Envelope.OrganizationId,
            normalized.OperationId,
            source.Observation.ResponseSha256,
            source.Observation.DgiReceiverId,
            Sha256(source.Observation.ConsultationToken!),
            response.DgiResponseId,
            response.IssuerRuc.Trim(),
            response.ReceiverRut.Trim(),
            response.SenderEnvelopeId,
            response.EnvelopeCfeCount,
            response.RespondedCount,
            response.AcceptedCount,
            response.RejectedCount,
            response.ObservedCount,
            response.OtherRejectedCount,
            detailsJson,
            response.ResponseXml,
            Sha256(response.ResponseXml),
            consultedAt);
        EnsureStoredIntegrity(consultation);

        try
        {
            return await _transactions.ExecuteAsync(async ct =>
            {
                var concurrentReplay = await _consultations.GetByOperationAsync(
                    normalized.OrganizationId,
                    normalized.OperationId,
                    ct);
                if (concurrentReplay is not null)
                {
                    EnsureStoredIntegrity(concurrentReplay);
                    EnsureSameSource(concurrentReplay, source);
                    return Result(concurrentReplay, replayed: true);
                }

                await _consultations.AddAsync(consultation, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result(consultation, replayed: false);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && _conflicts.IsUniqueConstraintConflict(ex))
        {
            var concurrent = await _consultations.GetByOperationAsync(
                normalized.OrganizationId,
                normalized.OperationId,
                cancellationToken)
                ?? throw Conflict(
                    "fiscal.envelope.document_response.concurrent_consultation_unresolved",
                    "A concurrent document-response consultation conflict could not be reconciled to durable evidence.",
                    "concurrent_uniqueness_conflict");
            EnsureStoredIntegrity(concurrent);
            EnsureSameSource(concurrent, source);
            return Result(concurrent, replayed: true);
        }
    }

    private async Task<SourceEvidence> RequiredSourceAsync(
        ConsultFiscalCfeEnvelopeDocumentResponseCommand command,
        CancellationToken cancellationToken)
    {
        var envelope = await _envelopes.GetByIdentityAsync(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.envelope_required",
                "A durable Sobre is required before document-response consultation.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureEnvelopeIntegrity(envelope);

        var submission = await _submissions.GetByEnvelopeIdAsync(envelope.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.submission_required",
                "A durable Sobre submission is required before document-response consultation.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity(submission);
        if (submission.State != FiscalCfeEnvelopeSubmissionState.ResponseReceived
            || string.IsNullOrWhiteSpace(submission.ResponseXml)
            || !Sha256Value(submission.ResponseSha256)
            || !string.Equals(submission.ResponseSha256, Sha256(submission.ResponseXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.document_response.response_required",
                "Document-response consultation requires exact durable ResponseReceived ACKSobre evidence.",
                "missing_prerequisite");
        }

        var observation = await _ackObservations.GetBySubmissionIdAsync(submission.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.ack_required",
                "A durable ACKSobre observation is required before document-response consultation.",
                "missing_prerequisite");
        ObserveFiscalCfeEnvelopeAckUseCase.EnsureObservationIntegrity(observation);
        if (observation.State != FiscalCfeEnvelopeAckState.Received)
        {
            throw Conflict(
                "fiscal.envelope.document_response.accepted_ack_required",
                "Only an accepted AS ACKSobre can authorize document-response consultation.",
                "missing_prerequisite");
        }
        if (string.IsNullOrWhiteSpace(observation.ConsultationToken)
            || string.IsNullOrWhiteSpace(observation.ConsultationAvailableAtText))
        {
            throw Conflict(
                "fiscal.envelope.document_response.consultation_parameters_required",
                "ACKSobre must contain the governed IdReceptor + Token consultation evidence.",
                "missing_prerequisite");
        }
        if (observation.DgiReceiverId is < 0 or > 9_999_999_999L
            || observation.ConsultationToken.Length > 8192)
        {
            throw Conflict(
                "fiscal.envelope.document_response.consultation_parameters_invalid",
                "Persisted ACKSobre consultation parameters exceed the governed DGI bounds.",
                "invalid_persisted_evidence");
        }

        return new(envelope, submission, observation);
    }

    private async Task<IReadOnlySet<DocumentIdentity>> ExpectedDocumentsAsync(
        StoredFiscalCfeEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var identities = new HashSet<DocumentIdentity>();
        foreach (var documentId in envelope.FiscalDocumentIds)
        {
            var document = await _documents.GetAsync(envelope.OrganizationId, documentId, cancellationToken)
                ?? throw Conflict(
                    "fiscal.envelope.document_response.document_required",
                    "Every CFE referenced by the durable Sobre must still resolve to immutable fiscal identity evidence.",
                    "invalid_persisted_evidence");
            var identity = new DocumentIdentity((int)document.CfeType, document.Series, document.Number);
            if (!identities.Add(identity))
            {
                throw Conflict(
                    "fiscal.envelope.document_response.document_identity_duplicate",
                    "The durable Sobre resolves to duplicate CFE identities.",
                    "invalid_persisted_evidence");
            }
        }
        return identities;
    }

    private static void EnsureExternalEvidence(
        FiscalCfeEnvelopeDocumentResponseConsultationResponse response,
        SourceEvidence source,
        IReadOnlySet<DocumentIdentity> expectedDocuments)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (!string.Equals(response.IssuerRuc?.Trim(), source.Envelope.IssuerRuc, StringComparison.Ordinal)
            || !string.Equals(response.ReceiverRut?.Trim(), source.Envelope.ReceiverRut, StringComparison.Ordinal)
            || response.SenderEnvelopeId != source.Envelope.SenderEnvelopeId
            || response.DgiReceiverId != source.Observation.DgiReceiverId
            || response.EnvelopeCfeCount != source.Envelope.CfeCount)
        {
            throw ExternalEvidenceInvalid("ACKCFE caratula does not correlate to the durable Sobre and ACKSobre evidence.");
        }
        if (response.DgiResponseId is < 0 or > 9_999_999_999L
            || response.RespondedCount is < 0 or > 250
            || response.AcceptedCount is < 0 or > 250
            || response.RejectedCount is < 0 or > 250
            || response.ObservedCount is < 0 or > 250
            || response.OtherRejectedCount is < 0 or > 250
            || response.Details is null
            || response.Details.Count != response.RespondedCount
            || response.Details.Count > source.Envelope.CfeCount
            || string.IsNullOrWhiteSpace(response.ResponseXml))
        {
            throw ExternalEvidenceInvalid("ACKCFE response counts or payload are outside the governed bounds.");
        }

        var ordinals = new HashSet<int>();
        var responseIdentities = new HashSet<DocumentIdentity>();
        foreach (var detail in response.Details)
        {
            if (detail.Ordinal is < 1 or > 250
                || !ordinals.Add(detail.Ordinal)
                || detail.CfeType <= 0
                || string.IsNullOrWhiteSpace(detail.Series)
                || detail.Series.Trim().Length > 20
                || detail.Number <= 0
                || string.IsNullOrWhiteSpace(detail.StateCode)
                || detail.StateCode.Trim().Length > 40)
            {
                throw ExternalEvidenceInvalid("ACKCFE detail evidence is incomplete or duplicated.");
            }

            var identity = new DocumentIdentity(detail.CfeType, detail.Series.Trim(), detail.Number);
            if (!responseIdentities.Add(identity) || !expectedDocuments.Contains(identity))
            {
                throw ExternalEvidenceInvalid("ACKCFE detail does not map uniquely to a CFE contained in the durable Sobre.");
            }
        }
    }

    internal static void EnsureStoredIntegrity(StoredFiscalCfeEnvelopeDocumentResponseConsultation value)
    {
        IReadOnlyList<FiscalCfeEnvelopeDocumentResponseDetail>? details;
        try
        {
            details = JsonSerializer.Deserialize<List<FiscalCfeEnvelopeDocumentResponseDetail>>(value.DetailsJson);
        }
        catch (JsonException)
        {
            details = null;
        }

        if (value.Id == Guid.Empty
            || value.AckObservationId == Guid.Empty
            || value.SubmissionId == Guid.Empty
            || value.EnvelopeId == Guid.Empty
            || string.IsNullOrWhiteSpace(value.OrganizationId)
            || string.IsNullOrWhiteSpace(value.OperationId)
            || value.OperationId.Length > 120
            || !Sha256Value(value.SourceAckResponseSha256)
            || value.DgiReceiverId is < 0 or > 9_999_999_999L
            || !Sha256Value(value.ConsultationTokenSha256)
            || value.DgiResponseId is < 0 or > 9_999_999_999L
            || !TwelveDigits(value.IssuerRuc)
            || !TwelveDigits(value.ReceiverRut)
            || value.SenderEnvelopeId is < 0 or > 9_999_999_999L
            || value.EnvelopeCfeCount is < 1 or > 250
            || value.RespondedCount is < 0 or > 250
            || value.AcceptedCount is < 0 or > 250
            || value.RejectedCount is < 0 or > 250
            || value.ObservedCount is < 0 or > 250
            || value.OtherRejectedCount is < 0 or > 250
            || details is null
            || details.Count != value.RespondedCount
            || string.IsNullOrWhiteSpace(value.ResponseXml)
            || !Sha256Value(value.ResponseSha256)
            || !string.Equals(value.ResponseSha256, Sha256(value.ResponseXml), StringComparison.Ordinal)
            || value.ConsultedAtUtc == default)
        {
            throw Conflict(
                "fiscal.envelope.document_response.persisted_evidence_invalid",
                "Persisted ACKCFE consultation evidence is incomplete or internally inconsistent.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSameSource(
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
                "fiscal.envelope.document_response.persisted_source_mismatch",
                "Persisted ACKCFE consultation evidence no longer matches its durable Sobre source.",
                "invalid_persisted_evidence");
        }
    }

    private static FiscalCfeEnvelopeDocumentResponseConsultationResult Result(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation value,
        bool replayed)
    {
        var details = JsonSerializer.Deserialize<List<FiscalCfeEnvelopeDocumentResponseDetail>>(value.DetailsJson)
            ?? throw Conflict(
                "fiscal.envelope.document_response.persisted_evidence_invalid",
                "Persisted ACKCFE detail evidence is invalid.",
                "invalid_persisted_evidence");
        return new(
            value.Id,
            value.AckObservationId,
            value.SubmissionId,
            value.EnvelopeId,
            value.OrganizationId,
            value.OperationId,
            value.DgiReceiverId,
            value.DgiResponseId,
            value.EnvelopeCfeCount,
            value.RespondedCount,
            value.AcceptedCount,
            value.RejectedCount,
            value.ObservedCount,
            value.OtherRejectedCount,
            details,
            value.ResponseXml,
            value.ResponseSha256,
            value.ConsultedAtUtc,
            replayed);
    }

    private static DateTimeOffset WholeSecond(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static bool Sha256Value(string? value) =>
        value is not null && value.Length == 64 && value.All(Uri.IsHexDigit);

    private static bool TwelveDigits(string? value) =>
        value is not null && value.Length == 12 && value.All(char.IsDigit);

    private static ApplicationProblemException ExternalEvidenceInvalid(string message) =>
        Conflict(
            "fiscal.envelope.document_response.external_evidence_invalid",
            message,
            "external_evidence_invalid");

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);

    private sealed record SourceEvidence(
        StoredFiscalCfeEnvelope Envelope,
        StoredFiscalCfeEnvelopeSubmission Submission,
        StoredFiscalCfeEnvelopeAckObservation Observation);

    private sealed record DocumentIdentity(int CfeType, string Series, long Number);
}
