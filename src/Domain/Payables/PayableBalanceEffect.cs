using EFactura.Domain.Common;

namespace EFactura.Domain.Payables;

public enum PayableBalanceEffectKind
{
    AdjustmentIncrease = 1,
    AdjustmentDecrease = 2,
    SupplierPaymentAllocation = 3,
    SupplierPaymentReversal = 4
}

public sealed class PayableBalanceEffect
{
    private PayableBalanceEffect(
        Guid id,
        string organizationId,
        Guid payableId,
        PayableBalanceEffectKind kind,
        decimal amount,
        string sourceId,
        int sourceSequence,
        Guid? reversesEffectId,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
            throw Rule("payables.effect.id_required", "Payable balance effect id is required.");
        if (payableId == Guid.Empty)
            throw Rule("payables.effect.payable_required", "Payable balance effect requires a payable.");
        if (!Enum.IsDefined(kind))
            throw Rule("payables.effect.kind_invalid", "Payable balance effect kind is invalid.");
        if (amount <= 0m)
            throw Rule("payables.effect.amount_invalid", "Payable balance effect amount must be greater than zero.");
        if (sourceSequence < 0)
            throw Rule("payables.effect.source_sequence_invalid", "Payable balance effect source sequence cannot be negative.");
        if (kind == PayableBalanceEffectKind.SupplierPaymentReversal && !reversesEffectId.HasValue)
            throw Rule("payables.effect.reversal_target_required", "Supplier payment reversal must reference the allocation effect it reverses.");
        if (kind != PayableBalanceEffectKind.SupplierPaymentReversal && reversesEffectId.HasValue)
            throw Rule("payables.effect.reversal_target_invalid", "Only supplier payment reversal effects may reference a reversed effect.");
        if (reversesEffectId == id)
            throw Rule("payables.effect.self_reversal_invalid", "A payable balance effect cannot reverse itself.");

        Id = id;
        OrganizationId = Required(organizationId, 200, "payables.effect.organization_required");
        PayableId = payableId;
        Kind = kind;
        Amount = NormalizeAmount(amount);
        SourceId = Required(sourceId, 200, "payables.effect.source_required");
        SourceSequence = sourceSequence;
        ReversesEffectId = reversesEffectId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid PayableId { get; }
    public PayableBalanceEffectKind Kind { get; }
    public decimal Amount { get; }
    public string SourceId { get; }
    public int SourceSequence { get; }
    public Guid? ReversesEffectId { get; }
    public DateTimeOffset OccurredAtUtc { get; }

    public decimal SignedDelta => Kind switch
    {
        PayableBalanceEffectKind.AdjustmentIncrease => Amount,
        PayableBalanceEffectKind.AdjustmentDecrease => -Amount,
        PayableBalanceEffectKind.SupplierPaymentAllocation => -Amount,
        PayableBalanceEffectKind.SupplierPaymentReversal => Amount,
        _ => throw Rule("payables.effect.kind_invalid", "Payable balance effect kind is invalid.")
    };

    public static PayableBalanceEffect CreateAdjustment(
        Guid id,
        string organizationId,
        Guid payableId,
        decimal amount,
        bool increasesBalance,
        string adjustmentId,
        int sourceSequence,
        DateTimeOffset occurredAtUtc) =>
        new(
            id,
            organizationId,
            payableId,
            increasesBalance
                ? PayableBalanceEffectKind.AdjustmentIncrease
                : PayableBalanceEffectKind.AdjustmentDecrease,
            amount,
            adjustmentId,
            sourceSequence,
            null,
            occurredAtUtc);

    public static PayableBalanceEffect CreateSupplierPaymentAllocation(
        Guid id,
        string organizationId,
        Guid payableId,
        decimal amount,
        string supplierPaymentId,
        int allocationSequence,
        DateTimeOffset occurredAtUtc) =>
        new(
            id,
            organizationId,
            payableId,
            PayableBalanceEffectKind.SupplierPaymentAllocation,
            amount,
            supplierPaymentId,
            allocationSequence,
            null,
            occurredAtUtc);

    public static PayableBalanceEffect CreateSupplierPaymentReversal(
        Guid id,
        string organizationId,
        Guid payableId,
        decimal amount,
        string reversalId,
        int reversalSequence,
        Guid reversedAllocationEffectId,
        DateTimeOffset occurredAtUtc) =>
        new(
            id,
            organizationId,
            payableId,
            PayableBalanceEffectKind.SupplierPaymentReversal,
            amount,
            reversalId,
            reversalSequence,
            reversedAllocationEffectId,
            occurredAtUtc);

    public static PayableBalanceEffect Rehydrate(
        Guid id,
        string organizationId,
        Guid payableId,
        PayableBalanceEffectKind kind,
        decimal amount,
        string sourceId,
        int sourceSequence,
        Guid? reversesEffectId,
        DateTimeOffset occurredAtUtc) =>
        new(id, organizationId, payableId, kind, amount, sourceId, sourceSequence, reversesEffectId, occurredAtUtc);

    private static decimal NormalizeAmount(decimal value) =>
        decimal.Round(value, 6, MidpointRounding.ToEven);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required payable balance effect value is missing.");

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Payable balance effect value cannot exceed {max} characters.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
