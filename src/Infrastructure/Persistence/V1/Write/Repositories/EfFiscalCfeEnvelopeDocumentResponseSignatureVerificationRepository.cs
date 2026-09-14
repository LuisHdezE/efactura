using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository :
    IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository(V1PersistenceDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification?> GetByConsultationIdAsync(
        Guid consultationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConsultationId == consultationId, cancellationToken);
        return Map(record);
    }

    public Task AddAsync(
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification verification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verification);
        _dbContext.Set<V1FiscalCfeDocumentResponseSignatureVerificationRecord>().Add(ToRecord(verification));
        return Task.CompletedTask;
    }

    private static V1FiscalCfeDocumentResponseSignatureVerificationRecord ToRecord(
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification value) => new()
    {
        Id = value.Id,
        ConsultationId = value.ConsultationId,
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

    private static StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification? Map(
        V1FiscalCfeDocumentResponseSignatureVerificationRecord? value) =>
        value is null ? null : new StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification(
            value.Id,
            value.ConsultationId,
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
