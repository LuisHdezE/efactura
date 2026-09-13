using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalCfeEnvelopePersistenceTests
{
    private const string OrganizationId = "company-envelope-persist";
    private const string ReceiverRut = "219999830019";
    private const string IssuerRuc = "214748364700";
    private const string CfeSchemaFingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string EnvelopeSchemaFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 13, 0, 50, 30, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_persists_and_replays_exact_Sobre_identity(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var firstId = Guid.Parse("91000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("91000000-0000-0000-0000-000000000002");
        var artifacts = new FakeSignedArtifactRepository(
            Artifact(firstId),
            Artifact(secondId));

        await using var context = database.CreateContext();
        var useCase = UseCase(context, artifacts);
        var command = new PersistFiscalCfeEnvelopeCommand(
            OrganizationId,
            ReceiverRut,
            IssuerRuc,
            3001,
            CreatedAt,
            [firstId, secondId],
            "sobre-persist-op-1");

        var first = await useCase.ExecuteAsync(command);
        var replay = await useCase.ExecuteAsync(command);
        var identityReplay = await useCase.ExecuteAsync(command with { OperationId = "sobre-persist-op-2" });

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.True(identityReplay.Replayed);
        Assert.Equal(first.EnvelopeId, replay.EnvelopeId);
        Assert.Equal(first.EnvelopeId, identityReplay.EnvelopeId);
        Assert.Equal("sobre-persist-op-1", identityReplay.OperationId);
        Assert.Equal(CreatedAt.Offset, identityReplay.CreatedAt.Offset);
        Assert.Equal(CreatedAt, identityReplay.CreatedAt);
        Assert.Equal(new[] { firstId, secondId }, identityReplay.FiscalDocumentIds);
        Assert.Equal(2, identityReplay.CfeCount);
        Assert.Matches("^[0-9a-f]{64}$", identityReplay.EnvelopeSha256);
        Assert.Equal(2, artifacts.ReadCount);

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalCfeEnvelopeRecord>().AsNoTracking().ToListAsync();
        Assert.Single(rows);
        var row = rows.Single();
        Assert.Equal(3001, row.SenderEnvelopeId);
        Assert.Equal(-180, row.CreatedAtOffsetMinutes);
        Assert.Equal(CreatedAt.UtcDateTime, row.CreatedAtUtc.UtcDateTime);
        Assert.Equal(2, row.CfeCount);
        Assert.Equal(first.EnvelopeXml, row.EnvelopeXml);
        Assert.Equal(first.EnvelopeSha256, row.EnvelopeSha256);

        var stored = await new EfFiscalCfeEnvelopeRepository(verify).GetByIdentityAsync(
            OrganizationId,
            IssuerRuc,
            ReceiverRut,
            3001);
        Assert.NotNull(stored);
        Assert.Equal(CreatedAt.Offset, stored!.CreatedAt.Offset);
        Assert.Equal(new[] { firstId, secondId }, stored.FiscalDocumentIds);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_rejects_identity_reuse_for_different_payload(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var firstId = Guid.Parse("92000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("92000000-0000-0000-0000-000000000002");
        var artifacts = new FakeSignedArtifactRepository(Artifact(firstId), Artifact(secondId));

        await using var context = database.CreateContext();
        var useCase = UseCase(context, artifacts);
        var initial = new PersistFiscalCfeEnvelopeCommand(
            OrganizationId,
            ReceiverRut,
            IssuerRuc,
            3002,
            CreatedAt,
            [firstId],
            "sobre-conflict-op-1");
        await useCase.ExecuteAsync(initial);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => useCase.ExecuteAsync(
            initial with
            {
                FiscalDocumentIds = [secondId],
                OperationId = "sobre-conflict-op-2"
            }));

        Assert.Equal("fiscal.envelope.persistence.identity_payload_conflict", error.Code);

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Set<V1FiscalCfeEnvelopeRecord>().CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_preserves_offset_and_normalizes_wire_timestamp_to_whole_second(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var documentId = Guid.Parse("93000000-0000-0000-0000-000000000001");
        var timestamp = new DateTimeOffset(2026, 9, 13, 0, 55, 30, 456, TimeSpan.FromHours(-3));
        await using var context = database.CreateContext();
        var useCase = UseCase(context, new FakeSignedArtifactRepository(Artifact(documentId)));

        var first = await useCase.ExecuteAsync(new(
            OrganizationId,
            ReceiverRut,
            IssuerRuc,
            3003,
            timestamp,
            [documentId],
            "sobre-offset-op-1"));

        Assert.Equal(0, first.CreatedAt.Millisecond);
        Assert.Equal(TimeSpan.FromHours(-3), first.CreatedAt.Offset);

        await using var verify = database.CreateContext();
        var stored = await new EfFiscalCfeEnvelopeRepository(verify).GetByOperationIdAsync(
            OrganizationId,
            "sobre-offset-op-1");
        Assert.NotNull(stored);
        Assert.Equal(TimeSpan.FromHours(-3), stored!.CreatedAt.Offset);
        Assert.Equal(0, stored.CreatedAt.Millisecond);
    }

    private static PersistFiscalCfeEnvelopeUseCase UseCase(
        Infrastructure.Persistence.V1.Write.V1PersistenceDbContext context,
        FakeSignedArtifactRepository artifacts)
    {
        var packager = new PackageFiscalCfeEnvelopeUseCase(
            artifacts,
            new FakeSignedCfeValidator(),
            new FakeEnvelopeBuilder(),
            new FakeEnvelopeValidator());
        return new PersistFiscalCfeEnvelopeUseCase(
            packager,
            new EfFiscalCfeEnvelopeRepository(context),
            new EfTransactionManager(context),
            new EfUnitOfWork(context));
    }

    private static StoredFiscalSignedArtifact Artifact(Guid documentId)
    {
        const string xml = "<signed />";
        return new StoredFiscalSignedArtifact(
            Guid.NewGuid(),
            OrganizationId,
            documentId,
            Guid.NewGuid(),
            new string('c', 64),
            new string('d', 64),
            new string('e', 64),
            Sha256(xml),
            CreatedAt.AddMinutes(-1),
            "test-profile",
            "thumb-envelope-persist",
            "serial-envelope-persist",
            "test-cfe-schema",
            "1.0",
            CfeSchemaFingerprint,
            xml);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FakeSignedArtifactRepository(params StoredFiscalSignedArtifact[] artifacts)
        : IFiscalSignedArtifactRepository
    {
        private readonly Dictionary<Guid, StoredFiscalSignedArtifact> _artifacts =
            artifacts.ToDictionary(x => x.FiscalDocumentId);

        public int ReadCount { get; private set; }

        public Task<StoredFiscalSignedArtifact?> GetByFiscalDocumentAsync(
            string organizationId,
            Guid fiscalDocumentId,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            _artifacts.TryGetValue(fiscalDocumentId, out var artifact);
            return Task.FromResult(artifact);
        }

        public Task AddAsync(StoredFiscalSignedArtifact artifact, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Persistence test artifact repository is read-only.");
    }

    private sealed class FakeSignedCfeValidator : IFiscalSignedCfeSchemaValidator
    {
        public FiscalSignedCfeSchemaValidationResult Validate(string signedXml) => new(
            FiscalSignedCfeSchemaValidationStatus.Valid,
            "test-cfe-schema",
            "1.0",
            CfeSchemaFingerprint,
            Array.Empty<FiscalSignedCfeSchemaValidationError>());
    }

    private sealed class FakeEnvelopeBuilder : IFiscalCfeEnvelopeBuilder
    {
        public FiscalCfeEnvelopeBuildArtifact Build(FiscalCfeEnvelopeBuildRequest request)
        {
            var ids = string.Join(",", request.Sources.Select(x => x.FiscalDocumentId));
            var xml = $"<EnvioCFE idemisor=\"{request.SenderEnvelopeId}\" fecha=\"{request.CreatedAt:O}\" ids=\"{ids}\" />";
            return new(xml, "thumb-envelope-persist", "serial-envelope-persist");
        }
    }

    private sealed class FakeEnvelopeValidator : IFiscalCfeEnvelopeSchemaValidator
    {
        public FiscalCfeEnvelopeSchemaValidationResult Validate(string envelopeXml) => new(
            FiscalCfeEnvelopeSchemaValidationStatus.Valid,
            "test-envelope-schema",
            "1.44.2",
            EnvelopeSchemaFingerprint,
            Array.Empty<FiscalCfeEnvelopeSchemaValidationError>());
    }
}
