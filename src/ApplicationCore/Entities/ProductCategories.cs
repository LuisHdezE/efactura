using NodaTime;

namespace ApplicationCore.Entites;

public partial class ProductCategories
{
    public long Id { get; set; }

    public string Name { get; set; }

    public LocalDateTime? CreatedAt { get; set; }

    public long CreatedBy { get; set; }

    public LocalDateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public LocalDateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
