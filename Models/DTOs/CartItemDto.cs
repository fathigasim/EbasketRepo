using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models.DTOs
{
    public class CartItemDto
    {
        [Required]
        public string ProductId { get; set; } = "";

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = "";

        [Url]
        public string? Image { get; set; }

        [Range(0.01, 1000000)]
        public decimal Price { get; set; }

        [Range(1, 999)]
        public int Quantity { get; set; }
    }
}
