using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDocumentIdentityTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Fiscal_document_identity_preserves_CAE_and_confirmation_snapshot()
    {
        var document = CreateDocument();

        Assert.Equal(CfeFamily.EFactura, document.CfeType);
        Assert.Equal("A", document.Series);
        Assert.Equal(17, document.Number);
        Assert.Equal("CAE-2026-001", document.CaeAuthorizationNumber);
        Assert.Equal(1, document.CaeRangeFrom);
        Assert.Equal(500, document.CaeRangeTo);
        Assert.Equal(new DateOnly(2026, 9, 8), document.FiscalDate);
        Assert.Equal("25.2", document.FormatVersion);
        Assert.Equal(Confirmation, document.ConfirmationFingerprint);
        Assert.Equal(Settlement, document.SettlementFingerprint);
        Assert.Equal(FiscalDocumentStatus.IdentityCreated, document.Status);
    }

    [Fact]
    public void Fiscal_document_rejects_number_outside_CAE_snapshot()
    {
        var error = Assert.Throws<DomainRuleException>(() =>
            CreateDocument(number: 501));

        Assert.Equal("fiscal.cae_range_invalid", error.Code);
    }

    [Fact]
    public void Fiscalization_request_advances_once_to_same_document_identity()
    {
        var request = FiscalizationRequest.CreateFromSale(
            Guid.NewGuid(),
            "company-1",
            Guid.NewGuid(),
            "loc-1",
            "term-1",
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            Settlement,
            "UYU",
            100m,
            22m,
            122m,
            DateTimeOffset.UtcNow);
        var documentId = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;

        request.MarkIdentityCreated(documentId, at, 1);
        request.MarkIdentityCreated(documentId, at.AddMinutes(1), 2);

        Assert.Equal(FiscalizationRequestStatus.IdentityCreated, request.Status);
        Assert.Equal(2, request.Version);
        Assert.Equal(documentId, request.FiscalDocumentId);

        var error = Assert.Throws<DomainRuleException>(() =>
            request.MarkIdentityCreated(Guid.NewGuid(), at, request.Version));
        Assert.Equal("fiscalization.identity_already_created", error.Code);
    }

    private static FiscalDocument CreateDocument(long number = 17) =>
        FiscalDocument.CreateIdentity(
            Guid.NewGuid(),
            "company-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CfeFamily.EFactura,
            "A",
            number,
            "CAE-2026-001",
            1,
            500,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            new DateOnly(2026, 9, 8),
            "loc-1",
            "term-1",
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            Settlement,
            "UYU",
            100m,
            22m,
            122m,
            DateTimeOffset.UtcNow);
}
