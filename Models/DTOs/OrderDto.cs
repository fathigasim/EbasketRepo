using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureApi.Models.DTOs
{
    public class OrderDto
    {

        public string Id { get; set; } = default!;

        /// <summary>
        /// ASP.NET Identity User ID (string)
        /// </summary>
     
        public string UserId { get; set; } = default!;

    
        public string OrderReference { get; set; } = "";

       
        public decimal TotalAmount { get; set; }

        
        public string Status { get; set; } 

       
        public string? StripeSessionId { get; set; }

     
        public string? StripePaymentIntentId { get; set; }

       
        public string? PaymentMethod { get; set; }

      
        public string? FailureReason { get; set; }

        public DateTime? SessionExpiresAt { get; set; }

     
        public DateTime CreatedAt { get; set; } 

        public DateTime? UpdatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CancelledAt { get; set; }


        public ICollection<OrderItemsDto> OrderItems { get; set; } = new List<OrderItemsDto>();
      
        public bool IsExpired { get; set; }


        public bool CanBeCancelled { get; set; }

    }
}
