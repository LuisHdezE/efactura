using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Entities
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        [Required]
        public int Stock { get; set; }

        [Required]
        public int MinimumStock { get; set; }

        public long? ProductCategoryId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public long UserId { get; set; }
    }
}
