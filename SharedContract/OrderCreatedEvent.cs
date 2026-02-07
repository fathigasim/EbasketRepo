namespace SecureApi.SharedContract
{
    public record OrderCreatedEvent
    {
        public string OrderId { get; init; } = Guid.NewGuid().ToString();
        public string CustomerEmail { get; init; } = default!;
        public string ProductName { get; init; } = default!;
        public int Quantity { get; init; }
        public decimal Amount { get; init; }
        public DateTime CreatedAt { get; init; }=DateTime.Now;
    }
}
