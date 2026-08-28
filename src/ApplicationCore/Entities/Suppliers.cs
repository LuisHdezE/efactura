using NodaTime;

namespace ApplicationCore.Entites;

public partial class Suppliers
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string ContactName { get; set; }

    public string Phone { get; set; }

    public string Email { get; set; }

    public string Address { get; set; }

    public long? SupplierTypeId { get; set; }

    public LocalDateTime? CreatedAt { get; set; }

    public long CreatedBy { get; set; }

    public LocalDateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public LocalDateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
