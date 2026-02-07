using MassTransit;
using SecureApi.SharedContract;

namespace SecureApi.Consumers
{
    public class PaymentConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly ILogger<InventoryConsumer> _logger;
        public PaymentConsumer(ILogger<InventoryConsumer> logger)
        {
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var order = context.Message;

            _logger.LogInformation("Processing payment for order {OrderId}", order.OrderId);

            try
            {
                // Simulate payment processing
                await Task.Delay(2000);

                _logger.LogInformation("Payment processed successfully: ${Amount} for order {OrderId}",
                    order.Amount, order.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment for order {OrderId}", order.OrderId);
                throw; // MassTransit will handle retry
            }
        }
    }
}
