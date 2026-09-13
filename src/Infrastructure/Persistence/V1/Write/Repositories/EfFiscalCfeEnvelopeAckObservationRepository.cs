using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalCfeEnvelopeAckObservationRepository : IFiscalCfeEnvelopeAckObservationRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalCfeEnvelopeAckObservationRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<StoredFiscalCfeEnvelopeAckObservation?> GetBySubmissionIdAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeEnvelopeAckObservationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SubmissionId == submissionId, cancellationToken);
        return Map(record);
    }

    public Task AddAsync(
        StoredFiscalCfeEnvelopeAckObservation observation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        _dbContext.Set<V1FiscalCfeEnvelopeAckObservationRecord>().Add(ToRecord(observation));
        return Task.CompletedTask;
    }

    private static V1FiscalCfeEnvelopeAckObservationRecord ToRecord(StoredFiscalCfeEnvelopeAckObservation value) => new()
    {
        Id = value.Id,
        SubmissionId = value.SubmissionId,
        EnvelopeId = value.EnvelopeId,
        OrganizationId = value.OrganizationId,
        IssuerRuc = value.IssuerRuc,
        ReceiverRut = value.ReceiverRut,
        SenderEnvelopeId = value.SenderEnvelopeId,
        ResponseSha256 = value.ResponseSha256,
        DgiResponseId = value.DgiResponseId,
        DgiReceiverId = value.DgiReceiverId,
        CfeCount = value.CfeCount,
        State = (int)value.State,
        ReceptionTimestampText = value.ReceptionTimestampText,
        SigningTimestampText = value.SigningTimestampText,
        ConsultationToken = value.ConsultationToken,
        ConsultationAvailableAtText = value.ConsultationAvailableAtText,
        RejectionReasonsJson = value.RejectionReasonsJson,
        ObservedAtUtc = value.ObservedAtUtc
    };

    private static StoredFiscalCfeEnvelopeAckObservation? Map(V1FiscalCfeEnvelopeAckObservationRecord? value) =>
        value is null ? null : new StoredFiscalCfeEnvelopeAckObservation(
            value.Id,
            value.SubmissionId,
            value.EnvelopeId,
            value.OrganizationId,
            value.IssuerRuc,
            value.ReceiverRut,
            value.SenderEnvelopeId,
            value.ResponseSha256,
            value.DgiResponseId,
            value.DgiReceiverId,
            value.CfeCount,
            (FiscalCfeEnvelopeAckState)value.State,
            value.ReceptionTimestampText,
            value.SigningTimestampText,
            value.ConsultationToken,
            value.ConsultationAvailableAtText,
            value.RejectionReasonsJson,
            value.ObservedAtUtc);
}
