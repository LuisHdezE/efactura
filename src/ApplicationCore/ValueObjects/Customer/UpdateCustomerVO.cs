namespace ApplicationCore.ValueObjects.Customer
{
    public class UpdateCustomerVO
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public long? CustomerTypeId { get; set; }

        public long UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }


    }
}
