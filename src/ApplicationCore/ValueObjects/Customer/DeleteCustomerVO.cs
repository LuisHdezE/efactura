namespace ApplicationCore.ValueObjects.Customer
{
    public class DeleteCustomerVO
    {
        public long Id { get; set; }

        public long DeletedBy { get; set; }

        public DateTime? DeletedAt { get; set; }


    }
}
