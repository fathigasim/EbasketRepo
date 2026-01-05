using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models
{
    public class Basket
    {
        [Key]
        [MaxLength(450)]
        public string BasketId { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<BasketItems> BasketItems { get; set; } = new();

    }
}
