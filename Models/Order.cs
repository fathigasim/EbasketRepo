using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureApi.Models
{
 
        [Index(nameof(OrderReference), IsUnique = true)]
        [Index(nameof(UserId))]
        [Index(nameof(Status))]
        [Index(nameof(StripeSessionId))]
        [Index(nameof(CreatedAt))]
        public class Order
        {
            [Key]
            public string Id { get; set; } = Guid.NewGuid().ToString().Substring(1,8).ToUpper();

            /// <summary>
            /// ASP.NET Identity User ID (string)
            /// </summary>
            [Required]
            public string UserId { get; set; } = default!;

            [Required]
            [StringLength(12)]
            public string OrderReference { get; set; } = "";

            [Required]
            [Column(TypeName = "decimal(18,2)")]
            public decimal TotalAmount { get; set; }
            public decimal VatAmount { get; set; }
            public decimal SubTotal { get; set; }
        

             [Required]
            [StringLength(50)]
            public string Status { get; set; } = OrderStatus.Pending;

            [StringLength(255)]
            public string? StripeSessionId { get; set; }

            [StringLength(255)]
            public string? StripePaymentIntentId { get; set; }

            [StringLength(50)]
            public string? PaymentMethod { get; set; }

            [StringLength(500)]
            public string? FailureReason { get; set; }

            public DateTime? SessionExpiresAt { get; set; }

            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }
            public DateTime? PaidAt { get; set; }
            public DateTime? CancelledAt { get; set; }

            // ✅ Navigation to Identity User
            [ForeignKey(nameof(UserId))]
            public ApplicationUser User { get; set; } = default!;

            public ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();

            // Computed properties
            [NotMapped]
            public bool IsExpired => SessionExpiresAt.HasValue && SessionExpiresAt.Value < DateTime.UtcNow;

            [NotMapped]
            public bool CanBeCancelled => Status == OrderStatus.Pending && !IsExpired;

            [NotMapped]
            public int TotalItems => OrderItems?.Sum(i => i.Quantity) ?? 0;
        }

        public static class OrderStatus
        {
            public const string Pending = "Pending";
            public const string Paid = "Paid";
            public const string Expired = "Expired";
            public const string Cancelled = "Cancelled";
            public const string PaymentFailed = "PaymentFailed";
            public const string Refunded = "Refunded";
            public const string Processing = "Processing";
            public const string Shipped = "Shipped";
            public const string Delivered = "Delivered";

            public static readonly string[] All =
            {
        Pending, Paid, Expired, Cancelled, PaymentFailed,
        Refunded, Processing, Shipped, Delivered
    };

            public static bool IsValid(string status) => All.Contains(status);
        }
    }

