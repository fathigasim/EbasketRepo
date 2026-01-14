using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureApi.Models
{
    [Index(nameof(OrderId))]
    [Index(nameof(ProductId))]
    public class OrderItems
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string OrderId { get; set; } = default!;

        [StringLength(255)]
        public string? ProductId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        // Navigation
        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = default!;

        [NotMapped]
        public decimal Subtotal => (Price * Quantity) - Discount;
    }
}
