using System;
using System.Collections.Generic;
using NodaTime;

namespace ApplicationCore.Entites;

public partial class Payments
{
    public long Id { get; set; }

    public long? InvoiceId { get; set; }

    public LocalDateTime? PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public long? PaymentMethodId { get; set; }

    public long CreatedBy { get; set; }

    public LocalDateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public LocalDateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
