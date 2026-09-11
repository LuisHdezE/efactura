namespace EFactura.Application.Fiscal;

public enum FiscalDailyReportUnsignedSchemaValidationStatus
{
    Valid = 1,
    DocumentInvalid = 2,
    SchemaSetInvalid = 3
}

public sealed record FiscalDailyReportUnsignedSchemaValidationError(
    string Code,
    string Message,
    int? LineNumber = null,
    int? LinePosition = null);

public sealed record FiscalDailyReportUnsignedSchemaValidationResult(
    FiscalDailyReportUnsignedSchemaValidationStatus Status,
    string SchemaSetId,
    string FunctionalFormatVersion,
    string SchemaArchiveVersion,
    string SchemaSetFingerprint,
    bool SignatureRequirementRelaxedForUnsignedValidation,
    IReadOnlyList<FiscalDailyReportUnsignedSchemaValidationError> Errors)
{
    public bool IsValid => Status == FiscalDailyReportUnsignedSchemaValidationStatus.Valid;
}

/// <summary>
/// Validates a pre-signature Reporte Diario against the byte-pinned DGI schema closure while
/// relaxing only the mandatory final ds:Signature cardinality in memory. Implementations must not
/// perform network I/O or claim that this is equivalent to validation of a signed report.
/// </summary>
public interface IFiscalDailyReportUnsignedSchemaValidator
{
    FiscalDailyReportUnsignedSchemaValidationResult Validate(string unsignedXml);
}
