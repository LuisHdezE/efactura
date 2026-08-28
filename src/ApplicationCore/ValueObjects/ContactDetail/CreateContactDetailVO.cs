namespace ApplicationCore.ValueObjects.ContactDetail
{
    public class CreateContactDetailVO : IEquatable<CreateContactDetailVO>
    {
        public CreateContactDetailVO(long customerId, long contactTypeId, string contactValue)
        {
            CustomerId = customerId;
            ContactTypeId = contactTypeId;
            ContactValue = contactValue;
        }

        public long CustomerId { get; }
        public long ContactTypeId { get; }
        public string ContactValue { get; }

        public bool Equals(CreateContactDetailVO other)
        {
            if (other is null) return false;
            return CustomerId == other.CustomerId &&
                   ContactTypeId == other.ContactTypeId &&
                   ContactValue == other.ContactValue;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CreateContactDetailVO);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CustomerId, ContactTypeId, ContactValue);
        }
    }

}
