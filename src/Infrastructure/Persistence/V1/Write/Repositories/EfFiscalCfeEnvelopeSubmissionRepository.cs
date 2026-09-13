using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalCfeEnvelopeSubmissionRepository : IFiscalCfeEnvelopeSubmissionRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalCfeEnvelopeSubmissionRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<StoredFiscalCfeEnvelopeSubmission?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeEnvelopeSubmissionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.OperationId == operationId, cancellationToken);
        return Map(record);
    }

    public async Task<StoredFiscalCfeEnvelopeSubmission?> GetByEnvelopeIdAsync(
        Guid envelopeId,
        CancellationToken cancellationToken = default)
    {
        var records = _dbContext.Set<V1FiscalCfeEnvelopeSubmissionRecord>();
        V1FiscalCfeEnvelopeSubmissionRecord? record;
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            var provider = _dbContext.Database.ProviderName ?? string.Empty;
            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                record = await records
                    .FromSqlInterpolated($"SELECT * FROM \"v1_fiscal_cfe_envelope_submissions\" WHERE \"EnvelopeId\" = {envelopeId} FOR UPDATE")
                    .AsNoTracking()
                    .SingleOrDefaultAsync(cancellationToken);
            }
            else if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                record = await records
                    .FromSqlInterpolated($"SELECT * FROM `v1_fiscal_cfe_envelope_submissions` WHERE `EnvelopeId` = {envelopeId} FOR UPDATE")
                    .AsNoTracking()
                    .SingleOrDefaultAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported provider for Sobre submission lock: {provider}");
            }
        }
        else
        {
            record = await records.AsNoTracking().SingleOrDefaultAsync(x => x.EnvelopeId == envelopeId, cancellationToken);
        }
        return Map(record);
    }

    public Task AddAsync(StoredFiscalCfeEnvelopeSubmission submission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        _dbContext.Set<V1FiscalCfeEnvelopeSubmissionRecord>().Add(ToRecord(submission));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(StoredFiscalCfeEnvelopeSubmission submission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        var record = await _dbContext.Set<V1FiscalCfeEnvelopeSubmissionRecord>()
            .SingleOrDefaultAsync(x => x.Id == submission.Id, cancellationToken)
            ?? throw new InvalidOperationException("Fiscal CFE envelope submission record no longer exists.");

        if (record.EnvelopeId != submission.EnvelopeId
            || !string.Equals(record.OrganizationId, submission.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(record.IssuerRuc, submission.IssuerRuc, StringComparison.Ordinal)
            || !string.Equals(record.ReceiverRut, submission.ReceiverRut, StringComparison.Ordinal)
            || record.SenderEnvelopeId != submission.SenderEnvelopeId
            || !string.Equals(record.OperationId, submission.OperationId, StringComparison.Ordinal)
            || !string.Equals(record.EnvelopeSha256, submission.EnvelopeSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("Immutable Fiscal CFE envelope submission identity changed during update.");

        record.State = (int)submission.State;
        record.AttemptCount = submission.AttemptCount;
        record.LastAttemptAtUtc = submission.LastAttemptAtUtc;
        record.CompletedAtUtc = submission.CompletedAtUtc;
        record.ResponseXml = submission.ResponseXml;
        record.ResponseSha256 = submission.ResponseSha256;
        record.FailureCode = submission.FailureCode;
    }

    private static V1FiscalCfeEnvelopeSubmissionRecord ToRecord(StoredFiscalCfeEnvelopeSubmission value) => new()
    {
        Id = value.Id,
        EnvelopeId = value.EnvelopeId,
        OrganizationId = value.OrganizationId,
        IssuerRuc = value.IssuerRuc,
        ReceiverRut = value.ReceiverRut,
        SenderEnvelopeId = value.SenderEnvelopeId,
        OperationId = value.OperationId,
        EnvelopeSha256 = value.EnvelopeSha256,
        State = (int)value.State,
        AttemptCount = value.AttemptCount,
        PreparedAtUtc = value.PreparedAtUtc,
        LastAttemptAtUtc = value.LastAttemptAtUtc,
        CompletedAtUtc = value.CompletedAtUtc,
        ResponseXml = value.ResponseXml,
        ResponseSha256 = value.ResponseSha256,
        FailureCode = value.FailureCode
    };

    private static StoredFiscalCfeEnvelopeSubmission? Map(V1FiscalCfeEnvelopeSubmissionRecord? value) =>
        value is null ? null : new StoredFiscalCfeEnvelopeSubmission(
            value.Id, value.EnvelopeId, value.OrganizationId, value.IssuerRuc, value.ReceiverRut,
            value.SenderEnvelopeId, value.OperationId, value.EnvelopeSha256,
            (FiscalCfeEnvelopeSubmissionState)value.State, value.AttemptCount, value.PreparedAtUtc,
            value.LastAttemptAtUtc, value.CompletedAtUtc, value.ResponseXml, value.ResponseSha256, value.FailureCode);
}
