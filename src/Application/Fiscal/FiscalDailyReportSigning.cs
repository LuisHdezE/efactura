namespace EFactura.Application.Fiscal;

public sealed record FiscalDailyReportSignatureRequest(
    string OrganizationId,
    string FunctionalFormatVersion,
    string ProjectionFingerprint,
    string UnsignedXml,
    string UnsignedContentHash,
    DateTimeOffset SigningTimestamp);

public sealed record FiscalDailyReportSignatureResult(
    string SignedXml,
    string SignedContentHash,
    string SignatureProfileId,
    string CertificateThumbprint,
    string CertificateSerialNumber);

/// <summary>
/// Reporte Diario XMLDSig boundary. Infrastructure owns certificate/private-key access and may only
/// append the required final ds:Signature to the exact deterministic unsigned payload supplied here.
/// </summary>
public interface IFiscalDailyReportSignatureProvider
{
    Task<FiscalDailyReportSignatureResult> SignAsync(
        FiscalDailyReportSignatureRequest request,
        CancellationToken cancellationToken = default);
}

public enum FiscalDailyReportSignedSchemaValidationStatus
{
    Valid = 1,
    DocumentInvalid = 2,
    SchemaSetInvalid = 3
}

public sealed record FiscalDailyReportSignedSchemaValidationError(
    string Code,
    string Message,
    int? LineNumber = null,
    int? LinePosition = null);

public sealed record FiscalDailyReportSignedSchemaValidationResult(
    FiscalDailyReportSignedSchemaValidationStatus Status,
    string SchemaSetId,
    string FunctionalFormatVersion,
    string SchemaArchiveVersion,
    string SchemaSetFingerprint,
    IReadOnlyList<FiscalDailyReportSignedSchemaValidationError> Errors)
{
    public bool IsValid => Status == FiscalDailyReportSignedSchemaValidationStatus.Valid;
}

/// <summary>
/// Validates a complete signed Reporte Diario against the untouched byte-pinned DGI schema closure.
/// Implementations must not relax ds:Signature cardinality or resolve schemas from the network.
/// </summary>
public interface IFiscalDailyReportSignedSchemaValidator
{
    FiscalDailyReportSignedSchemaValidationResult Validate(string signedXml);
}
