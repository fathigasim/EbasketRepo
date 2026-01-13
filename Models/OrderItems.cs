using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models
{
    public class OrderItems
    {
        [Key]
        public int ItemId { get; set; }
       
        public string ProductId { get; set; } = default!;
        public Product Product { get; set; } = default!;
        public int OrderId { get; set; }
        public Order Order { get; set; }=default!;
        public string? Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }

    }
}
