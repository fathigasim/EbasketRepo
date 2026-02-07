using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureApi.Models;
using SecureApi.Publisher;
using SecureApi.Services;

namespace SecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
        public class OrderController : ControllerBase
        {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<Order>>> GetAsync(int page = 1, int pageSize = 3)
        {    
              
         var result = await _orderService.GetAsync(page, pageSize);
              
             return Ok(result);
        }


        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllAsync()
        {
         var result= await _orderService.GetAllAsync();
         return Ok(result);
        }


        [HttpGet("{date}")]
        public async Task<ActionResult<PagedResult<Order>>> GetAsync(DateTime date, int page = 1, int pageSize = 3)
        {
            var result=   await _orderService.GetAsync(date, page, pageSize);

            return Ok(result);
        }

        //[HttpGet("OrderSum")]
        //public async Task<ActionResult<PagedResult<OrderSumVm>>> GetOrderSumAsync(int page = 1, int pageSize = 3)
        //{
        //    var query = (from ord in dbContext.Order
        //                 join itm in dbContext.OrderItems
        //               on ord.Id equals itm.OrderId
        //                 group new { ord, itm }
        //               by ord.Id into g
        //                 select new OrderSumVm
        //                 {
        //                     OrderId = g.Key,
        //                     Total = g.Sum(p => p.itm.Quantity * p.itm.Price),
        //                     Product = g.First().itm.Name ?? "",
        //                     Items = g.Select(p =>
        //                     new OrderSumItemsVm
        //                     {
        //                         ProductName = p.itm.Name,
        //                         ProductQuantity = p.itm.Quantity,
        //                         ProductPrice = p.itm.Price
        //                     }).ToList(),
        //                 }).AsQueryable();

        //    var model = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        //    var totalItems = query.Count();
        //    var result = new PagedResult<OrderSumVm> { Items = model, PageNumber = page, PageSize = pageSize, TotalItems = totalItems, };
        //    return Ok(result);
        //}
        //[HttpPost]
        //public async Task<IActionResult> Post()
        //{
        //    var order = new Order();
        //   await dbContext.Order.AddAsync(order);
        //    await dbContext.SaveChangesAsync();
        //    return Ok();

        //}
//        await _orderPublisher.PublishOrder(new OrderCreatedEvent
//{
//    OrderId = Guid.NewGuid().ToString(),
//    CustomerEmail = "fathi@example.com",
//    ProductId = "P001",
//    Quantity = 2,
//    Amount = 199.99m,
//    CreatedAt = DateTime.UtcNow
//    });

    }
}
