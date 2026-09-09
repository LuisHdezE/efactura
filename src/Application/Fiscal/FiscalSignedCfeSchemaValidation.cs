namespace EFactura.Application.Fiscal;

public enum FiscalSignedCfeSchemaValidationStatus
{
    Valid = 1,
    DocumentInvalid = 2,
    SchemaSetInvalid = 3
}

public sealed record FiscalSignedCfeSchemaValidationError(
    string Code,
    string Message,
    int? LineNumber = null,
    int? LinePosition = null);

public sealed record FiscalSignedCfeSchemaValidationResult(
    FiscalSignedCfeSchemaValidationStatus Status,
    string SchemaSetId,
    string SchemaVersion,
    string SchemaSetFingerprint,
    IReadOnlyList<FiscalSignedCfeSchemaValidationError> Errors)
{
    public bool IsValid => Status == FiscalSignedCfeSchemaValidationStatus.Valid;
}

/// <summary>
/// Validates a signed CFE against a pinned, locally available DGI schema set.
/// Implementations must not use network I/O while validating fiscal artifacts.
/// </summary>
public interface IFiscalSignedCfeSchemaValidator
{
    FiscalSignedCfeSchemaValidationResult Validate(string signedXml);
}
