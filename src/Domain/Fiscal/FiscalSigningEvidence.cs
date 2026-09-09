using EFactura.Domain.Common;

namespace EFactura.Domain.Fiscal;

/// <summary>
/// Durable evidence for the signing act that will later produce TmstFirma and ds:Signature.
/// It binds one signing timestamp to one FiscalDocument, immutable fiscal-content fingerprint
/// and deterministic unsigned-content hash. Certificate/private-key concerns remain outside Domain.
/// </summary>
public sealed class FiscalSigningEvidence
{
    private FiscalSigningEvidence(
        Guid id,
        string organizationId,
        Guid fiscalDocumentId,
        string fiscalContentFingerprint,
        string unsignedContentHash,
        DateTimeOffset signingTimestamp)
    {
        if (id == Guid.Empty)
            throw Rule("fiscal.signing_evidence.id_required", "Signing evidence id is required.");
        if (fiscalDocumentId == Guid.Empty)
            throw Rule("fiscal.signing_evidence.document_id_required", "Fiscal document id is required.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "fiscal.signing_evidence.organization_required");
        FiscalDocumentId = fiscalDocumentId;
        FiscalContentFingerprint = Fingerprint(
            fiscalContentFingerprint,
            "fiscal.signing_evidence.snapshot_fingerprint_invalid");
        UnsignedContentHash = Fingerprint(
            unsignedContentHash,
            "fiscal.signing_evidence.unsigned_hash_invalid");
        SigningTimestamp = NormalizeToSecond(signingTimestamp);
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid FiscalDocumentId { get; }
    public string FiscalContentFingerprint { get; }
    public string UnsignedContentHash { get; }

    /// <summary>
    /// Signing-act timestamp that later renders as DGI TmstFirma. Once persisted, retries reuse it.
    /// </summary>
    public DateTimeOffset SigningTimestamp { get; }

    public static FiscalSigningEvidence Establish(
        Guid id,
        string organizationId,
        Guid fiscalDocumentId,
        string fiscalContentFingerprint,
        string unsignedContentHash,
        DateTimeOffset signingTimestamp) =>
        new(
            id,
            organizationId,
            fiscalDocumentId,
            fiscalContentFingerprint,
            unsignedContentHash,
            signingTimestamp);

    public static FiscalSigningEvidence Rehydrate(
        Guid id,
        string organizationId,
        Guid fiscalDocumentId,
        string fiscalContentFingerprint,
        string unsignedContentHash,
        DateTimeOffset signingTimestamp) =>
        new(
            id,
            organizationId,
            fiscalDocumentId,
            fiscalContentFingerprint,
            unsignedContentHash,
            signingTimestamp);

    private static DateTimeOffset NormalizeToSecond(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required signing-evidence value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Signing-evidence value cannot exceed {max} characters.");
        return normalized;
    }

    private static string Fingerprint(string value, string code)
    {
        var normalized = Required(value, 64, code).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Rule(code, "Signing evidence requires a SHA-256 hexadecimal value.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
