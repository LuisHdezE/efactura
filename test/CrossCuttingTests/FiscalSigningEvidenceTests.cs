using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalSigningEvidenceTests
{
    private const string SnapshotFingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string UnsignedHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Establish_preserves_offset_and_normalizes_to_DGI_second_precision()
    {
        var evidence = FiscalSigningEvidence.Establish(
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            "company-1",
            Guid.Parse("40000000-0000-0000-0000-000000000002"),
            SnapshotFingerprint,
            UnsignedHash,
            new DateTimeOffset(2026, 9, 9, 12, 34, 56, 789, TimeSpan.FromHours(-3)));

        Assert.Equal(new DateTimeOffset(2026, 9, 9, 12, 34, 56, TimeSpan.FromHours(-3)), evidence.SigningTimestamp);
        Assert.Equal(SnapshotFingerprint, evidence.FiscalContentFingerprint);
        Assert.Equal(UnsignedHash, evidence.UnsignedContentHash);
    }

    [Fact]
    public void Rehydrate_preserves_the_original_signing_timestamp_for_replay()
    {
        var timestamp = new DateTimeOffset(2026, 9, 9, 12, 34, 56, TimeSpan.FromHours(-3));

        var evidence = FiscalSigningEvidence.Rehydrate(
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            "company-1",
            Guid.Parse("40000000-0000-0000-0000-000000000002"),
            SnapshotFingerprint,
            UnsignedHash,
            timestamp);

        Assert.Equal(timestamp, evidence.SigningTimestamp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void Establish_rejects_invalid_unsigned_content_hash(string hash)
    {
        var error = Assert.Throws<DomainRuleException>(() => FiscalSigningEvidence.Establish(
            Guid.NewGuid(),
            "company-1",
            Guid.NewGuid(),
            SnapshotFingerprint,
            hash,
            new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.FromHours(-3))));

        Assert.Equal("fiscal.signing_evidence.unsigned_hash_invalid", error.Code);
    }
}
