using EFactura.Application.Fiscal;

namespace Infrastructure.Fiscal;

/// <summary>
/// Applies the already-governed PKI Uruguay trust policy to the certificate embedded in a
/// cryptographically verified ACKCFE. The underlying trust mechanics are intentionally shared with
/// ACKSobre validation, while ACKCFE evidence and lifecycle remain completely separate.
/// </summary>
public sealed class DgiFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator :
    IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator
{
    private const string AckPrefix = "fiscal.envelope.ack.trust.";
    private const string DocumentResponsePrefix = "fiscal.envelope.document_response.trust.";

    private readonly IFiscalCfeEnvelopeAckCertificateTrustValidator _sharedValidator;

    public DgiFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator(
        IFiscalCfeEnvelopeAckCertificateTrustValidator sharedValidator) =>
        _sharedValidator = sharedValidator ?? throw new ArgumentNullException(nameof(sharedValidator));

    public FiscalCfeEnvelopeDocumentResponseCertificateTrustEvidence Validate(
        string responseXml,
        string expectedCertificateSha256,
        DateTimeOffset validationTimeUtc)
    {
        var evidence = _sharedValidator.Validate(
            responseXml,
            expectedCertificateSha256,
            validationTimeUtc);

        return new(
            evidence.IsTrusted,
            evidence.ValidationProfileId,
            evidence.CertificateSha256,
            evidence.TrustedRootSha256,
            evidence.ChainCertificateSha256,
            evidence.ChainBuilt,
            evidence.RevocationChecked,
            evidence.RevocationMode,
            DgiIdentityValidated: false,
            FailureCode: MapFailureCode(evidence.FailureCode));
    }

    private static string? MapFailureCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return code;
        return code.StartsWith(AckPrefix, StringComparison.Ordinal)
            ? DocumentResponsePrefix + code[AckPrefix.Length..]
            : DocumentResponsePrefix + "shared_policy_failed";
    }
}
