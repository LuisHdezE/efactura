from pathlib import Path

ROOT = Path('.')

def write(path: str, content: str):
    p = ROOT / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(content, encoding='utf-8')

def replace(path: str, old: str, new: str):
    p = ROOT / path
    text = p.read_text(encoding='utf-8')
    if old not in text:
        raise SystemExit(f'marker not found in {path}: {old[:120]!r}')
    p.write_text(text.replace(old, new, 1), encoding='utf-8')

write('src/Application/Fiscal/FiscalDailyReportVersionLifecycle.cs', r'''using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public enum FiscalDailyReportRevisionKind
{
    Initial = 1,
    Correction = 2,
    FxReliquidation = 3
}

public sealed record StoredFiscalDailyReportVersion(
    Guid Id,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    Guid? PreviousVersionId,
    string OperationId,
    FiscalDailyReportRevisionKind RevisionKind,
    string ReasonCode,
    string ReconciliationFingerprint,
    bool RequiresFxReliquidation,
    DateTimeOffset CreatedAtUtc,
    string VersionFingerprint)
{
    public void EnsureIntegrity()
    {
        if (Id == Guid.Empty)
            throw Conflict("fiscal.daily_report.version.id_invalid", "Daily Report version identity is invalid.", "invalid_persisted_evidence");
        if (string.IsNullOrWhiteSpace(OrganizationId) || OrganizationId.Length > 200)
            throw Conflict("fiscal.daily_report.version.organization_invalid", "Daily Report version organization is invalid.", "invalid_persisted_evidence");
        if (string.IsNullOrWhiteSpace(IssuerRuc) || IssuerRuc.Length != 12 || IssuerRuc.Any(c => !char.IsDigit(c)))
            throw Conflict("fiscal.daily_report.version.ruc_invalid", "Daily Report version issuer RUC is invalid.", "invalid_persisted_evidence");
        if (SummaryDate == default)
            throw Conflict("fiscal.daily_report.version.summary_date_invalid", "Daily Report version summary date is invalid.", "invalid_persisted_evidence");
        if (Sequence is < 1 or > 99)
            throw Conflict("fiscal.daily_report.version.sequence_invalid", "Daily Report version sequence must be between 1 and 99.", "invalid_persisted_evidence");
        if (Sequence == 1 && PreviousVersionId is not null)
            throw Conflict("fiscal.daily_report.version.initial_previous_forbidden", "Initial Daily Report version cannot reference a previous version.", "invalid_persisted_evidence");
        if (Sequence > 1 && PreviousVersionId is null)
            throw Conflict("fiscal.daily_report.version.previous_required", "Corrected Daily Report version requires its previous version identity.", "invalid_persisted_evidence");
        Required(OperationId, 120, "fiscal.daily_report.version.operation_id_invalid");
        if (!Enum.IsDefined(RevisionKind))
            throw Conflict("fiscal.daily_report.version.revision_kind_invalid", "Daily Report revision kind is invalid.", "invalid_persisted_evidence");
        if (Sequence == 1 && RevisionKind != FiscalDailyReportRevisionKind.Initial)
            throw Conflict("fiscal.daily_report.version.initial_kind_invalid", "Sequence 1 must be the initial Daily Report version.", "invalid_persisted_evidence");
        if (Sequence > 1 && RevisionKind == FiscalDailyReportRevisionKind.Initial)
            throw Conflict("fiscal.daily_report.version.correction_kind_required", "Sequence greater than 1 must be a correction or reliquidation.", "invalid_persisted_evidence");
        Required(ReasonCode, 120, "fiscal.daily_report.version.reason_invalid");
        RequiredHash(ReconciliationFingerprint, "fiscal.daily_report.version.reconciliation_fingerprint_invalid");
        RequiredHash(VersionFingerprint, "fiscal.daily_report.version.fingerprint_invalid");
        if (!string.Equals(VersionFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Conflict("fiscal.daily_report.version.fingerprint_mismatch", "Persisted Daily Report version fingerprint does not match its immutable evidence.", "invalid_persisted_evidence");
    }

    public string ComputeFingerprint() => Hash(string.Join(
        "|",
        Id.ToString("N"),
        OrganizationId,
        IssuerRuc,
        SummaryDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
        PreviousVersionId?.ToString("N") ?? "-",
        OperationId,
        ((int)RevisionKind).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ReasonCode,
        ReconciliationFingerprint,
        RequiresFxReliquidation ? "1" : "0",
        CreatedAtUtc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

    internal static string Required(string? value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw Validation(code, "Required Daily Report version value is missing or too long.");
        return value;
    }

    internal static string RequiredHash(string? value, string code)
    {
        if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
            throw Validation(code, "Expected a SHA-256 hexadecimal fingerprint.");
        return value.ToLowerInvariant();
    }

    internal static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    internal static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    internal static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}

public interface IFiscalDailyReportVersionRepository
{
    Task<bool> AcquireOrganizationSequenceLockAsync(string organizationId, CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportVersion?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportVersion?> GetLatestAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportVersion?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default);

    Task AddAsync(StoredFiscalDailyReportVersion version, CancellationToken cancellationToken = default);
}

public sealed record AllocateFiscalDailyReportVersionCommand(
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    string OperationId,
    FiscalDailyReportRevisionKind RevisionKind,
    string? ReasonCode,
    IReadOnlyCollection<FiscalDailyReportDocumentEvidence>? CompleteDocuments = null,
    IReadOnlyCollection<FiscalDailyReportAnnulmentEvidence>? CompleteAnnulments = null);

public sealed record FiscalDailyReportVersionAllocatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VersionId,
    Guid? PreviousVersionId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    FiscalDailyReportRevisionKind RevisionKind,
    string ReasonCode,
    string ReconciliationFingerprint,
    bool RequiresFxReliquidation) : IIntegrationEvent;

public sealed record FiscalDailyReportVersionAllocationResult(
    Guid VersionId,
    Guid? PreviousVersionId,
    FiscalDailyReportSnapshot Snapshot,
    FiscalDailyReportRevisionKind RevisionKind,
    string ReasonCode,
    bool RequiresFxReliquidation,
    string VersionFingerprint,
    bool Replayed);

/// <summary>
/// Allocates the DGI Reporte Diario SecEnvio lifecycle. Sequence 1 is the first complete daily
/// report. Corrections/reliquidations are complete replacement reports and must use previous + 1.
/// The application never exposes a patch/delta report command.
/// </summary>
public sealed class AllocateFiscalDailyReportVersionUseCase
{
    private readonly IFiscalDailyReportVersionRepository _versions;
    private readonly IFiscalDailyReportSignedArtifactRepository _signedArtifacts;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public AllocateFiscalDailyReportVersionUseCase(
        IFiscalDailyReportVersionRepository versions,
        IFiscalDailyReportSignedArtifactRepository signedArtifacts,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _versions = versions;
        _signedArtifacts = signedArtifacts;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<FiscalDailyReportVersionAllocationResult> ExecuteAsync(
        AllocateFiscalDailyReportVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        StoredFiscalDailyReportVersion.Required(command.OrganizationId, 200, "fiscal.daily_report.version.organization_required");
        StoredFiscalDailyReportVersion.Required(command.OperationId, 120, "fiscal.daily_report.version.operation_id_required");
        if (command.SummaryDate == default)
            throw StoredFiscalDailyReportVersion.Validation("fiscal.daily_report.version.summary_date_required", "Summary date is required.");
        if (!Enum.IsDefined(command.RevisionKind))
            throw StoredFiscalDailyReportVersion.Validation("fiscal.daily_report.version.revision_kind_invalid", "Daily Report revision kind is invalid.");

        var documents = (command.CompleteDocuments ?? Array.Empty<FiscalDailyReportDocumentEvidence>()).ToArray();
        var annulments = (command.CompleteAnnulments ?? Array.Empty<FiscalDailyReportAnnulmentEvidence>()).ToArray();

        return _transactions.ExecuteAsync(async ct =>
        {
            if (!await _versions.AcquireOrganizationSequenceLockAsync(command.OrganizationId, ct))
            {
                throw StoredFiscalDailyReportVersion.Validation(
                    "fiscal.daily_report.version.organization_not_configured",
                    "Daily Report sequence allocation requires an existing company fiscal profile.");
            }

            var operationId = command.OperationId.Trim();
            var replay = await _versions.GetByOperationIdAsync(command.OrganizationId, operationId, ct);
            if (replay is not null)
            {
                replay.EnsureIntegrity();
                var replaySnapshot = CreateSnapshot(command, replay.Sequence, documents, annulments);
                var expectedReason = NormalizeReason(command.RevisionKind, command.ReasonCode);
                EnsureReplayMatches(replay, command, replaySnapshot, expectedReason);
                return Result(replay, replaySnapshot, true);
            }

            var latest = await _versions.GetLatestAsync(command.OrganizationId, command.IssuerRuc, command.SummaryDate, ct);
            latest?.EnsureIntegrity();

            int sequence;
            Guid? previousVersionId;
            if (latest is null)
            {
                if (command.RevisionKind != FiscalDailyReportRevisionKind.Initial)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.initial_required",
                        "The first Daily Report version for the day must use the initial revision kind and SecEnvio 1.",
                        "sequence_transition_invalid");
                }
                sequence = 1;
                previousVersionId = null;
            }
            else
            {
                if (command.RevisionKind == FiscalDailyReportRevisionKind.Initial)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.initial_already_exists",
                        "An initial Daily Report version already exists for this issuer and summary date.",
                        "sequence_transition_invalid");
                }
                if (latest.Sequence >= 99)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.sequence_exhausted",
                        "Reporte Diario SecEnvio exhausted the pinned two-digit DGI field.",
                        "sequence_exhausted");
                }

                var priorSigned = await _signedArtifacts.GetByIdentityAsync(
                    latest.OrganizationId,
                    latest.IssuerRuc,
                    latest.SummaryDate,
                    latest.Sequence,
                    ct);
                if (priorSigned is null)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.prior_not_signed",
                        "A corrected Daily Report version cannot be allocated until the immediately previous version has a durable signed artifact.",
                        "sequence_transition_invalid");
                }

                sequence = checked(latest.Sequence + 1);
                previousVersionId = latest.Id;
            }

            var snapshot = CreateSnapshot(command, sequence, documents, annulments);
            var requiresFxReliquidation = snapshot.Documents.Any(document => document.CurrencyConversionRequiresReliquidation);
            var reasonCode = NormalizeReason(command.RevisionKind, command.ReasonCode);

            if (command.RevisionKind == FiscalDailyReportRevisionKind.FxReliquidation)
            {
                if (latest is null || !latest.RequiresFxReliquidation)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.fx_reliquidation_not_pending",
                        "FX reliquidation requires an immediately previous Daily Report version marked as requiring reliquidation.",
                        "sequence_transition_invalid");
                }
                if (requiresFxReliquidation)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.fx_reliquidation_unresolved",
                        "FX reliquidation cannot close while the complete replacement report still carries future-date reliquidation evidence.",
                        "sequence_transition_invalid");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var provisional = new StoredFiscalDailyReportVersion(
                Guid.NewGuid(),
                snapshot.OrganizationId,
                snapshot.IssuerRuc,
                snapshot.SummaryDate,
                snapshot.Sequence,
                previousVersionId,
                operationId,
                command.RevisionKind,
                reasonCode,
                snapshot.ReconciliationFingerprint,
                requiresFxReliquidation,
                now,
                new string('0', 64));
            var version = provisional with { VersionFingerprint = provisional.ComputeFingerprint() };
            version.EnsureIntegrity();

            await _versions.AddAsync(version, ct);

            var actor = _actors.Current;
            var correlation = _correlations.Current;
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(),
                now,
                "FISCAL_DAILY_REPORT_VERSION_ALLOCATED",
                actor.ActorId,
                version.OrganizationId,
                null,
                null,
                "FiscalDailyReportVersion",
                version.Id.ToString(),
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["issuerRuc"] = version.IssuerRuc,
                    ["summaryDate"] = version.SummaryDate.ToString("yyyy-MM-dd"),
                    ["sequence"] = version.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["previousVersionId"] = version.PreviousVersionId?.ToString(),
                    ["revisionKind"] = version.RevisionKind.ToString(),
                    ["reasonCode"] = version.ReasonCode,
                    ["reconciliationFingerprint"] = version.ReconciliationFingerprint,
                    ["requiresFxReliquidation"] = version.RequiresFxReliquidation ? "true" : "false"
                }),
                ct);

            await _outbox.EnqueueAsync(
                new FiscalDailyReportVersionAllocatedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    version.Id,
                    version.PreviousVersionId,
                    version.OrganizationId,
                    version.IssuerRuc,
                    version.SummaryDate,
                    version.Sequence,
                    version.RevisionKind,
                    version.ReasonCode,
                    version.ReconciliationFingerprint,
                    version.RequiresFxReliquidation),
                new OutboxContext(correlation.CorrelationId, null, version.OrganizationId, actor.ActorId),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return Result(version, snapshot, false);
        }, cancellationToken);
    }

    private static FiscalDailyReportSnapshot CreateSnapshot(
        AllocateFiscalDailyReportVersionCommand command,
        int sequence,
        IReadOnlyCollection<FiscalDailyReportDocumentEvidence> documents,
        IReadOnlyCollection<FiscalDailyReportAnnulmentEvidence> annulments)
    {
        try
        {
            return FiscalDailyReportSnapshot.Create(
                command.OrganizationId,
                command.IssuerRuc,
                command.SummaryDate,
                sequence,
                documents,
                annulments);
        }
        catch (EFactura.Domain.Common.DomainRuleException ex)
        {
            throw StoredFiscalDailyReportVersion.Validation(ex.Code, ex.Message);
        }
    }

    private static string NormalizeReason(FiscalDailyReportRevisionKind kind, string? reasonCode)
    {
        if (kind == FiscalDailyReportRevisionKind.Initial)
        {
            if (!string.IsNullOrWhiteSpace(reasonCode) && !string.Equals(reasonCode.Trim(), "initial", StringComparison.Ordinal))
            {
                throw StoredFiscalDailyReportVersion.Validation(
                    "fiscal.daily_report.version.initial_reason_invalid",
                    "Initial Daily Report version uses the fixed reason code 'initial'.");
            }
            return "initial";
        }

        return StoredFiscalDailyReportVersion.Required(
            reasonCode?.Trim(),
            120,
            "fiscal.daily_report.version.correction_reason_required");
    }

    private static void EnsureReplayMatches(
        StoredFiscalDailyReportVersion stored,
        AllocateFiscalDailyReportVersionCommand command,
        FiscalDailyReportSnapshot snapshot,
        string expectedReason)
    {
        var requiresFxReliquidation = snapshot.Documents.Any(document => document.CurrencyConversionRequiresReliquidation);
        if (!string.Equals(stored.OrganizationId, command.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(stored.IssuerRuc, command.IssuerRuc, StringComparison.Ordinal)
            || stored.SummaryDate != command.SummaryDate
            || stored.RevisionKind != command.RevisionKind
            || !string.Equals(stored.ReasonCode, expectedReason, StringComparison.Ordinal)
            || !string.Equals(stored.ReconciliationFingerprint, snapshot.ReconciliationFingerprint, StringComparison.Ordinal)
            || stored.RequiresFxReliquidation != requiresFxReliquidation)
        {
            throw StoredFiscalDailyReportVersion.Conflict(
                "fiscal.daily_report.version.operation_replay_mismatch",
                "Daily Report version operation id was already used with different immutable input.",
                "inconsistent_replay");
        }
    }

    private static FiscalDailyReportVersionAllocationResult Result(
        StoredFiscalDailyReportVersion version,
        FiscalDailyReportSnapshot snapshot,
        bool replayed) =>
        new(
            version.Id,
            version.PreviousVersionId,
            snapshot,
            version.RevisionKind,
            version.ReasonCode,
            version.RequiresFxReliquidation,
            version.VersionFingerprint,
            replayed);
}
''')

# Add persistence record.
replace(
    'src/Infrastructure/Persistence/V1/Write/Models/FiscalDailyReportRecords.cs',
    'namespace Infrastructure.Persistence.V1.Write.Models;\n\npublic sealed class V1FiscalDailyReportSigningEvidenceRecord',
    '''namespace Infrastructure.Persistence.V1.Write.Models;\n\npublic sealed class V1FiscalDailyReportVersionRecord\n{\n    public Guid Id { get; set; }\n    public string OrganizationId { get; set; } = string.Empty;\n    public string IssuerRuc { get; set; } = string.Empty;\n    public DateTime SummaryDate { get; set; }\n    public int Sequence { get; set; }\n    public Guid? PreviousVersionId { get; set; }\n    public string OperationId { get; set; } = string.Empty;\n    public int RevisionKind { get; set; }\n    public string ReasonCode { get; set; } = string.Empty;\n    public string ReconciliationFingerprint { get; set; } = string.Empty;\n    public bool RequiresFxReliquidation { get; set; }\n    public DateTimeOffset CreatedAtUtc { get; set; }\n    public string VersionFingerprint { get; set; } = string.Empty;\n}\n\npublic sealed class V1FiscalDailyReportSigningEvidenceRecord''')

# Add repository before existing repository classes.
repo_path = 'src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportRepositories.cs'
replace(
    repo_path,
    'namespace Infrastructure.Persistence.V1.Write.Repositories;\n\npublic sealed class EfFiscalDailyReportSigningEvidenceRepository',
    r'''namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalDailyReportVersionRepository : IFiscalDailyReportVersionRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportVersionRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> AcquireOrganizationSequenceLockAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Daily Report sequence lock requires an active transaction.");

        var provider = _dbContext.Database.ProviderName ?? string.Empty;
        string sql;
        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            sql = "SELECT * FROM \"v1_company_fiscal_profiles\" WHERE \"OrganizationId\" = {0} FOR UPDATE";
        }
        else if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            sql = "SELECT * FROM `v1_company_fiscal_profiles` WHERE `OrganizationId` = {0} FOR UPDATE";
        }
        else
        {
            throw new InvalidOperationException($"Unsupported provider for Daily Report sequence lock: {provider}");
        }

        var locked = await _dbContext.Set<V1CompanyFiscalProfileRecord>()
            .FromSqlRaw(sql, organizationId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return locked.Count == 1;
    }

    public async Task<StoredFiscalDailyReportVersion?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDailyReportVersionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return Map(record);
    }

    public async Task<StoredFiscalDailyReportVersion?> GetLatestAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var record = await _dbContext.Set<V1FiscalDailyReportVersionRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc && x.SummaryDate == date)
            .OrderByDescending(x => x.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
        return Map(record);
    }

    public async Task<StoredFiscalDailyReportVersion?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var record = await _dbContext.Set<V1FiscalDailyReportVersionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                    && x.IssuerRuc == issuerRuc
                    && x.SummaryDate == date
                    && x.Sequence == sequence,
                cancellationToken);
        return Map(record);
    }

    public Task AddAsync(StoredFiscalDailyReportVersion version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        version.EnsureIntegrity();
        _dbContext.Set<V1FiscalDailyReportVersionRecord>().Add(new V1FiscalDailyReportVersionRecord
        {
            Id = version.Id,
            OrganizationId = version.OrganizationId,
            IssuerRuc = version.IssuerRuc,
            SummaryDate = version.SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = version.Sequence,
            PreviousVersionId = version.PreviousVersionId,
            OperationId = version.OperationId,
            RevisionKind = (int)version.RevisionKind,
            ReasonCode = version.ReasonCode,
            ReconciliationFingerprint = version.ReconciliationFingerprint,
            RequiresFxReliquidation = version.RequiresFxReliquidation,
            CreatedAtUtc = version.CreatedAtUtc,
            VersionFingerprint = version.VersionFingerprint
        });
        return Task.CompletedTask;
    }

    private static StoredFiscalDailyReportVersion? Map(V1FiscalDailyReportVersionRecord? record) =>
        record is null ? null : new StoredFiscalDailyReportVersion(
            record.Id,
            record.OrganizationId,
            record.IssuerRuc,
            DateOnly.FromDateTime(record.SummaryDate),
            record.Sequence,
            record.PreviousVersionId,
            record.OperationId,
            (FiscalDailyReportRevisionKind)record.RevisionKind,
            record.ReasonCode,
            record.ReconciliationFingerprint,
            record.RequiresFxReliquidation,
            record.CreatedAtUtc,
            record.VersionFingerprint);
}

public sealed class EfFiscalDailyReportSigningEvidenceRepository''')

# Extend model customizer before signing evidence mapping.
replace(
    'src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs',
    'private static void ConfigureFiscalDailyReportDurability(ModelBuilder modelBuilder)\n{\n    modelBuilder.Entity<V1FiscalDailyReportSigningEvidenceRecord>(entity =>',
    r'''private static void ConfigureFiscalDailyReportDurability(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<V1FiscalDailyReportVersionRecord>(entity =>
    {
        entity.ToTable("v1_fiscal_daily_report_versions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
        entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
        entity.Property(x => x.SummaryDate).HasColumnType("date");
        entity.Property(x => x.OperationId).HasMaxLength(120).IsRequired();
        entity.Property(x => x.ReasonCode).HasMaxLength(120).IsRequired();
        entity.Property(x => x.ReconciliationFingerprint).HasMaxLength(64).IsRequired();
        entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
        entity.Property(x => x.VersionFingerprint).HasMaxLength(64).IsRequired();
        entity.HasOne<V1FiscalDailyReportVersionRecord>()
            .WithMany()
            .HasForeignKey(x => x.PreviousVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_v1_fdr_version_previous");
        entity.HasIndex(x => new { x.OrganizationId, x.IssuerRuc, x.SummaryDate, x.Sequence })
            .IsUnique()
            .HasDatabaseName("UX_v1_fdr_version_identity");
        entity.HasIndex(x => new { x.OrganizationId, x.OperationId })
            .IsUnique()
            .HasDatabaseName("UX_v1_fdr_version_operation");
        entity.HasIndex(x => x.PreviousVersionId)
            .HasDatabaseName("IX_v1_fdr_version_previous");
    });

    modelBuilder.Entity<V1FiscalDailyReportSigningEvidenceRecord>(entity =>''')

# Register repository/use case.
replace(
    'src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs',
    '        services.AddScoped<IFiscalDailyReportSigningEvidenceRepository, EfFiscalDailyReportSigningEvidenceRepository>();\n        services.AddScoped<IFiscalDailyReportSignedArtifactRepository, EfFiscalDailyReportSignedArtifactRepository>();',
    '        services.AddScoped<IFiscalDailyReportVersionRepository, EfFiscalDailyReportVersionRepository>();\n        services.AddScoped<IFiscalDailyReportSigningEvidenceRepository, EfFiscalDailyReportSigningEvidenceRepository>();\n        services.AddScoped<IFiscalDailyReportSignedArtifactRepository, EfFiscalDailyReportSignedArtifactRepository>();')
replace(
    'src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs',
    '        services.AddScoped<PrepareFiscalDailyReportSigningEvidenceUseCase>();\n        services.AddScoped<SignFiscalDailyReportUseCase>();',
    '        services.AddScoped<AllocateFiscalDailyReportVersionUseCase>();\n        services.AddScoped<PrepareFiscalDailyReportSigningEvidenceUseCase>();\n        services.AddScoped<SignFiscalDailyReportUseCase>();')

write('src/Infrastructure/Persistence/V1/Migrations/20260912043000_V1FiscalDailyReportSequenceLifecycle.cs', r'''using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260912043000_V1FiscalDailyReportSequenceLifecycle")]
public sealed class V1FiscalDailyReportSequenceLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_daily_report_versions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                Sequence = table.Column<int>(nullable: false),
                PreviousVersionId = table.Column<Guid>(nullable: true),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                RevisionKind = table.Column<int>(nullable: false),
                ReasonCode = table.Column<string>(maxLength: 120, nullable: false),
                ReconciliationFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                RequiresFxReliquidation = table.Column<bool>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                VersionFingerprint = table.Column<string>(maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_versions", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fdr_version_previous",
                    column: x => x.PreviousVersionId,
                    principalTable: "v1_fiscal_daily_report_versions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_version_identity",
            table: "v1_fiscal_daily_report_versions",
            columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_version_operation",
            table: "v1_fiscal_daily_report_versions",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_version_previous",
            table: "v1_fiscal_daily_report_versions",
            column: "PreviousVersionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_daily_report_versions");
}
''')

write('documentation/blueprint-api-implementation/49_FISCAL_DAILY_REPORT_SEQUENCE_LIFECYCLE.md', r'''# Fiscal Daily Report SecEnvio lifecycle and reliquidation lineage

## Regulatory boundary

This increment implements the sequence semantics already pinned in the byte-preserved DGI `ReporteDiarioCFE.xsd` used by the project. The schema annotation for `SecEnvio` states that the first daily submission uses sequence 1 and a correction must resend the complete file with `SecEnvio = previous sequence + 1`. The field is restricted to two digits.

The XSD itself permits numeric zero, but the semantic annotation says the first submission is 1. The application therefore fails closed on `1..99` and never allocates zero.

## Lifecycle model

A Reporte Diario version is scoped by:

- internal organization;
- issuer RUC;
- `FechaResumen`;
- `SecEnvio`.

`AllocateFiscalDailyReportVersionUseCase` accepts only a **complete** frozen document/annulment population. It does not expose a patch API.

The first version for one issuer/day is always:

- `SecEnvio = 1`;
- revision kind `Initial`;
- no previous version;
- fixed internal reason `initial`.

Every later version:

- must be `Correction` or `FxReliquidation`;
- uses exactly `previous SecEnvio + 1`;
- points to the immediately previous durable version;
- requires a caller-supplied auditable reason code;
- cannot be allocated once sequence 99 is reached.

## Idempotency and concurrency

Each allocation carries an organization-scoped `OperationId`. Replaying the same operation with identical immutable input returns the original version. Reusing the operation id with different input fails closed.

The EF repository acquires a transaction-scoped row lock on the organization's fiscal profile before reading the latest report version. This deliberately coarse lock serializes the very low-frequency daily-report sequence decision and prevents two concurrent first versions from both observing an empty history.

Database uniqueness is additionally enforced on:

- organization + RUC + summary date + sequence;
- organization + operation id.

## Correction prerequisite

A new sequence after the first requires a durable signed artifact for the immediately previous version. This prevents generating an arbitrary chain of unsigned corrections.

Transport is still not implemented. A later transport slice must strengthen the send-order rule by refusing to submit version N+1 unless the required submission/response evidence for version N exists.

## FX reliquidation

The existing source-fact model can mark a report as requiring future FX reliquidation. This lifecycle persists that marker per version.

A version explicitly classified as `FxReliquidation` is allowed only when:

1. the immediately previous version is marked as requiring FX reliquidation; and
2. the complete replacement snapshot no longer carries unresolved reliquidation evidence.

The lifecycle does not acquire BCU quotations and does not reinterpret the existing FX evidence rules.

## Audit and durability

Every new version persists:

- previous-version identity;
- operation id;
- revision kind and reason code;
- reconciliation fingerprint;
- pending-FX-reliquidation marker;
- creation timestamp;
- tamper-evident version fingerprint.

Allocation, audit evidence and outbox event are committed in one local transaction.

## Deliberate non-scope

This increment does **not** implement:

- `EFACRECEPCIONREPORTE` transport;
- DGI acknowledgement lifecycle;
- transport retry policy;
- Sobre v05 packaging;
- live BCU acquisition;
- automatic regulatory decision that a generic correction is required;
- Production enablement.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
''')

write('test/CrossCuttingTests/FiscalDailyReportSequenceLifecycleTests.cs', r'''using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportSequenceLifecycleTests
{
    [Fact]
    public async Task First_version_is_one_and_operation_replay_does_not_consume_sequence()
    {
        var versions = new InMemoryVersions();
        var sut = UseCase(versions, new AlwaysSignedArtifacts());
        var command = Command("op-initial", FiscalDailyReportRevisionKind.Initial);

        var first = await sut.ExecuteAsync(command);
        var replay = await sut.ExecuteAsync(command);

        Assert.Equal(1, first.Snapshot.Sequence);
        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.VersionId, replay.VersionId);
        Assert.Single(versions.Values);
    }

    [Fact]
    public async Task Correction_uses_previous_plus_one_and_requires_prior_signed_artifact()
    {
        var versions = new InMemoryVersions();
        var initial = await UseCase(versions, new AlwaysSignedArtifacts()).ExecuteAsync(
            Command("op-initial", FiscalDailyReportRevisionKind.Initial));

        var unsignedPrior = UseCase(versions, new NeverSignedArtifacts());
        var blocked = await Assert.ThrowsAsync<ApplicationProblemException>(() => unsignedPrior.ExecuteAsync(
            Command("op-correction-blocked", FiscalDailyReportRevisionKind.Correction, "detected-discrepancy")));
        Assert.Equal("fiscal.daily_report.version.prior_not_signed", blocked.Code);

        var correction = await UseCase(versions, new AlwaysSignedArtifacts()).ExecuteAsync(
            Command("op-correction", FiscalDailyReportRevisionKind.Correction, "detected-discrepancy"));
        Assert.Equal(2, correction.Snapshot.Sequence);
        Assert.Equal(initial.VersionId, correction.PreviousVersionId);
        Assert.Equal("detected-discrepancy", correction.ReasonCode);
    }

    [Fact]
    public async Task Initial_cannot_be_allocated_twice_and_sequences_cannot_skip()
    {
        var versions = new InMemoryVersions();
        var sut = UseCase(versions, new AlwaysSignedArtifacts());
        await sut.ExecuteAsync(Command("op-1", FiscalDailyReportRevisionKind.Initial));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            sut.ExecuteAsync(Command("op-2", FiscalDailyReportRevisionKind.Initial)));

        Assert.Equal("fiscal.daily_report.version.initial_already_exists", error.Code);
        Assert.Single(versions.Values);
    }

    [Fact]
    public async Task Reusing_operation_id_with_different_input_fails_closed()
    {
        var versions = new InMemoryVersions();
        var sut = UseCase(versions, new AlwaysSignedArtifacts());
        await sut.ExecuteAsync(Command("same-op", FiscalDailyReportRevisionKind.Initial));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => sut.ExecuteAsync(
            Command("same-op", FiscalDailyReportRevisionKind.Initial) with { SummaryDate = new DateOnly(2026, 9, 13) }));

        Assert.Equal("fiscal.daily_report.version.operation_replay_mismatch", error.Code);
    }

    [Fact]
    public async Task Fx_reliquidation_requires_pending_previous_marker_and_resolved_replacement()
    {
        var versions = new InMemoryVersions();
        var prior = SeedVersion(sequence: 1, requiresFxReliquidation: true);
        versions.Values.Add(prior);
        var sut = UseCase(versions, new AlwaysSignedArtifacts());

        var result = await sut.ExecuteAsync(Command(
            "fx-close",
            FiscalDailyReportRevisionKind.FxReliquidation,
            "future-date-fx-resolved"));

        Assert.Equal(2, result.Snapshot.Sequence);
        Assert.False(result.RequiresFxReliquidation);
        Assert.Equal(prior.Id, result.PreviousVersionId);
    }

    [Fact]
    public async Task Fx_reliquidation_without_pending_marker_fails_closed()
    {
        var versions = new InMemoryVersions();
        versions.Values.Add(SeedVersion(sequence: 1, requiresFxReliquidation: false));
        var sut = UseCase(versions, new AlwaysSignedArtifacts());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => sut.ExecuteAsync(Command(
            "fx-invalid",
            FiscalDailyReportRevisionKind.FxReliquidation,
            "future-date-fx-resolved")));

        Assert.Equal("fiscal.daily_report.version.fx_reliquidation_not_pending", error.Code);
    }

    private static AllocateFiscalDailyReportVersionCommand Command(
        string operationId,
        FiscalDailyReportRevisionKind kind,
        string? reason = null) =>
        new(
            "company-1",
            "214748364700",
            new DateOnly(2026, 9, 12),
            operationId,
            kind,
            reason,
            Array.Empty<FiscalDailyReportDocumentEvidence>(),
            Array.Empty<FiscalDailyReportAnnulmentEvidence>());

    private static AllocateFiscalDailyReportVersionUseCase UseCase(
        IFiscalDailyReportVersionRepository versions,
        IFiscalDailyReportSignedArtifactRepository artifacts) =>
        new(
            versions,
            artifacts,
            new InlineTransactionManager(),
            new UnitOfWork(),
            new Audit(),
            new Outbox(),
            new Actors(),
            new Correlations());

    private static StoredFiscalDailyReportVersion SeedVersion(int sequence, bool requiresFxReliquidation)
    {
        var snapshot = FiscalDailyReportSnapshot.Create(
            "company-1", "214748364700", new DateOnly(2026, 9, 12), sequence);
        var provisional = new StoredFiscalDailyReportVersion(
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            new DateOnly(2026, 9, 12),
            sequence,
            sequence == 1 ? null : Guid.NewGuid(),
            $"seed-{sequence}",
            sequence == 1 ? FiscalDailyReportRevisionKind.Initial : FiscalDailyReportRevisionKind.Correction,
            sequence == 1 ? "initial" : "seed",
            snapshot.ReconciliationFingerprint,
            requiresFxReliquidation,
            new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero),
            new string('0', 64));
        return provisional with { VersionFingerprint = provisional.ComputeFingerprint() };
    }

    private sealed class InMemoryVersions : IFiscalDailyReportVersionRepository
    {
        public List<StoredFiscalDailyReportVersion> Values { get; } = [];
        public Task<bool> AcquireOrganizationSequenceLockAsync(string organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<StoredFiscalDailyReportVersion?> GetByOperationIdAsync(string organizationId, string operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x => x.OrganizationId == organizationId && x.OperationId == operationId));
        public Task<StoredFiscalDailyReportVersion?> GetLatestAsync(string organizationId, string issuerRuc, DateOnly summaryDate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.Where(x => x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc && x.SummaryDate == summaryDate).OrderByDescending(x => x.Sequence).FirstOrDefault());
        public Task<StoredFiscalDailyReportVersion?> GetByIdentityAsync(string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x => x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc && x.SummaryDate == summaryDate && x.Sequence == sequence));
        public Task AddAsync(StoredFiscalDailyReportVersion version, CancellationToken cancellationToken = default)
        {
            Values.Add(version);
            return Task.CompletedTask;
        }
    }

    private sealed class AlwaysSignedArtifacts : IFiscalDailyReportSignedArtifactRepository
    {
        public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalDailyReportSignedArtifact?>(new(
                Guid.NewGuid(), Guid.NewGuid(), organizationId, issuerRuc, summaryDate, sequence,
                "13.2", new string('a', 64), new string('b', 64), new string('c', 64),
                DateTimeOffset.UtcNow, "profile", "thumb", "serial", "schema", "13.2", "1.44.2", new string('d', 64), "<signed/>"));
        public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NeverSignedArtifacts : IFiscalDailyReportSignedArtifactRepository
    {
        public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, CancellationToken cancellationToken = default) => Task.FromResult<StoredFiscalDailyReportSignedArtifact?>(null);
        public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InlineTransactionManager : ITransactionManager
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
    private sealed class UnitOfWork : IUnitOfWork { public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1); }
    private sealed class Audit : IAuditWriter { public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class Outbox : IOutboxWriter { public Task EnqueueAsync<TEvent>(TEvent integrationEvent, OutboxContext context, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent => Task.CompletedTask; }
    private sealed class Actors : IActorContextAccessor { public ActorContext Current { get; } = new("actor", "Actor", true, new HashSet<string>(), new HashSet<string> { "company-1" }, new HashSet<string>(), new HashSet<string>(), "device"); }
    private sealed class Correlations : ICorrelationContextAccessor { public CorrelationContext Current { get; } = new("corr", "trace"); }
}
''')

write('test/ArchitectureTests/FiscalDailyReportSequenceLifecycleArchitectureTests.cs', r'''using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportSequenceLifecycleArchitectureTests
{
    [Fact]
    public void Pinned_DGI_schema_states_first_send_is_one_and_correction_is_previous_plus_one()
    {
        var schema = Read("src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2/ReporteDiarioCFE.xsd");
        Assert.Contains("El primer envío del día trae el", schema, StringComparison.Ordinal);
        Assert.Contains("secuencia anterior +1", schema, StringComparison.Ordinal);
        Assert.Contains("<xs:totalDigits value=\"2\"/>", schema, StringComparison.Ordinal);
    }

    [Fact]
    public void Lifecycle_is_complete_replacement_idempotent_and_transport_free()
    {
        var source = Read("src/Application/Fiscal/FiscalDailyReportVersionLifecycle.cs");
        var docs = Read("documentation/blueprint-api-implementation/49_FISCAL_DAILY_REPORT_SEQUENCE_LIFECYCLE.md");
        Assert.Contains("CompleteDocuments", source, StringComparison.Ordinal);
        Assert.Contains("CompleteAnnulments", source, StringComparison.Ordinal);
        Assert.Contains("OperationId", source, StringComparison.Ordinal);
        Assert.Contains("previous SecEnvio + 1", docs, StringComparison.Ordinal);
        Assert.Contains("EFACRECEPCIONREPORTE", docs, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_has_linear_identity_and_operation_uniqueness()
    {
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260912043000_V1FiscalDailyReportSequenceLifecycle.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportRepositories.cs");
        Assert.Contains("UX_v1_fdr_version_identity", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_version_operation", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fdr_version_previous", migration, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", repository, StringComparison.Ordinal);
        Assert.Contains("CurrentTransaction", repository, StringComparison.Ordinal);
    }

    private static string Read(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return File.ReadAllText(Path.GetFullPath(full));
    }
}
''')

write('test/PersistenceIntegrationTests/FiscalDailyReportSequenceLifecyclePersistenceTests.cs', r'''using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalDailyReportSequenceLifecyclePersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Version_repository_persists_sequence_lineage_and_operation_idempotency(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;
        await SeedCompanyAsync(database);

        FiscalDailyReportVersionAllocationResult first;
        await using (var context = database.CreateContext())
            first = await UseCase(context).ExecuteAsync(Command("op-1", FiscalDailyReportRevisionKind.Initial));

        Assert.Equal(1, first.Snapshot.Sequence);

        FiscalDailyReportVersionAllocationResult replay;
        await using (var context = database.CreateContext())
            replay = await UseCase(context).ExecuteAsync(Command("op-1", FiscalDailyReportRevisionKind.Initial));

        Assert.True(replay.Replayed);
        Assert.Equal(first.VersionId, replay.VersionId);

        FiscalDailyReportVersionAllocationResult correction;
        await using (var context = database.CreateContext())
            correction = await UseCase(context).ExecuteAsync(Command("op-2", FiscalDailyReportRevisionKind.Correction, "detected-discrepancy"));

        Assert.Equal(2, correction.Snapshot.Sequence);
        Assert.Equal(first.VersionId, correction.PreviousVersionId);

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalDailyReportVersionRecord>().AsNoTracking().OrderBy(x => x.Sequence).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { 1, 2 }, rows.Select(x => x.Sequence).ToArray());
        Assert.Equal(rows[0].Id, rows[1].PreviousVersionId);
        Assert.Equal(2, rows.Select(x => x.OperationId).Distinct().Count());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_initial_allocations_cannot_create_two_sequence_one_versions(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;
        await SeedCompanyAsync(database);

        async Task<(bool Success, string? Code)> Run(string operation)
        {
            try
            {
                await using var context = database.CreateContext();
                _ = await UseCase(context).ExecuteAsync(Command(operation, FiscalDailyReportRevisionKind.Initial));
                return (true, null);
            }
            catch (EFactura.Application.Common.Errors.ApplicationProblemException ex)
            {
                return (false, ex.Code);
            }
        }

        var results = await Task.WhenAll(Run("concurrent-1"), Run("concurrent-2"));
        Assert.Single(results.Where(x => x.Success));
        Assert.Single(results.Where(x => !x.Success));
        Assert.Equal("fiscal.daily_report.version.initial_already_exists", results.Single(x => !x.Success).Code);

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalDailyReportVersionRecord>().AsNoTracking().ToListAsync();
        Assert.Single(rows);
        Assert.Equal(1, rows.Single().Sequence);
    }

    private static AllocateFiscalDailyReportVersionUseCase UseCase(V1PersistenceDbContext context) =>
        new(
            new EfFiscalDailyReportVersionRepository(context),
            new AlwaysSignedArtifacts(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context),
            new NoOpAudit(),
            new NoOpOutbox(),
            new Actors(),
            new Correlations());

    private static AllocateFiscalDailyReportVersionCommand Command(string operation, FiscalDailyReportRevisionKind kind, string? reason = null) =>
        new("company-1", "214748364700", new DateOnly(2026, 9, 12), operation, kind, reason);

    private static async Task SeedCompanyAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        if (await context.Set<V1CompanyFiscalProfileRecord>().AnyAsync()) return;
        var now = DateTimeOffset.UtcNow;
        context.Set<V1CompanyFiscalProfileRecord>().Add(new V1CompanyFiscalProfileRecord
        {
            OrganizationId = "company-1",
            Ruc = "214748364700",
            LegalName = "Sequence Test Company",
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await context.SaveChangesAsync();
    }

    private sealed class AlwaysSignedArtifacts : IFiscalDailyReportSignedArtifactRepository
    {
        public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalDailyReportSignedArtifact?>(new(
                Guid.NewGuid(), Guid.NewGuid(), organizationId, issuerRuc, summaryDate, sequence,
                "13.2", new string('a', 64), new string('b', 64), new string('c', 64), DateTimeOffset.UtcNow,
                "profile", "thumb", "serial", "schema", "13.2", "1.44.2", new string('d', 64), "<signed/>"));
        public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class NoOpAudit : IAuditWriter { public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class NoOpOutbox : IOutboxWriter { public Task EnqueueAsync<TEvent>(TEvent integrationEvent, OutboxContext context, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent => Task.CompletedTask; }
    private sealed class Actors : IActorContextAccessor { public ActorContext Current { get; } = new("actor", "Actor", true, new HashSet<string>(), new HashSet<string> { "company-1" }, new HashSet<string>(), new HashSet<string>(), "device"); }
    private sealed class Correlations : ICorrelationContextAccessor { public CorrelationContext Current { get; } = new("corr", "trace"); }
}
''')

print('Daily Report SecEnvio lifecycle patch applied.')
