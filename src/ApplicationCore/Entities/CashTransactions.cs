using System;
using System.Collections.Generic;
using NodaTime;

namespace ApplicationCore.Entites;

public partial class CashTransactions
{
    public long Id { get; set; }

    public LocalDateTime? TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public string TransactionType { get; set; }

    public string Description { get; set; }

    public long? RelatedInvoiceId { get; set; }

    public long CreatedBy { get; set; }

    public LocalDateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public LocalDateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
