namespace ApplicationCore.ValueObjects.Customer
{
    public class CreateCustomerVO
    {

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public long? CustomerTypeId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public long CreatedBy { get; set; }
    }
}
