using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1PaymentMethodRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class V1PaymentRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public int Sequence { get; set; }
    public Guid PaymentMethodId { get; set; }
    public long PaymentMethodVersion { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string ConfirmationFingerprint { get; set; } = string.Empty;
    public string SettlementFingerprint { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class V1ReceivableRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid CustomerPartyId { get; set; }
    public Guid SaleId { get; set; }
    public decimal OriginalAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string ConfirmationFingerprint { get; set; } = string.Empty;
    public string SettlementFingerprint { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public ICollection<V1ReceivableBalanceEffectRecord> BalanceEffects { get; set; } = new List<V1ReceivableBalanceEffectRecord>();
}

[Table("v1_receivable_balance_effects")]
[Index(nameof(OrganizationId), nameof(ReceivableId), nameof(OccurredAtUtc), Name = "IX_v1_ar_effect_org_receivable_time")]
[Index(nameof(OrganizationId), nameof(Kind), nameof(SourceId), nameof(SourceSequence), IsUnique = true, Name = "UX_v1_ar_effect_source")]
[Index(nameof(OrganizationId), nameof(ReversesEffectId), IsUnique = true, Name = "UX_v1_ar_effect_reversal")]
public sealed class V1ReceivableBalanceEffectRecord
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string OrganizationId { get; set; } = string.Empty;

    public Guid ReceivableId { get; set; }
    public int Kind { get; set; }

    [Precision(18, 6)]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string SourceId { get; set; } = string.Empty;

    public int SourceSequence { get; set; }
    public Guid? ReversesEffectId { get; set; }

    [Precision(6)]
    public DateTimeOffset OccurredAtUtc { get; set; }

    [ForeignKey(nameof(ReceivableId))]
    public V1ReceivableRecord Receivable { get; set; } = null!;

    [ForeignKey(nameof(ReversesEffectId))]
    public V1ReceivableBalanceEffectRecord? ReversedEffect { get; set; }
}
