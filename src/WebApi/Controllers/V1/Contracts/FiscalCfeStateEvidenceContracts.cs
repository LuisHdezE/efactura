namespace WebApi.Controllers.V1.Contracts;

public sealed record FiscalCfeStateEvidenceDto(
    string ConsultationId,
    string FiscalDocumentId,
    int CfeType,
    string Series,
    long Number,
    string ExternalStateCode,
    DateTimeOffset ConsultedAtUtc,
    string ResponseSha256,
    bool Replayed,
    bool StateMeaningResolved,
    bool LocalLifecycleMutationAuthorized);
