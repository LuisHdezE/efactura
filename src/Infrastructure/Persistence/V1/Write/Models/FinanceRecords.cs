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
    public List<V1ReceivableBalanceFactRecord> BalanceFacts { get; set; } = new();
}

[Table("v1_receivable_balance_facts")]
[Index(nameof(OrganizationId), nameof(ReceivableId), nameof(EffectiveOn), Name = "IX_v1_ar_fact_org_receivable_effective")]
[Index(nameof(ReversalOfFactId), IsUnique = true, Name = "UX_v1_ar_fact_reversal_of")]
public sealed class V1ReceivableBalanceFactRecord
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string OrganizationId { get; set; } = string.Empty;

    public Guid ReceivableId { get; set; }
    public int Kind { get; set; }

    [Precision(18, 6)]
    public decimal Amount { get; set; }

    [Column(TypeName = "date")]
    public DateTime EffectiveOn { get; set; }

    public Guid? ReversalOfFactId { get; set; }

    [Precision(6)]
    public DateTimeOffset RecordedAtUtc { get; set; }

    public V1ReceivableRecord Receivable { get; set; } = null!;
}
