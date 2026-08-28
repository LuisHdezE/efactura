using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Entities
{
    public class ContactDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Customer")]
        public long? CustomerId { get; set; }

        [Required]
        [ForeignKey("ContactType")]
        public long? ContactTypeId { get; set; }

        [Required]
        [Column("value")]
        [StringLength(100)]
        public string ContactValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        [ForeignKey("ContactTypeId")]
        public virtual ContactType ContactType { get; set; }
    }
}
