using EFactura.Application.Common.Errors;

namespace EFactura.Application.Fiscal;

public sealed record PlanFiscalCfeEnvelopeBatchesCommand(
    string OrganizationId,
    IReadOnlyList<Guid> FiscalDocumentIds);

public sealed record FiscalCfeEnvelopeBatchPlan(
    int BatchOrdinal,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    IReadOnlyList<Guid> FiscalDocumentIds)
{
    public int CfeCount => FiscalDocumentIds.Count;
}

public sealed record FiscalCfeEnvelopeBatchPlanResult(
    string OrganizationId,
    int TotalDocuments,
    IReadOnlyList<FiscalCfeEnvelopeBatchPlan> Batches);

/// <summary>
/// Plans deterministic local product batches from an explicit caller-supplied CFE candidate set.
/// This boundary does not discover pending documents, allocate Idemisor values, package XML,
/// persist envelopes, dispatch to DGI or interpret DGI responses. It only respects the already
/// governed Sobre constraints that one batch contains at most 250 CFE and never mixes signing certificates.
/// </summary>
public sealed class PlanFiscalCfeEnvelopeBatchesUseCase
{
    private const int MaxCfePerEnvelope = 250;

    private readonly IFiscalSignedArtifactRepository _signedArtifacts;

    public PlanFiscalCfeEnvelopeBatchesUseCase(IFiscalSignedArtifactRepository signedArtifacts)
    {
        _signedArtifacts = signedArtifacts;
    }

    public async Task<FiscalCfeEnvelopeBatchPlanResult> ExecuteAsync(
        PlanFiscalCfeEnvelopeBatchesCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        var organizationId = command.OrganizationId.Trim();
        var ids = command.FiscalDocumentIds.ToArray();
        if (ids.Distinct().Count() != ids.Length)
        {
            throw Validation(
                "fiscal.envelope_batch.duplicate_document",
                "A fiscal document may appear only once in the same batch-planning request.");
        }

        var groups = new List<CertificateGroup>();
        var groupByCertificate = new Dictionary<CertificateKey, CertificateGroup>();

        foreach (var fiscalDocumentId in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var artifact = await _signedArtifacts.GetByFiscalDocumentAsync(
                organizationId,
                fiscalDocumentId,
                cancellationToken)
                ?? throw Conflict(
                    "fiscal.envelope_batch.signed_artifact_required",
                    "Batch planning requires a durable signed CFE artifact for every explicitly selected fiscal document.",
                    "missing_prerequisite");

            EnsureArtifactAssociation(artifact, organizationId, fiscalDocumentId);

            var key = new CertificateKey(
                artifact.CertificateThumbprint,
                artifact.CertificateSerialNumber);

            if (!groupByCertificate.TryGetValue(key, out var group))
            {
                group = new CertificateGroup(key);
                groupByCertificate.Add(key, group);
                groups.Add(group);
            }

            group.DocumentIds.Add(fiscalDocumentId);
        }

        var batches = new List<FiscalCfeEnvelopeBatchPlan>();
        var ordinal = 1;

        foreach (var group in groups)
        {
            for (var offset = 0; offset < group.DocumentIds.Count; offset += MaxCfePerEnvelope)
            {
                var chunk = group.DocumentIds
                    .Skip(offset)
                    .Take(MaxCfePerEnvelope)
                    .ToArray();

                batches.Add(new FiscalCfeEnvelopeBatchPlan(
                    ordinal++,
                    group.Key.Thumbprint,
                    group.Key.SerialNumber,
                    chunk));
            }
        }

        return new FiscalCfeEnvelopeBatchPlanResult(
            organizationId,
            ids.Length,
            batches);
    }

    private static void Validate(PlanFiscalCfeEnvelopeBatchesCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.OrganizationId)
            || command.OrganizationId.Trim().Length > 200)
        {
            throw Validation(
                "fiscal.envelope_batch.organization_invalid",
                "Organization id is required and must not exceed 200 characters.");
        }

        if (command.FiscalDocumentIds is null || command.FiscalDocumentIds.Count == 0)
        {
            throw Validation(
                "fiscal.envelope_batch.documents_required",
                "At least one explicit fiscal document id is required for batch planning.");
        }

        if (command.FiscalDocumentIds.Any(id => id == Guid.Empty))
        {
            throw Validation(
                "fiscal.envelope_batch.document_id_invalid",
                "Every fiscal document id must be non-empty.");
        }
    }

    private static void EnsureArtifactAssociation(
        StoredFiscalSignedArtifact artifact,
        string organizationId,
        Guid fiscalDocumentId)
    {
        if (artifact.Id == Guid.Empty
            || artifact.FiscalDocumentId != fiscalDocumentId
            || !string.Equals(artifact.OrganizationId, organizationId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(artifact.CertificateThumbprint)
            || string.IsNullOrWhiteSpace(artifact.CertificateSerialNumber))
        {
            throw Conflict(
                "fiscal.envelope_batch.signed_artifact_invalid",
                "Persisted signed CFE evidence is incomplete or belongs to another fiscal identity.",
                "invalid_persisted_evidence");
        }
    }

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);

    private sealed record CertificateKey(string Thumbprint, string SerialNumber);

    private sealed class CertificateGroup(CertificateKey key)
    {
        public CertificateKey Key { get; } = key;
        public List<Guid> DocumentIds { get; } = [];
    }
}
