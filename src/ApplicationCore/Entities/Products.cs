using System;
using System.Collections.Generic;
using NodaTime;

namespace ApplicationCore.Entites;

public partial class Products
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public long? ProductCategoryId { get; set; }

    public LocalDateTime? CreatedAt { get; set; }

    public long CreatedBy { get; set; }

    public LocalDateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public LocalDateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
