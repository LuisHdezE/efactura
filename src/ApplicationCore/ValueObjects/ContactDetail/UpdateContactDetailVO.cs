namespace ApplicationCore.ValueObjects.ContactDetail
{
    public class UpdateContactDetailVO : IEquatable<UpdateContactDetailVO>
    {
        public UpdateContactDetailVO(long id, long customerId, long contactTypeId, string contactValue)
        {
            Id = id;
            CustomerId = customerId;
            ContactTypeId = contactTypeId;
            ContactValue = contactValue;
        }

        public long Id { get; private set; }
        public long CustomerId { get; private set; }
        public long ContactTypeId { get; private set; }
        public string ContactValue { get; private set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UpdateContactDetailVO)obj);
        }

        public bool Equals(UpdateContactDetailVO other)
        {
            return CustomerId == other.CustomerId &&
                   ContactTypeId == other.ContactTypeId &&
                   ContactValue == other.ContactValue;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CustomerId, ContactTypeId, ContactValue);
        }
    }
}