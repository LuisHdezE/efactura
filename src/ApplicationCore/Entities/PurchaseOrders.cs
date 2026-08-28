using System;
using System.Collections.Generic;
using NodaTime;

namespace ApplicationCore.Entites;

public partial class PurchaseOrders
{
    public long Id { get; set; }

    public long? CustomerId { get; set; }

    public LocalDateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; }

    public long CreatedBy { get; set; }

    public LocalDateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public LocalDateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
