using ApplicationCore.Entites;
using NodaTime;

namespace ApplicationCore.ValueObjects.Customer
{
    public class ListCustomerVO
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public long? CustomerTypeId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public long UserId { get; set; }

       // public virtual ICollection<PurchaseOrders> PurchaseOrders { get; set; } = new List<PurchaseOrders>();

       // public virtual CustomerTypes customer_type { get; set; }
    }
}
