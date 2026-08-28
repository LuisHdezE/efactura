namespace ApplicationCore.ValueObjects.TaxType
{
    public class UpdateTaxTypeVO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal Rate { get; set; }
    }
}
