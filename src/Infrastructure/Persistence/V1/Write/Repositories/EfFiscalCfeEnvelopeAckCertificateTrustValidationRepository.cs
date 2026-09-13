using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalCfeEnvelopeAckCertificateTrustValidationRepository
    : IFiscalCfeEnvelopeAckCertificateTrustValidationRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalCfeEnvelopeAckCertificateTrustValidationRepository(V1PersistenceDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<StoredFiscalCfeEnvelopeAckCertificateTrustValidation?> GetByOperationAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeEnvelopeAckCertificateTrustValidationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return Map(record);
    }

    public Task AddAsync(
        StoredFiscalCfeEnvelopeAckCertificateTrustValidation validation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validation);
        _dbContext.Set<V1FiscalCfeEnvelopeAckCertificateTrustValidationRecord>().Add(ToRecord(validation));
        return Task.CompletedTask;
    }

    private static V1FiscalCfeEnvelopeAckCertificateTrustValidationRecord ToRecord(
        StoredFiscalCfeEnvelopeAckCertificateTrustValidation value) => new()
    {
        Id = value.Id,
        SignatureVerificationId = value.SignatureVerificationId,
        AckObservationId = value.AckObservationId,
        SubmissionId = value.SubmissionId,
        EnvelopeId = value.EnvelopeId,
        OrganizationId = value.OrganizationId,
        OperationId = value.OperationId,
        ResponseSha256 = value.ResponseSha256,
        ValidationProfileId = value.ValidationProfileId,
        CertificateSha256 = value.CertificateSha256,
        TrustedRootSha256 = value.TrustedRootSha256,
        ChainCertificateSha256Json = value.ChainCertificateSha256Json,
        RevocationMode = value.RevocationMode,
        PkiUruguayTrustValidated = value.PkiUruguayTrustValidated,
        DgiIdentityValidated = value.DgiIdentityValidated,
        ValidatedAtUtc = value.ValidatedAtUtc
    };

    private static StoredFiscalCfeEnvelopeAckCertificateTrustValidation? Map(
        V1FiscalCfeEnvelopeAckCertificateTrustValidationRecord? value) =>
        value is null ? null : new StoredFiscalCfeEnvelopeAckCertificateTrustValidation(
            value.Id,
            value.SignatureVerificationId,
            value.AckObservationId,
            value.SubmissionId,
            value.EnvelopeId,
            value.OrganizationId,
            value.OperationId,
            value.ResponseSha256,
            value.ValidationProfileId,
            value.CertificateSha256,
            value.TrustedRootSha256,
            value.ChainCertificateSha256Json,
            value.RevocationMode,
            value.PkiUruguayTrustValidated,
            value.DgiIdentityValidated,
            value.ValidatedAtUtc);
}
