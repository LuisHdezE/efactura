namespace ApplicationCore.ValueObjects.Supplier
{
    public class CreateSupplierVO
    {
        public string Name { get; set; }

        public string ContactName { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }

        public long? SupplierTypeId { get; set; }

        public long UserId { get; set; }
    }
}
