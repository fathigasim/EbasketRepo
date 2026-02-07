namespace SecureApi.Models.DTOs
{
    public class OrderCreatedEventDto
    {
        public string CustomerEmail { get; init; } = default!;
        public string ProductName { get; init; } = default!;
        public int Quantity { get; init; }
        public decimal Amount { get; init; }
    }
}
