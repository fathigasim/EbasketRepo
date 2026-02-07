using MassTransit;
using SecureApi.SharedContract;

namespace SecureApi.Publisher
{
    public class OrderPublisher
    {
        private readonly IPublishEndpoint _publish;

        public OrderPublisher(IPublishEndpoint publish)
        {
            _publish = publish;
        }

        public async Task PublishOrder(OrderCreatedEvent order)
        {
            await _publish.Publish(order);
        }
    }

}
