namespace SecureApi.Models.DTOs
{
    public class BasketItemViewModel
    {
        public int Id { get; set; }

        public int Quantity { get; set; }
        public string ProductId { get; set; } = default!;
        public string ProductName { get; set; } = default!;

        public decimal Price { get; set; }

        public string Image { get; set; } = default!;

    }
}
