using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        [StringLength(50)]
        public string? Status { get; set; }
        [StringLength(100)]
        public string? OrderReference { get; set; }
        [StringLength(100)]
        public string? StripeSessionId { get; set; }
        [StringLength(100)]
        public string? StripePaymentIntentId { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
        public ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();


    }
}
