using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Entities
{
    public class VoucherType
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }
    }
}
