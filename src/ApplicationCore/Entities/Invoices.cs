using System;
using System.Collections.Generic;
using NodaTime;

namespace ApplicationCore.Entites;

public partial class Invoices
{
    public long Id { get; set; }

    public long? OrderId { get; set; }

    public LocalDateTime? InvoiceDate { get; set; }

    public decimal AmountDue { get; set; }

    public decimal? AmountPaid { get; set; }

    public LocalDate? DueDate { get; set; }

    public long CreatedBy { get; set; }

    public LocalDateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public LocalDateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
