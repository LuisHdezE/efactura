using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

/// <summary>
/// Clock boundary owned by the signing workflow. Implementations must supply the intended signing
/// instant including its UTC offset. The timestamp is consulted only before first durable evidence.
/// </summary>
public interface IFiscalSigningTimeSource
{
    DateTimeOffset GetSigningTimestamp();
}

/// <summary>
/// Future XMLDSig boundary. Infrastructure implementations own certificate/private-key access.
/// The request contains the deterministic signing payload with durable TmstFirma already inserted;
/// implementations may add ds:Signature but must not replace the payload or signing timestamp.
/// This slice defines the contract only and does not invoke it yet.
/// </summary>
public interface IFiscalSignatureProvider
{
    Task<FiscalSignatureResult> SignAsync(
        FiscalSignatureRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record FiscalSignatureRequest(
    Guid FiscalDocumentId,
    CfeFamily CfeFamily,
    string FormatVersion,
    string FiscalContentFingerprint,
    string UnsignedContentHash,
    string SigningPayloadXml,
    string SigningPayloadHash,
    DateTimeOffset SigningTimestamp);

public sealed record FiscalSignatureResult(string SignedXml);

public interface IFiscalSigningEvidenceRepository
{
    Task<FiscalSigningEvidence?> GetByFiscalDocumentAsync(
        string organizationId,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        FiscalSigningEvidence evidence,
        CancellationToken cancellationToken = default);
}

public sealed record PrepareFiscalSigningEvidenceCommand(
    string OrganizationId,
    Guid FiscalDocumentId);

public sealed record FiscalSigningEvidenceResult(
    Guid SigningEvidenceId,
    Guid FiscalDocumentId,
    string FiscalContentFingerprint,
    string UnsignedContentHash,
    DateTimeOffset SigningTimestamp,
    bool Replayed);

public sealed record FiscalSigningEvidenceEstablishedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SigningEvidenceId,
    Guid FiscalDocumentId,
    string OrganizationId,
    string FiscalContentFingerprint,
    string UnsignedContentHash,
    DateTimeOffset SigningTimestamp) : IIntegrationEvent;

/// <summary>
/// Establishes replay-safe signing evidence before any certificate/private-key boundary is crossed.
/// The unsigned artifact is rebuilt from immutable evidence and hashed first. Only when no durable
/// evidence exists is the signing clock consulted. Replays verify the same snapshot/hash and reuse
/// the persisted signing timestamp without consulting the clock.
/// </summary>
public sealed class PrepareFiscalSigningEvidenceUseCase
{
    private readonly IFiscalDocumentRepository _documents;
    private readonly IFiscalContentSnapshotRepository _snapshots;
    private readonly IFiscalXmlBuilder _builder;
    private readonly IFiscalSigningEvidenceRepository _signingEvidence;
    private readonly IFiscalSigningTimeSource _signingTime;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public PrepareFiscalSigningEvidenceUseCase(
        IFiscalDocumentRepository documents,
        IFiscalContentSnapshotRepository snapshots,
        IFiscalXmlBuilder builder,
        IFiscalSigningEvidenceRepository signingEvidence,
        IFiscalSigningTimeSource signingTime,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _documents = documents;
        _snapshots = snapshots;
        _builder = builder;
        _signingEvidence = signingEvidence;
        _signingTime = signingTime;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<FiscalSigningEvidenceResult> ExecuteAsync(
        PrepareFiscalSigningEvidenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId))
            throw Validation("fiscal.signing.organization_required", "Organization id is required.");
        if (command.FiscalDocumentId == Guid.Empty)
            throw Validation("fiscal.signing.document_id_required", "Fiscal document id is required.");

        var organizationId = command.OrganizationId.Trim();
        return _transactions.ExecuteAsync(async ct =>
        {
            var document = await _documents.GetAsync(organizationId, command.FiscalDocumentId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "fiscal.document_not_found",
                    "Fiscal document was not found.");

            var storedSnapshot = await _snapshots.GetByFiscalDocumentAsync(organizationId, document.Id, ct)
                ?? throw Conflict(
                    "fiscal.signing.snapshot_required",
                    "Signing evidence requires the immutable fiscal content snapshot.",
                    "missing_prerequisite");

            UnsignedCfeArtifact unsigned;
            try
            {
                storedSnapshot.Snapshot.EnsureIntegrity();
                unsigned = _builder.Build(document, storedSnapshot.Snapshot);
            }
            catch (DomainRuleException ex)
            {
                throw Map(ex);
            }

            var unsignedHash = Sha256(unsigned.Xml);
            var existing = await _signingEvidence.GetByFiscalDocumentAsync(organizationId, document.Id, ct);
            if (existing is not null)
            {
                EnsureReplayMatches(existing, storedSnapshot.Snapshot.ContentFingerprint, unsignedHash);
                return Result(existing, true);
            }

            FiscalSigningEvidence evidence;
            try
            {
                evidence = FiscalSigningEvidence.Establish(
                    Guid.NewGuid(),
                    organizationId,
                    document.Id,
                    storedSnapshot.Snapshot.ContentFingerprint,
                    unsignedHash,
                    _signingTime.GetSigningTimestamp());
            }
            catch (DomainRuleException ex)
            {
                throw Map(ex);
            }

            await _signingEvidence.AddAsync(evidence, ct);

            var occurredAt = evidence.SigningTimestamp.ToUniversalTime();
            var actor = _actors.Current;
            var correlation = _correlations.Current;
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(),
                occurredAt,
                "FISCAL_SIGNING_EVIDENCE_ESTABLISHED",
                actor.ActorId,
                organizationId,
                document.LocationId,
                document.TerminalId,
                "FiscalSigningEvidence",
                evidence.Id.ToString(),
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["fiscalDocumentId"] = document.Id.ToString(),
                    ["fiscalContentFingerprint"] = evidence.FiscalContentFingerprint,
                    ["unsignedContentHash"] = evidence.UnsignedContentHash,
                    ["signingTimestamp"] = evidence.SigningTimestamp.ToString("O")
                }),
                ct);

            await _outbox.EnqueueAsync(
                new FiscalSigningEvidenceEstablishedIntegrationEvent(
                    Guid.NewGuid(),
                    occurredAt,
                    evidence.Id,
                    document.Id,
                    organizationId,
                    evidence.FiscalContentFingerprint,
                    evidence.UnsignedContentHash,
                    evidence.SigningTimestamp),
                new OutboxContext(
                    correlation.CorrelationId,
                    null,
                    organizationId,
                    actor.ActorId),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return Result(evidence, false);
        }, cancellationToken);
    }

    private static void EnsureReplayMatches(
        FiscalSigningEvidence evidence,
        string fiscalContentFingerprint,
        string unsignedContentHash)
    {
        if (!string.Equals(evidence.FiscalContentFingerprint, fiscalContentFingerprint, StringComparison.Ordinal)
            || !string.Equals(evidence.UnsignedContentHash, unsignedContentHash, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.signing.replay_evidence_mismatch",
                "Durable signing evidence no longer matches the immutable snapshot and unsigned CFE content.",
                "inconsistent_replay");
        }
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static FiscalSigningEvidenceResult Result(FiscalSigningEvidence evidence, bool replayed) =>
        new(
            evidence.Id,
            evidence.FiscalDocumentId,
            evidence.FiscalContentFingerprint,
            evidence.UnsignedContentHash,
            evidence.SigningTimestamp,
            replayed);

    private static ApplicationProblemException Map(DomainRuleException ex) =>
        Validation(ex.Code, ex.Message);

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(
            ApplicationProblemKind.Conflict,
            code,
            message,
            conflictType: conflictType);
}
