using EFactura.Application.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeDocumentResponseCertificateTrustValidatorTests
{
    private const string CertificateSha256 = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private const string RootSha256 = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";

    [Fact]
    public void Trusted_shared_PKI_evidence_is_forwarded_without_claiming_DGI_identity()
    {
        var shared = new RecordingSharedValidator(new(
            IsTrusted: true,
            ValidationProfileId: "pki-uruguay-test-v1",
            CertificateSha256,
            RootSha256,
            new[] { CertificateSha256, RootSha256 },
            ChainBuilt: true,
            RevocationChecked: true,
            RevocationMode: "Online",
            DgiIdentityValidated: false,
            FailureCode: null));
        var validator = new DgiFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator(shared);
        var time = new DateTimeOffset(2026, 9, 14, 12, 30, 0, TimeSpan.Zero);

        var result = validator.Validate("<ACKCFE />", CertificateSha256, time);

        Assert.True(result.IsTrusted);
        Assert.Equal("pki-uruguay-test-v1", result.ValidationProfileId);
        Assert.Equal(CertificateSha256, result.CertificateSha256);
        Assert.Equal(RootSha256, result.TrustedRootSha256);
        Assert.Equal(new[] { CertificateSha256, RootSha256 }, result.ChainCertificateSha256);
        Assert.True(result.ChainBuilt);
        Assert.True(result.RevocationChecked);
        Assert.Equal("Online", result.RevocationMode);
        Assert.False(result.DgiIdentityValidated);
        Assert.Null(result.FailureCode);
        Assert.Equal("<ACKCFE />", shared.ResponseXml);
        Assert.Equal(CertificateSha256, shared.ExpectedCertificateSha256);
        Assert.Equal(time, shared.ValidationTimeUtc);
    }

    [Fact]
    public void Shared_ACK_failure_code_is_mapped_to_ACKCFE_boundary()
    {
        var shared = new RecordingSharedValidator(new(
            IsTrusted: false,
            ValidationProfileId: "pki-uruguay-test-v1",
            CertificateSha256,
            TrustedRootSha256: string.Empty,
            ChainCertificateSha256: Array.Empty<string>(),
            ChainBuilt: false,
            RevocationChecked: false,
            RevocationMode: "Online",
            DgiIdentityValidated: false,
            FailureCode: "fiscal.envelope.ack.trust.revocation_unavailable"));
        var validator = new DgiFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator(shared);

        var result = validator.Validate(
            "<ACKCFE />",
            CertificateSha256,
            new DateTimeOffset(2026, 9, 14, 12, 30, 0, TimeSpan.Zero));

        Assert.False(result.IsTrusted);
        Assert.False(result.DgiIdentityValidated);
        Assert.Equal(
            "fiscal.envelope.document_response.trust.revocation_unavailable",
            result.FailureCode);
    }

    private sealed class RecordingSharedValidator : IFiscalCfeEnvelopeAckCertificateTrustValidator
    {
        private readonly FiscalCfeEnvelopeAckCertificateTrustEvidence _evidence;

        public RecordingSharedValidator(FiscalCfeEnvelopeAckCertificateTrustEvidence evidence) =>
            _evidence = evidence;

        public string? ResponseXml { get; private set; }
        public string? ExpectedCertificateSha256 { get; private set; }
        public DateTimeOffset ValidationTimeUtc { get; private set; }

        public FiscalCfeEnvelopeAckCertificateTrustEvidence Validate(
            string responseXml,
            string expectedCertificateSha256,
            DateTimeOffset validationTimeUtc)
        {
            ResponseXml = responseXml;
            ExpectedCertificateSha256 = expectedCertificateSha256;
            ValidationTimeUtc = validationTimeUtc;
            return _evidence;
        }
    }
}
