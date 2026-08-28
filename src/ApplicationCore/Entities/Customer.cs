using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Entities
{
    [Table("customer", Schema = "public")]
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        [Column("unique_code")]
        public string UniqueCode { get; set; }

        public int CustomerTypeId { get; set; }
        public int? DepartmentId { get; set; }

        [StringLength(80)]
        public string City { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        [StringLength(5)]
        public string ZipCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CustomerTypeId")]
        public virtual CustomerType CustomerType { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        public virtual ICollection<ContactDetail> ContactDetails { get; set; }


    }
}