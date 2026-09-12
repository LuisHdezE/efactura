using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalDailyReportResponseConsultationRepository :
    IFiscalDailyReportResponseConsultationRepository,
    IFiscalDailyReportConsultationTargetReader
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportResponseConsultationRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredFiscalDailyReportResponseConsultation?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDailyReportResponseConsultationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task AddAsync(
        StoredFiscalDailyReportResponseConsultation consultation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consultation);
        if (consultation.RootSubmissionId.HasValue == consultation.BrCorrectionRevisionId.HasValue)
            throw new InvalidOperationException("Consultation evidence must reference exactly one local Reporte Diario target.");

        await _dbContext.Set<V1FiscalDailyReportResponseConsultationRecord>().AddAsync(
            new V1FiscalDailyReportResponseConsultationRecord
            {
                Id = consultation.Id,
                RootSubmissionId = consultation.RootSubmissionId,
                BrCorrectionRevisionId = consultation.BrCorrectionRevisionId,
                OrganizationId = consultation.OrganizationId,
                IssuerRuc = consultation.IssuerRuc,
                SummaryDate = consultation.SummaryDate.ToDateTime(TimeOnly.MinValue),
                Sequence = consultation.Sequence,
                LocalRevision = consultation.LocalRevision,
                OperationId = consultation.OperationId,
                DgiReceiverId = consultation.DgiReceiverId,
                AckStateCode = consultation.AckStateCode,
                AckXml = consultation.AckXml,
                AckXmlHash = consultation.AckXmlHash,
                Consistency = (int)consultation.Consistency,
                ConsultedAtUtc = consultation.ConsultedAtUtc
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<FiscalDailyReportConsultationTarget>> FindByReceiverIdAsync(
        string organizationId,
        string dgiReceiverId,
        CancellationToken cancellationToken = default)
    {
        var result = new List<FiscalDailyReportConsultationTarget>(2);

        var root = await _dbContext.Set<V1FiscalDailyReportSubmissionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.DgiReceiverId == dgiReceiverId,
                cancellationToken);
        if (root is not null)
        {
            result.Add(new FiscalDailyReportConsultationTarget(
                FiscalDailyReportConsultationTargetKind.RootSubmission,
                root.Id,
                root.OrganizationId,
                root.IssuerRuc,
                DateOnly.FromDateTime(root.SummaryDate),
                root.Sequence,
                null,
                root.DgiReceiverId!,
                root.AckStateCode));
        }

        var revision = await _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.DgiReceiverId == dgiReceiverId,
                cancellationToken);
        if (revision is not null)
        {
            result.Add(new FiscalDailyReportConsultationTarget(
                FiscalDailyReportConsultationTargetKind.BrCorrectionRevision,
                revision.Id,
                revision.OrganizationId,
                revision.IssuerRuc,
                DateOnly.FromDateTime(revision.SummaryDate),
                revision.Sequence,
                revision.LocalRevision,
                revision.DgiReceiverId!,
                revision.AckStateCode));
        }

        return result;
    }

    private static StoredFiscalDailyReportResponseConsultation Map(
        V1FiscalDailyReportResponseConsultationRecord value) =>
        new(
            value.Id,
            value.RootSubmissionId,
            value.BrCorrectionRevisionId,
            value.OrganizationId,
            value.IssuerRuc,
            DateOnly.FromDateTime(value.SummaryDate),
            value.Sequence,
            value.LocalRevision,
            value.OperationId,
            value.DgiReceiverId,
            value.AckStateCode,
            value.AckXml,
            value.AckXmlHash,
            (FiscalDailyReportConsultationConsistency)value.Consistency,
            value.ConsultedAtUtc);
}
