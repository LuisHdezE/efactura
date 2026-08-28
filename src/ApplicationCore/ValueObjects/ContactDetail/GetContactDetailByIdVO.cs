namespace ApplicationCore.ValueObjects.ContactDetail
{
    public class GetContactDetailVO : IEquatable<GetContactDetailVO>
    {
        public GetContactDetailVO(int countryId, string name, string code)
        {
            CountryId = countryId;
            Name = name;
            Code = code;
        }

        public int CountryId { get; }
        public string Name { get; }
        public string Code { get; }

        public bool Equals(GetContactDetailVO other)
        {
            if (other is null) return false;
            return CountryId == other.CountryId &&
                   Name == other.Name &&
                   Code == other.Code;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GetContactDetailVO);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CountryId, Name, Code);
        }
    }
}

