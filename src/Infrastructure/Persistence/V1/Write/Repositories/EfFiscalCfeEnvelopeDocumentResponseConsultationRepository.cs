using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalCfeEnvelopeDocumentResponseConsultationRepository :
    IFiscalCfeEnvelopeDocumentResponseConsultationRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalCfeEnvelopeDocumentResponseConsultationRepository(V1PersistenceDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<StoredFiscalCfeEnvelopeDocumentResponseConsultation?> GetByOperationAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeDocumentResponseConsultationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return Map(record);
    }

    public Task AddAsync(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consultation);
        _dbContext.Set<V1FiscalCfeDocumentResponseConsultationRecord>().Add(ToRecord(consultation));
        return Task.CompletedTask;
    }

    private static V1FiscalCfeDocumentResponseConsultationRecord ToRecord(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation value) => new()
    {
        Id = value.Id,
        AckObservationId = value.AckObservationId,
        SubmissionId = value.SubmissionId,
        EnvelopeId = value.EnvelopeId,
        OrganizationId = value.OrganizationId,
        OperationId = value.OperationId,
        SourceAckResponseSha256 = value.SourceAckResponseSha256,
        DgiReceiverId = value.DgiReceiverId,
        ConsultationTokenSha256 = value.ConsultationTokenSha256,
        DgiResponseId = value.DgiResponseId,
        IssuerRuc = value.IssuerRuc,
        ReceiverRut = value.ReceiverRut,
        SenderEnvelopeId = value.SenderEnvelopeId,
        EnvelopeCfeCount = value.EnvelopeCfeCount,
        RespondedCount = value.RespondedCount,
        AcceptedCount = value.AcceptedCount,
        RejectedCount = value.RejectedCount,
        ObservedCount = value.ObservedCount,
        OtherRejectedCount = value.OtherRejectedCount,
        DetailsJson = value.DetailsJson,
        ResponseXml = value.ResponseXml,
        ResponseSha256 = value.ResponseSha256,
        ConsultedAtUtc = value.ConsultedAtUtc
    };

    private static StoredFiscalCfeEnvelopeDocumentResponseConsultation? Map(
        V1FiscalCfeDocumentResponseConsultationRecord? value) =>
        value is null ? null : new StoredFiscalCfeEnvelopeDocumentResponseConsultation(
            value.Id,
            value.AckObservationId,
            value.SubmissionId,
            value.EnvelopeId,
            value.OrganizationId,
            value.OperationId,
            value.SourceAckResponseSha256,
            value.DgiReceiverId,
            value.ConsultationTokenSha256,
            value.DgiResponseId,
            value.IssuerRuc,
            value.ReceiverRut,
            value.SenderEnvelopeId,
            value.EnvelopeCfeCount,
            value.RespondedCount,
            value.AcceptedCount,
            value.RejectedCount,
            value.ObservedCount,
            value.OtherRejectedCount,
            value.DetailsJson,
            value.ResponseXml,
            value.ResponseSha256,
            value.ConsultedAtUtc);
}
