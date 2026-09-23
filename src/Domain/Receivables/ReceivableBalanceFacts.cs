using EFactura.Domain.Common;

namespace EFactura.Domain.Receivables;

public enum ReceivableBalanceFactKind
{
    AdjustmentIncrease = 1,
    AdjustmentDecrease = 2,
    CollectionAllocation = 3,
    CollectionReversal = 4
}

public sealed class ReceivableBalanceFact
{
    private ReceivableBalanceFact(
        Guid id,
        string organizationId,
        Guid receivableId,
        ReceivableBalanceFactKind kind,
        decimal amount,
        DateOnly effectiveOn,
        Guid? reversalOfFactId,
        DateTimeOffset recordedAtUtc)
    {
        if (id == Guid.Empty)
            throw Rule("receivables.balance_fact_id_required", "Receivable balance fact id is required.");
        if (string.IsNullOrWhiteSpace(organizationId))
            throw Rule("receivables.organization_required", "Receivable organization is required.");
        if (receivableId == Guid.Empty)
            throw Rule("receivables.id_required", "Receivable id is required.");
        if (!Enum.IsDefined(kind))
            throw Rule("receivables.balance_fact_kind_invalid", "Receivable balance fact kind is invalid.");
        if (amount <= 0m)
            throw Rule("receivables.balance_fact_amount_invalid", "Receivable balance fact amount must be greater than zero.");

        var normalizedOrganizationId = organizationId.Trim();
        if (normalizedOrganizationId.Length > 200)
            throw Rule("receivables.organization_invalid", "Receivable organization cannot exceed 200 characters.");

        if (kind == ReceivableBalanceFactKind.CollectionReversal && !reversalOfFactId.HasValue)
            throw Rule("receivables.collection_reversal_reference_required", "Collection reversal must reference the allocation it reverses.");
        if (kind != ReceivableBalanceFactKind.CollectionReversal && reversalOfFactId.HasValue)
            throw Rule("receivables.balance_fact_reversal_reference_invalid", "Only collection reversals may reference another balance fact.");
        if (reversalOfFactId == id)
            throw Rule("receivables.collection_reversal_self_reference", "Collection reversal cannot reference itself.");

        Id = id;
        OrganizationId = normalizedOrganizationId;
        ReceivableId = receivableId;
        Kind = kind;
        Amount = amount;
        EffectiveOn = effectiveOn;
        ReversalOfFactId = reversalOfFactId;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid ReceivableId { get; }
    public ReceivableBalanceFactKind Kind { get; }
    public decimal Amount { get; }
    public DateOnly EffectiveOn { get; }
    public Guid? ReversalOfFactId { get; }
    public DateTimeOffset RecordedAtUtc { get; }

    public decimal SignedEffect => Kind switch
    {
        ReceivableBalanceFactKind.AdjustmentIncrease => Amount,
        ReceivableBalanceFactKind.AdjustmentDecrease => -Amount,
        ReceivableBalanceFactKind.CollectionAllocation => -Amount,
        ReceivableBalanceFactKind.CollectionReversal => Amount,
        _ => throw Rule("receivables.balance_fact_kind_invalid", "Receivable balance fact kind is invalid.")
    };

    public static ReceivableBalanceFact AdjustmentIncrease(
        Guid id,
        string organizationId,
        Guid receivableId,
        decimal amount,
        DateOnly effectiveOn,
        DateTimeOffset recordedAtUtc) =>
        new(id, organizationId, receivableId, ReceivableBalanceFactKind.AdjustmentIncrease,
            amount, effectiveOn, null, recordedAtUtc);

    public static ReceivableBalanceFact AdjustmentDecrease(
        Guid id,
        string organizationId,
        Guid receivableId,
        decimal amount,
        DateOnly effectiveOn,
        DateTimeOffset recordedAtUtc) =>
        new(id, organizationId, receivableId, ReceivableBalanceFactKind.AdjustmentDecrease,
            amount, effectiveOn, null, recordedAtUtc);

    public static ReceivableBalanceFact CollectionAllocation(
        Guid id,
        string organizationId,
        Guid receivableId,
        decimal amount,
        DateOnly effectiveOn,
        DateTimeOffset recordedAtUtc) =>
        new(id, organizationId, receivableId, ReceivableBalanceFactKind.CollectionAllocation,
            amount, effectiveOn, null, recordedAtUtc);

    public static ReceivableBalanceFact CollectionReversal(
        Guid id,
        string organizationId,
        Guid receivableId,
        Guid allocationFactId,
        decimal amount,
        DateOnly effectiveOn,
        DateTimeOffset recordedAtUtc) =>
        new(id, organizationId, receivableId, ReceivableBalanceFactKind.CollectionReversal,
            amount, effectiveOn, allocationFactId, recordedAtUtc);

    public static ReceivableBalanceFact Rehydrate(
        Guid id,
        string organizationId,
        Guid receivableId,
        ReceivableBalanceFactKind kind,
        decimal amount,
        DateOnly effectiveOn,
        Guid? reversalOfFactId,
        DateTimeOffset recordedAtUtc) =>
        new(id, organizationId, receivableId, kind, amount, effectiveOn, reversalOfFactId, recordedAtUtc);

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
