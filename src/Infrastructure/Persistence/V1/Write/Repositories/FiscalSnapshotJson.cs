using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;

namespace Infrastructure.Persistence.V1.Write.Repositories;

internal static class FiscalSnapshotJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string SerializeConfirmation(FiscalConfirmationEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        evidence.EnsureIntegrity();
        return JsonSerializer.Serialize(evidence, Options);
    }

    public static FiscalConfirmationEvidence DeserializeConfirmation(
        string json,
        string expectedFingerprint)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(expectedFingerprint))
            throw PersistedInvalid("Persisted fiscal confirmation evidence is incomplete.");

        try
        {
            var evidence = JsonSerializer.Deserialize<FiscalConfirmationEvidence>(json, Options)
                ?? throw PersistedInvalid("Persisted fiscal confirmation evidence could not be materialized.");
            evidence.EnsureIntegrity();
            if (!string.Equals(
                    evidence.EvidenceFingerprint,
                    expectedFingerprint,
                    StringComparison.Ordinal))
            {
                throw PersistedInvalid("Persisted fiscal confirmation evidence fingerprint does not match its record metadata.");
            }

            return evidence;
        }
        catch (JsonException)
        {
            throw PersistedInvalid("Persisted fiscal confirmation evidence JSON is invalid.");
        }
        catch (DomainRuleException ex)
        {
            throw PersistedInvalid(ex.Message);
        }
    }

    public static string SerializeContent(FiscalContentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        snapshot.EnsureIntegrity();
        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static FiscalContentSnapshot DeserializeContent(
        string json,
        string expectedFingerprint)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(expectedFingerprint))
            throw PersistedInvalid("Persisted fiscal content snapshot is incomplete.");

        try
        {
            var snapshot = JsonSerializer.Deserialize<FiscalContentSnapshot>(json, Options)
                ?? throw PersistedInvalid("Persisted fiscal content snapshot could not be materialized.");
            snapshot.EnsureIntegrity();
            if (!string.Equals(snapshot.ContentFingerprint, expectedFingerprint, StringComparison.Ordinal))
                throw PersistedInvalid("Persisted fiscal content fingerprint does not match its record metadata.");

            return snapshot;
        }
        catch (JsonException)
        {
            throw PersistedInvalid("Persisted fiscal content snapshot JSON is invalid.");
        }
        catch (DomainRuleException ex)
        {
            throw PersistedInvalid(ex.Message);
        }
    }

    private static ApplicationProblemException PersistedInvalid(string message) =>
        new(
            ApplicationProblemKind.Conflict,
            "fiscal.snapshot.persisted_evidence_invalid",
            message,
            conflictType: "inconsistent_state");
}
