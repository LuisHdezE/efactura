namespace ApplicationCore.ValueObjects.Product
{
    public class ListProductVO
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public int MinimumStock { get; set; }

        public long? ProductCategoryId { get; set; }
    }
}
