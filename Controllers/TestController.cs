using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureApi.Models.DTOs;
using SecureApi.Publisher;
using SecureApi.SharedContract;

namespace SecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly OrderPublisher orderPublisher;
        public TestController(OrderPublisher orderPublisher )
        {
            this.orderPublisher = orderPublisher;
        }
        [HttpPost("Send")]
        public async Task<IActionResult> Send(OrderCreatedEventDto orderCreatedEventDto)
        {
            OrderCreatedEvent orderCreatedEvent = new OrderCreatedEvent
            {
                CustomerEmail = orderCreatedEventDto.CustomerEmail,
                ProductName = orderCreatedEventDto.ProductName,
                Amount = orderCreatedEventDto.Amount,
                Quantity = orderCreatedEventDto.Quantity,

            };
            await orderPublisher.PublishOrder(orderCreatedEvent);
            //var data = new List<string> { "one", "two", "three" };
            return Ok("Order Created Successfully");
        }
    }
}