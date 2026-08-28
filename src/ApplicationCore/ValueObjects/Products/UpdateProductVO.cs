using NodaTime;

namespace ApplicationCore.ValueObjects.Products
{
    public class UpdateProductVO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public long? ProductCategoryId { get; set; }
        public LocalDateTime? CreatedAt { get; set; }
        public long UserId { get; set; }
        public long UpdatedBy { get; set; }
    }
}
