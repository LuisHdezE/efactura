using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeBatchPlanningTests
{
    [Fact]
    public async Task Planner_groups_by_certificate_in_first_seen_order_and_preserves_document_order()
    {
        var a1 = Artifact("91000000-0000-0000-0000-000000000001", "thumb-a", "serial-a");
        var b1 = Artifact("91000000-0000-0000-0000-000000000002", "thumb-b", "serial-b");
        var a2 = Artifact("91000000-0000-0000-0000-000000000003", "thumb-a", "serial-a");
        var b2 = Artifact("91000000-0000-0000-0000-000000000004", "thumb-b", "serial-b");
        var repository = new FakeSignedArtifactRepository(a1, b1, a2, b2);
        var useCase = new PlanFiscalCfeEnvelopeBatchesUseCase(repository);

        var result = await useCase.ExecuteAsync(new(
            "company-envelope-batch",
            [a1.FiscalDocumentId, b1.FiscalDocumentId, a2.FiscalDocumentId, b2.FiscalDocumentId]));

        Assert.Equal(4, result.TotalDocuments);
        Assert.Equal(2, result.Batches.Count);
        Assert.Equal(1, result.Batches[0].BatchOrdinal);
        Assert.Equal("thumb-a", result.Batches[0].CertificateThumbprint);
        Assert.Equal(new[] { a1.FiscalDocumentId, a2.FiscalDocumentId }, result.Batches[0].FiscalDocumentIds);
        Assert.Equal(2, result.Batches[1].BatchOrdinal);
        Assert.Equal("thumb-b", result.Batches[1].CertificateThumbprint);
        Assert.Equal(new[] { b1.FiscalDocumentId, b2.FiscalDocumentId }, result.Batches[1].FiscalDocumentIds);
        Assert.Equal(4, repository.ReadCount);
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task Planner_splits_same_certificate_into_250_document_chunks()
    {
        var artifacts = Enumerable.Range(1, 251)
            .Select(index => Artifact(GuidFrom(index), "thumb-a", "serial-a"))
            .ToArray();
        var useCase = new PlanFiscalCfeEnvelopeBatchesUseCase(new FakeSignedArtifactRepository(artifacts));

        var result = await useCase.ExecuteAsync(new(
            "company-envelope-batch",
            artifacts.Select(x => x.FiscalDocumentId).ToArray()));

        Assert.Equal(251, result.TotalDocuments);
        Assert.Equal(2, result.Batches.Count);
        Assert.Equal(250, result.Batches[0].CfeCount);
        Assert.Equal(1, result.Batches[1].CfeCount);
        Assert.Equal(artifacts.Take(250).Select(x => x.FiscalDocumentId), result.Batches[0].FiscalDocumentIds);
        Assert.Equal(artifacts.Skip(250).Select(x => x.FiscalDocumentId), result.Batches[1].FiscalDocumentIds);
    }

    [Fact]
    public async Task Planner_rejects_duplicate_document_before_repository_reads()
    {
        var id = Guid.Parse("92000000-0000-0000-0000-000000000001");
        var repository = new FakeSignedArtifactRepository();
        var useCase = new PlanFiscalCfeEnvelopeBatchesUseCase(repository);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new("company-envelope-batch", [id, id])));

        Assert.Equal("fiscal.envelope_batch.duplicate_document", error.Code);
        Assert.Equal(0, repository.ReadCount);
    }

    [Fact]
    public async Task Planner_requires_durable_signed_artifact_for_every_candidate()
    {
        var repository = new FakeSignedArtifactRepository();
        var useCase = new PlanFiscalCfeEnvelopeBatchesUseCase(repository);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new(
                "company-envelope-batch",
                [Guid.Parse("93000000-0000-0000-0000-000000000001")])));

        Assert.Equal("fiscal.envelope_batch.signed_artifact_required", error.Code);
        Assert.Equal(1, repository.ReadCount);
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task Planner_fails_closed_on_cross_organization_or_incomplete_certificate_evidence()
    {
        var artifact = Artifact("94000000-0000-0000-0000-000000000001", "thumb-a", "serial-a") with
        {
            OrganizationId = "another-company"
        };
        var useCase = new PlanFiscalCfeEnvelopeBatchesUseCase(new FakeSignedArtifactRepository(artifact));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new("company-envelope-batch", [artifact.FiscalDocumentId])));

        Assert.Equal("fiscal.envelope_batch.signed_artifact_invalid", error.Code);
    }

    private static StoredFiscalSignedArtifact Artifact(string id, string thumbprint, string serial) =>
        Artifact(Guid.Parse(id), thumbprint, serial);

    private static StoredFiscalSignedArtifact Artifact(Guid id, string thumbprint, string serial) => new(
        Guid.NewGuid(),
        "company-envelope-batch",
        id,
        Guid.NewGuid(),
        new string('a', 64),
        new string('b', 64),
        new string('c', 64),
        new string('d', 64),
        new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.FromHours(-3)),
        "test-profile",
        thumbprint,
        serial,
        "test-schema",
        "1.0",
        new string('e', 64),
        "<signed />");

    private static Guid GuidFrom(int value) =>
        Guid.Parse($"95000000-0000-0000-0000-{value:000000000000}");

    private sealed class FakeSignedArtifactRepository(params StoredFiscalSignedArtifact[] artifacts)
        : IFiscalSignedArtifactRepository
    {
        private readonly Dictionary<Guid, StoredFiscalSignedArtifact> _artifacts =
            artifacts.ToDictionary(x => x.FiscalDocumentId);

        public int ReadCount { get; private set; }
        public int AddCount { get; private set; }

        public Task<StoredFiscalSignedArtifact?> GetByFiscalDocumentAsync(
            string organizationId,
            Guid fiscalDocumentId,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            _artifacts.TryGetValue(fiscalDocumentId, out var artifact);
            return Task.FromResult(artifact);
        }

        public Task AddAsync(
            StoredFiscalSignedArtifact artifact,
            CancellationToken cancellationToken = default)
        {
            AddCount++;
            return Task.CompletedTask;
        }
    }
}
