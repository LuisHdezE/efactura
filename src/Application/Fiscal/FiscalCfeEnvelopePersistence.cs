using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public sealed record PersistFiscalCfeEnvelopeCommand(
    string OrganizationId,
    string ReceiverRut,
    string IssuerRuc,
    long SenderEnvelopeId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> FiscalDocumentIds,
    string OperationId);

public sealed record StoredFiscalCfeEnvelope(
    Guid Id,
    string OrganizationId,
    string ReceiverRut,
    string IssuerRuc,
    long SenderEnvelopeId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> FiscalDocumentIds,
    string OperationId,
    int CfeCount,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string EnvelopeXml,
    string EnvelopeSha256,
    string SchemaSetId,
    string SchemaVersion,
    string SchemaSetFingerprint);

public interface IFiscalCfeEnvelopeRepository
{
    Task<StoredFiscalCfeEnvelope?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalCfeEnvelope?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        string receiverRut,
        long senderEnvelopeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeEnvelope envelope,
        CancellationToken cancellationToken = default);
}

public sealed record FiscalCfeEnvelopePersistenceResult(
    Guid EnvelopeId,
    string OrganizationId,
    string ReceiverRut,
    string IssuerRuc,
    long SenderEnvelopeId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> FiscalDocumentIds,
    string OperationId,
    int CfeCount,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string EnvelopeXml,
    string EnvelopeSha256,
    string SchemaSetId,
    string SchemaVersion,
    string SchemaSetFingerprint,
    bool Replayed);

/// <summary>
/// Persists one already-packaged DGI Sobre identity without allocating Idemisor and without crossing
/// any transport boundary. DGI defines Idemisor as a number assigned by the issuer to the shipment,
/// but the governed evidence used by this consumer does not define a sequence/allocation algorithm.
/// Therefore SenderEnvelopeId remains explicit caller input.
/// </summary>
public sealed class PersistFiscalCfeEnvelopeUseCase
{
    private readonly PackageFiscalCfeEnvelopeUseCase _packager;
    private readonly IFiscalCfeEnvelopeRepository _envelopes;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public PersistFiscalCfeEnvelopeUseCase(
        PackageFiscalCfeEnvelopeUseCase packager,
        IFiscalCfeEnvelopeRepository envelopes,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _packager = packager;
        _envelopes = envelopes;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public Task<FiscalCfeEnvelopePersistenceResult> ExecuteAsync(
        PersistFiscalCfeEnvelopeCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var normalized = Normalize(command);

        return _transactions.ExecuteAsync(async ct =>
        {
            var existingOperation = await _envelopes.GetByOperationIdAsync(
                normalized.OrganizationId,
                normalized.OperationId,
                ct);
            if (existingOperation is not null)
            {
                EnsureStoredIntegrity(existingOperation);
                EnsureSameCommand(existingOperation, normalized, "fiscal.envelope.persistence.operation_replay_mismatch");
                return Result(existingOperation, true);
            }

            var existingIdentity = await _envelopes.GetByIdentityAsync(
                normalized.OrganizationId,
                normalized.IssuerRuc,
                normalized.ReceiverRut,
                normalized.SenderEnvelopeId,
                ct);
            if (existingIdentity is not null)
            {
                EnsureStoredIntegrity(existingIdentity);
                EnsureSameCommand(existingIdentity, normalized, "fiscal.envelope.persistence.identity_payload_conflict");
                return Result(existingIdentity, true);
            }

            var package = await _packager.ExecuteAsync(new PackageFiscalCfeEnvelopeCommand(
                normalized.OrganizationId,
                normalized.ReceiverRut,
                normalized.IssuerRuc,
                normalized.SenderEnvelopeId,
                normalized.CreatedAt,
                normalized.FiscalDocumentIds),
                ct);

            var stored = new StoredFiscalCfeEnvelope(
                Guid.NewGuid(),
                package.OrganizationId,
                package.ReceiverRut,
                package.IssuerRuc,
                package.SenderEnvelopeId,
                package.CreatedAt,
                package.FiscalDocumentIds.ToArray(),
                normalized.OperationId,
                package.CfeCount,
                package.CertificateThumbprint,
                package.CertificateSerialNumber,
                package.EnvelopeXml,
                package.EnvelopeSha256,
                package.SchemaSetId,
                package.SchemaVersion,
                package.SchemaSetFingerprint);

            EnsureStoredIntegrity(stored);
            await _envelopes.AddAsync(stored, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result(stored, false);
        }, cancellationToken);
    }

    private static PersistFiscalCfeEnvelopeCommand Normalize(PersistFiscalCfeEnvelopeCommand command) =>
        command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            ReceiverRut = command.ReceiverRut.Trim(),
            IssuerRuc = command.IssuerRuc.Trim(),
            CreatedAt = WholeSecond(command.CreatedAt),
            OperationId = command.OperationId.Trim(),
            FiscalDocumentIds = command.FiscalDocumentIds.ToArray()
        };

    private static void Validate(PersistFiscalCfeEnvelopeCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
            throw Validation("fiscal.envelope.persistence.organization_invalid", "Organization id is required and must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(command.ReceiverRut) || command.ReceiverRut.Trim().Length != 12 || command.ReceiverRut.Trim().Any(c => !char.IsDigit(c)))
            throw Validation("fiscal.envelope.persistence.receiver_rut_invalid", "Receiver RUT must contain exactly 12 digits.");
        if (string.IsNullOrWhiteSpace(command.IssuerRuc) || command.IssuerRuc.Trim().Length != 12 || command.IssuerRuc.Trim().Any(c => !char.IsDigit(c)))
            throw Validation("fiscal.envelope.persistence.issuer_ruc_invalid", "Issuer RUC must contain exactly 12 digits.");
        if (command.SenderEnvelopeId is < 0 or > 9_999_999_999L)
            throw Validation("fiscal.envelope.persistence.sender_id_invalid", "DGI Idemisor must be between 0 and 9999999999.");
        if (command.CreatedAt == default)
            throw Validation("fiscal.envelope.persistence.created_at_required", "Sobre creation timestamp is required.");
        if (command.FiscalDocumentIds is null || command.FiscalDocumentIds.Count is < 1 or > 250)
            throw Validation("fiscal.envelope.persistence.cfe_count_invalid", "A DGI Sobre must contain between 1 and 250 CFEs.");
        if (command.FiscalDocumentIds.Any(id => id == Guid.Empty))
            throw Validation("fiscal.envelope.persistence.document_id_invalid", "Every fiscal document id must be non-empty.");
        if (command.FiscalDocumentIds.Distinct().Count() != command.FiscalDocumentIds.Count)
            throw Validation("fiscal.envelope.persistence.duplicate_document", "A CFE may appear only once in the same Sobre.");
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
            throw Validation("fiscal.envelope.persistence.operation_id_invalid", "Operation id is required and must not exceed 120 characters.");
    }

    private static void EnsureStoredIntegrity(StoredFiscalCfeEnvelope stored)
    {
        if (stored.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(stored.OrganizationId)
            || stored.OrganizationId.Length > 200
            || !TwelveDigits(stored.ReceiverRut)
            || !TwelveDigits(stored.IssuerRuc)
            || stored.SenderEnvelopeId is < 0 or > 9_999_999_999L
            || stored.CreatedAt == default
            || stored.CreatedAt.Ticks % TimeSpan.TicksPerSecond != 0
            || string.IsNullOrWhiteSpace(stored.OperationId)
            || stored.OperationId.Length > 120
            || stored.FiscalDocumentIds is null
            || stored.FiscalDocumentIds.Count is < 1 or > 250
            || stored.FiscalDocumentIds.Any(id => id == Guid.Empty)
            || stored.FiscalDocumentIds.Distinct().Count() != stored.FiscalDocumentIds.Count
            || stored.CfeCount != stored.FiscalDocumentIds.Count
            || string.IsNullOrWhiteSpace(stored.CertificateThumbprint)
            || string.IsNullOrWhiteSpace(stored.CertificateSerialNumber)
            || string.IsNullOrWhiteSpace(stored.EnvelopeXml)
            || string.IsNullOrWhiteSpace(stored.SchemaSetId)
            || string.IsNullOrWhiteSpace(stored.SchemaVersion)
            || !Sha256Value(stored.EnvelopeSha256)
            || !Sha256Value(stored.SchemaSetFingerprint)
            || !string.Equals(stored.EnvelopeSha256, Sha256(stored.EnvelopeXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.persistence.persisted_evidence_invalid",
                "Persisted Sobre evidence is incomplete or internally inconsistent.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSameCommand(
        StoredFiscalCfeEnvelope stored,
        PersistFiscalCfeEnvelopeCommand command,
        string code)
    {
        if (!string.Equals(stored.OrganizationId, command.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(stored.ReceiverRut, command.ReceiverRut, StringComparison.Ordinal)
            || !string.Equals(stored.IssuerRuc, command.IssuerRuc, StringComparison.Ordinal)
            || stored.SenderEnvelopeId != command.SenderEnvelopeId
            || stored.CreatedAt.UtcDateTime != command.CreatedAt.UtcDateTime
            || stored.CreatedAt.Offset != command.CreatedAt.Offset
            || !stored.FiscalDocumentIds.SequenceEqual(command.FiscalDocumentIds))
        {
            throw Conflict(
                code,
                "The durable Sobre identity was already used for different immutable packaging input.",
                "inconsistent_replay");
        }
    }

    private static DateTimeOffset WholeSecond(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Offset);

    private static bool TwelveDigits(string value) =>
        value.Length == 12 && value.All(char.IsDigit);

    private static bool Sha256Value(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static FiscalCfeEnvelopePersistenceResult Result(StoredFiscalCfeEnvelope stored, bool replayed) =>
        new(
            stored.Id,
            stored.OrganizationId,
            stored.ReceiverRut,
            stored.IssuerRuc,
            stored.SenderEnvelopeId,
            stored.CreatedAt,
            stored.FiscalDocumentIds,
            stored.OperationId,
            stored.CfeCount,
            stored.CertificateThumbprint,
            stored.CertificateSerialNumber,
            stored.EnvelopeXml,
            stored.EnvelopeSha256,
            stored.SchemaSetId,
            stored.SchemaVersion,
            stored.SchemaSetFingerprint,
            replayed);

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}
