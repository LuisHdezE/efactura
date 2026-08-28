namespace ApplicationCore.ValueObjects.CustomerType
{
    public class GetCustomerTypeVO
    {
        public long Id { get; private set; }
        public string Name { get; private set; }
        public int UserId { get; private set; }
    }
}
