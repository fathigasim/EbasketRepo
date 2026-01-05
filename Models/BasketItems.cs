using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureApi.Models
{
    public class BasketItems
    {
        [Key]
        public int BasketitemId { get; set; }

        [Required]
        [MaxLength(450)]
        public string BasketId { get; set; }

        [Required]
        public string ProductId { get; set; }   // you may use Guid/ string depending on your Product PK

        [Required]
        public int Quantity { get; set; } = 1;

        // navigation
        [ForeignKey(nameof(BasketId))]
        public Basket Basket { get; set; }

        // optional navigation to Product (if in same DB)
        public Product Product { get; set; }

    }
}
