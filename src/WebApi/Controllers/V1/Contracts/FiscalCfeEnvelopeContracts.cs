namespace WebApi.Controllers.V1.Contracts;

public sealed record FiscalCfeEnvelopeDocumentResponseEvidenceDto(
    string EnvelopeId,
    string SubmissionId,
    string AckObservationId,
    string ConsultationId,
    DateTimeOffset ConsultedAtUtc,
    string ResponseSha256,
    int EnvelopeCfeCount,
    int RespondedCount,
    int AcceptedCount,
    int RejectedCount,
    int ObservedCount,
    int OtherRejectedCount,
    string CoverageStatus,
    int ConsultationObservationCount,
    int DistinctResponseMessageCount,
    int CoveredDocumentCount,
    int MissingDocumentCount,
    IReadOnlyList<FiscalCfeEnvelopeDocumentResponseCoveredDocumentDto> CoveredDocuments,
    IReadOnlyList<FiscalCfeEnvelopeDocumentResponseMissingDocumentDto> MissingDocuments,
    bool XmlSignatureValidated,
    bool PkiUruguayTrustValidated,
    bool ConsultationReplayed,
    bool SignatureVerificationReplayed,
    bool TrustValidationReplayed,
    bool FullyReplayed,
    bool DgiIdentityValidated,
    bool ProtocolFinalityProven,
    bool TokenExhaustionProven,
    bool AutomaticReconsultationAuthorized);

public sealed record FiscalCfeEnvelopeDocumentResponseCoveredDocumentDto(
    int CfeType,
    string Series,
    long Number,
    string StateCode,
    string State,
    int EvidenceMessageCount);

public sealed record FiscalCfeEnvelopeDocumentResponseMissingDocumentDto(
    int CfeType,
    string Series,
    long Number);
