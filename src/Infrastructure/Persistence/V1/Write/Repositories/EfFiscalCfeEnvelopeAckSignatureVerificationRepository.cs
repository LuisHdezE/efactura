using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalCfeEnvelopeAckSignatureVerificationRepository
    : IFiscalCfeEnvelopeAckSignatureVerificationRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalCfeEnvelopeAckSignatureVerificationRepository(V1PersistenceDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<StoredFiscalCfeEnvelopeAckSignatureVerification?> GetByAckObservationIdAsync(
        Guid ackObservationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.AckObservationId == ackObservationId, cancellationToken);
        return Map(record);
    }

    public Task AddAsync(
        StoredFiscalCfeEnvelopeAckSignatureVerification verification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verification);
        _dbContext.Set<V1FiscalCfeEnvelopeAckSignatureVerificationRecord>().Add(ToRecord(verification));
        return Task.CompletedTask;
    }

    private static V1FiscalCfeEnvelopeAckSignatureVerificationRecord ToRecord(
        StoredFiscalCfeEnvelopeAckSignatureVerification value) => new()
    {
        Id = value.Id,
        AckObservationId = value.AckObservationId,
        SubmissionId = value.SubmissionId,
        EnvelopeId = value.EnvelopeId,
        OrganizationId = value.OrganizationId,
        ResponseSha256 = value.ResponseSha256,
        VerificationProfileId = value.VerificationProfileId,
        CertificateSha256 = value.CertificateSha256,
        CertificateThumbprint = value.CertificateThumbprint,
        CertificateSerialNumber = value.CertificateSerialNumber,
        CertificateSubject = value.CertificateSubject,
        CertificateIssuer = value.CertificateIssuer,
        CanonicalizationMethod = value.CanonicalizationMethod,
        SignatureMethod = value.SignatureMethod,
        DigestMethod = value.DigestMethod,
        ReferenceUri = value.ReferenceUri,
        ReferenceTransformsJson = value.ReferenceTransformsJson,
        CertificateTrustValidated = value.CertificateTrustValidated,
        VerifiedAtUtc = value.VerifiedAtUtc
    };

    private static StoredFiscalCfeEnvelopeAckSignatureVerification? Map(
        V1FiscalCfeEnvelopeAckSignatureVerificationRecord? value) =>
        value is null ? null : new StoredFiscalCfeEnvelopeAckSignatureVerification(
            value.Id,
            value.AckObservationId,
            value.SubmissionId,
            value.EnvelopeId,
            value.OrganizationId,
            value.ResponseSha256,
            value.VerificationProfileId,
            value.CertificateSha256,
            value.CertificateThumbprint,
            value.CertificateSerialNumber,
            value.CertificateSubject,
            value.CertificateIssuer,
            value.CanonicalizationMethod,
            value.SignatureMethod,
            value.DigestMethod,
            value.ReferenceUri,
            value.ReferenceTransformsJson,
            value.CertificateTrustValidated,
            value.VerifiedAtUtc);
}
