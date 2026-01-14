using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureApi.Models
{
    [Index(nameof(EventId), IsUnique = true)]
    [Index(nameof(ProcessedAt))]
    public class StripeWebhookEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string EventId { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = "";

        [Required]
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? Payload { get; set; }
    }
}
