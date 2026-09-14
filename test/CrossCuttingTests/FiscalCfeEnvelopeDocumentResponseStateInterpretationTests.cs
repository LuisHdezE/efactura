using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeDocumentResponseStateInterpretationTests
{
    [Fact]
    public async Task Trusted_ACKCFE_maps_published_AE_BE_CE_taxonomy_without_overclaiming_identity()
    {
        var source = Source(
            new[]
            {
                Detail(1, 101, "A", 123, "AE"),
                Detail(2, 101, "A", 124, "BE"),
                Detail(3, 101, "A", 125, "CE")
            },
            accepted: 1,
            rejected: 1,
            observed: 1);
        var useCase = UseCase(source);

        var result = await useCase.ExecuteAsync(Command());

        Assert.Equal(3, result.RespondedCount);
        Assert.Collection(
            result.Details,
            value => Assert.Equal(FiscalCfeEnvelopeDocumentResponseSemanticState.Received, value.State),
            value => Assert.Equal(FiscalCfeEnvelopeDocumentResponseSemanticState.Rejected, value.State),
            value => Assert.Equal(FiscalCfeEnvelopeDocumentResponseSemanticState.ObservedContingency, value.State));
        Assert.Equal(new[] { "AE", "BE", "CE" }, result.Details.Select(x => x.StateCode).ToArray());
        Assert.True(result.PkiUruguayTrustValidated);
        Assert.False(result.DgiIdentityValidated);
    }

    [Fact]
    public async Task Historical_or_unknown_state_code_fails_closed()
    {
        var source = Source(
            new[] { Detail(1, 101, "A", 123, "A") },
            accepted: 1,
            rejected: 0,
            observed: 0);
        var useCase = UseCase(source);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command()));

        Assert.Equal("fiscal.envelope.document_response.semantics.state_code_unsupported", error.Code);
    }

    [Fact]
    public async Task Caratula_counts_must_match_interpreted_detail_semantics()
    {
        var source = Source(
            new[]
            {
                Detail(1, 101, "A", 123, "AE"),
                Detail(2, 101, "A", 124, "BE")
            },
            accepted: 2,
            rejected: 0,
            observed: 0);
        var useCase = UseCase(source);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command()));

        Assert.Equal("fiscal.envelope.document_response.semantics.count_mismatch", error.Code);
    }

    [Fact]
    public async Task Missing_PKI_trust_evidence_blocks_semantic_interpretation()
    {
        var source = Source(
            new[] { Detail(1, 101, "A", 123, "AE") },
            accepted: 1,
            rejected: 0,
            observed: 0);
        var useCase = new InterpretFiscalCfeEnvelopeDocumentResponseStateUseCase(
            new ConsultationRepository(source.Consultation),
            new SignatureRepository(source.Signature),
            new TrustRepository(null));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command()));

        Assert.Equal("fiscal.envelope.document_response.semantics.trust_validation_required", error.Code);
    }

    [Fact]
    public async Task Trust_evidence_for_another_consultation_fails_closed()
    {
        var source = Source(
            new[] { Detail(1, 101, "A", 123, "AE") },
            accepted: 1,
            rejected: 0,
            observed: 0);
        var mismatchedTrust = source.Trust with { ConsultationId = Guid.NewGuid() };
        var useCase = new InterpretFiscalCfeEnvelopeDocumentResponseStateUseCase(
            new ConsultationRepository(source.Consultation),
            new SignatureRepository(source.Signature),
            new TrustRepository(mismatchedTrust));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command()));

        Assert.Equal("fiscal.envelope.document_response.semantics.source_mismatch", error.Code);
    }

    private static InterpretFiscalCfeEnvelopeDocumentResponseStateUseCase UseCase(SourceBundle source) =>
        new(
            new ConsultationRepository(source.Consultation),
            new SignatureRepository(source.Signature),
            new TrustRepository(source.Trust));

    private static InterpretFiscalCfeEnvelopeDocumentResponseStateCommand Command() =>
        new("company-ackcfe-semantics", "consultation-op", "trust-op");

    private static SourceBundle Source(
        IReadOnlyList<FiscalCfeEnvelopeDocumentResponseDetail> details,
        int accepted,
        int rejected,
        int observed)
    {
        var consultationId = Guid.NewGuid();
        var ackObservationId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        const string organizationId = "company-ackcfe-semantics";
        const string responseXml = "<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula /></ACKCFE>";
        var responseSha = Sha256(responseXml);
        var certificateSha = new string('c', 64);
        var trustedRootSha = new string('d', 64);

        var consultation = new StoredFiscalCfeEnvelopeDocumentResponseConsultation(
            consultationId,
            ackObservationId,
            submissionId,
            envelopeId,
            organizationId,
            "consultation-op",
            new string('a', 64),
            1516,
            new string('b', 64),
            1517,
            "214844360018",
            "219999820013",
            3009,
            Math.Max(details.Count, 1),
            details.Count,
            accepted,
            rejected,
            observed,
            0,
            JsonSerializer.Serialize(details),
            responseXml,
            responseSha,
            new DateTimeOffset(2026, 9, 14, 18, 0, 0, TimeSpan.Zero));

        var signature = new StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification(
            Guid.NewGuid(),
            consultationId,
            ackObservationId,
            submissionId,
            envelopeId,
            organizationId,
            responseSha,
            "ackcfe-xmldsig-v1",
            certificateSha,
            "thumbprint-ackcfe",
            "serial-ackcfe",
            "CN=ACKCFE signer",
            "CN=Issuer",
            "http://www.w3.org/2001/10/xml-exc-c14n#",
            "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
            "http://www.w3.org/2000/09/xmldsig#sha1",
            string.Empty,
            JsonSerializer.Serialize(new[] { "http://www.w3.org/2000/09/xmldsig#enveloped-signature" }),
            CertificateTrustValidated: false,
            new DateTimeOffset(2026, 9, 14, 18, 1, 0, TimeSpan.Zero));

        var trust = new StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation(
            Guid.NewGuid(),
            signature.Id,
            consultationId,
            ackObservationId,
            submissionId,
            envelopeId,
            organizationId,
            "trust-op",
            responseSha,
            "pki-uruguay-online-v1",
            certificateSha,
            trustedRootSha,
            JsonSerializer.Serialize(new[] { certificateSha, trustedRootSha }),
            "Online",
            PkiUruguayTrustValidated: true,
            DgiIdentityValidated: false,
            new DateTimeOffset(2026, 9, 14, 18, 2, 0, TimeSpan.Zero));

        return new(consultation, signature, trust);
    }

    private static FiscalCfeEnvelopeDocumentResponseDetail Detail(
        int ordinal,
        int cfeType,
        string series,
        long number,
        string stateCode) =>
        new(ordinal, cfeType, series, number, stateCode);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record SourceBundle(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation Consultation,
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification Signature,
        StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation Trust);

    private sealed class ConsultationRepository(StoredFiscalCfeEnvelopeDocumentResponseConsultation? value) :
        IFiscalCfeEnvelopeDocumentResponseConsultationRepository
    {
        public Task<StoredFiscalCfeEnvelopeDocumentResponseConsultation?> GetByOperationAsync(
            string organizationId,
            string operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(value is not null
                && value.OrganizationId == organizationId
                && value.OperationId == operationId
                    ? value
                    : null);

        public Task AddAsync(
            StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class SignatureRepository(StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification? value) :
        IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository
    {
        public Task<StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification?> GetByConsultationIdAsync(
            Guid consultationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(value is not null && value.ConsultationId == consultationId ? value : null);

        public Task AddAsync(
            StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification verification,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TrustRepository(StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation? value) :
        IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository
    {
        public Task<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation?> GetByOperationAsync(
            string organizationId,
            string operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(value is not null
                && value.OrganizationId == organizationId
                && value.OperationId == operationId
                    ? value
                    : null);

        public Task AddAsync(
            StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation validation,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
