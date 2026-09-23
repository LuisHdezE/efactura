using EFactura.Domain.Common;

namespace EFactura.Domain.Receivables;

public enum ReceivableBalanceEffectKind
{
    AdjustmentIncrease = 1,
    AdjustmentDecrease = 2,
    CollectionAllocation = 3,
    CollectionReversal = 4
}

public sealed class ReceivableBalanceEffect
{
    private ReceivableBalanceEffect(
        Guid id,
        string organizationId,
        Guid receivableId,
        ReceivableBalanceEffectKind kind,
        decimal amount,
        string sourceId,
        int sourceSequence,
        Guid? reversesEffectId,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
            throw Rule("receivables.effect.id_required", "Receivable balance effect id is required.");
        if (receivableId == Guid.Empty)
            throw Rule("receivables.effect.receivable_required", "Receivable balance effect requires a receivable.");
        if (!Enum.IsDefined(kind))
            throw Rule("receivables.effect.kind_invalid", "Receivable balance effect kind is invalid.");
        if (amount <= 0m)
            throw Rule("receivables.effect.amount_invalid", "Receivable balance effect amount must be greater than zero.");
        if (sourceSequence < 0)
            throw Rule("receivables.effect.source_sequence_invalid", "Receivable balance effect source sequence cannot be negative.");
        if (kind == ReceivableBalanceEffectKind.CollectionReversal && !reversesEffectId.HasValue)
            throw Rule("receivables.effect.reversal_target_required", "Collection reversal must reference the allocation effect it reverses.");
        if (kind != ReceivableBalanceEffectKind.CollectionReversal && reversesEffectId.HasValue)
            throw Rule("receivables.effect.reversal_target_invalid", "Only collection reversal effects may reference a reversed effect.");
        if (reversesEffectId == id)
            throw Rule("receivables.effect.self_reversal_invalid", "A receivable balance effect cannot reverse itself.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "receivables.effect.organization_required");
        ReceivableId = receivableId;
        Kind = kind;
        Amount = NormalizeAmount(amount);
        SourceId = Required(sourceId, 200, "receivables.effect.source_required");
        SourceSequence = sourceSequence;
        ReversesEffectId = reversesEffectId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid ReceivableId { get; }
    public ReceivableBalanceEffectKind Kind { get; }
    public decimal Amount { get; }
    public string SourceId { get; }
    public int SourceSequence { get; }
    public Guid? ReversesEffectId { get; }
    public DateTimeOffset OccurredAtUtc { get; }

    public decimal SignedDelta => Kind switch
    {
        ReceivableBalanceEffectKind.AdjustmentIncrease => Amount,
        ReceivableBalanceEffectKind.AdjustmentDecrease => -Amount,
        ReceivableBalanceEffectKind.CollectionAllocation => -Amount,
        ReceivableBalanceEffectKind.CollectionReversal => Amount,
        _ => throw Rule("receivables.effect.kind_invalid", "Receivable balance effect kind is invalid.")
    };

    public static ReceivableBalanceEffect CreateAdjustment(
        Guid id,
        string organizationId,
        Guid receivableId,
        decimal amount,
        bool increasesBalance,
        string adjustmentId,
        int sourceSequence,
        DateTimeOffset occurredAtUtc) =>
        new(
            id,
            organizationId,
            receivableId,
            increasesBalance
                ? ReceivableBalanceEffectKind.AdjustmentIncrease
                : ReceivableBalanceEffectKind.AdjustmentDecrease,
            amount,
            adjustmentId,
            sourceSequence,
            null,
            occurredAtUtc);

    public static ReceivableBalanceEffect CreateCollectionAllocation(
        Guid id,
        string organizationId,
        Guid receivableId,
        decimal amount,
        string collectionId,
        int allocationSequence,
        DateTimeOffset occurredAtUtc) =>
        new(
            id,
            organizationId,
            receivableId,
            ReceivableBalanceEffectKind.CollectionAllocation,
            amount,
            collectionId,
            allocationSequence,
            null,
            occurredAtUtc);

    public static ReceivableBalanceEffect CreateCollectionReversal(
        Guid id,
        string organizationId,
        Guid receivableId,
        decimal amount,
        string reversalId,
        int reversalSequence,
        Guid reversedAllocationEffectId,
        DateTimeOffset occurredAtUtc) =>
        new(
            id,
            organizationId,
            receivableId,
            ReceivableBalanceEffectKind.CollectionReversal,
            amount,
            reversalId,
            reversalSequence,
            reversedAllocationEffectId,
            occurredAtUtc);

    public static ReceivableBalanceEffect Rehydrate(
        Guid id,
        string organizationId,
        Guid receivableId,
        ReceivableBalanceEffectKind kind,
        decimal amount,
        string sourceId,
        int sourceSequence,
        Guid? reversesEffectId,
        DateTimeOffset occurredAtUtc) =>
        new(id, organizationId, receivableId, kind, amount, sourceId, sourceSequence, reversesEffectId, occurredAtUtc);

    private static decimal NormalizeAmount(decimal value) =>
        decimal.Round(value, 6, MidpointRounding.ToEven);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required receivable balance effect value is missing.");

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Receivable balance effect value cannot exceed {max} characters.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
