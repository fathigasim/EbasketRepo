using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureApi.Services;

namespace SecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasketController : ControllerBase
    {
        IBasketService basketService;
        IHttpContextAccessor httpcontext;
        public BasketController(IBasketService _basketService, IHttpContextAccessor _httpcontext)
        {
            basketService = _basketService;
            httpcontext = _httpcontext;
        }
        [HttpGet("BasketItems")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var model = await basketService.GetBasketItems(HttpContext);

                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddToBasket(AddToBasketDto addToBasketDto)
        {
            try
            {

                await basketService.AddToBasket(httpcontext.HttpContext, addToBasketDto.ProdId, addToBasketDto.InputQnt);
                var UpdatedBasket = await basketService.GetBasketItems(httpcontext.HttpContext);
                return Ok(new { message = "Added Successfully", items = UpdatedBasket });
            }
            catch (Exception ex)
            {

                return BadRequest("some went wrong");

            }

        }


        [HttpGet("BasketSummery")]
        public async Task<IActionResult> GetBasketSummery()
        {
            var basketSummery = await basketService.GetBasketSummery(HttpContext);

            return Ok(basketSummery);

        }


        [HttpDelete("RemoveFromBasket")]
        public async Task<IActionResult> RemoveFromBasket([FromQuery] BasketRemoveDto basketRemoveDto)
        {

            await basketService.RemoveFromBasket(HttpContext, basketRemoveDto.productId, basketRemoveDto.quantity);

            var updatedBakset = await basketService.GetBasketItems(HttpContext);
            return Ok(updatedBakset);

        }

        [HttpDelete("ClearBasket")]
        public async Task<IActionResult> RemoveAllBasket()
        {
            try
            {
                await basketService.ClearBasket(HttpContext);
                return Ok("Basket cleared");
            }
            catch (ArgumentException ex)
            {
                return BadRequest("Basket already empty");
            }
        }

        [HttpDelete("RemoveBasket")]
        public async Task<IActionResult> RemoveBasket()
        {
            try
            {
                await basketService.RemoveBasket(HttpContext);
                return Ok("Basket Removed");
            }
            catch (ArgumentException ex)
            {
                return BadRequest("Basket Not Found");
            }
        }
        //[HttpGet("BasketItems")]
        //public async Task<IActionResult> BasketItems(HttpContext httpcontext)
        //{
        //   var BasketItems= await basketService.GetBasketItems(httpcontext);
        //    //  if(BasketItems == null)
        //    //{
        //    //    return BadRequest("");
        //    //}
        //    return Ok(BasketItems);
        //}

    }

    public record AddToBasketDto(string ProdId, int InputQnt);

    public record BasketRemoveDto(string productId, int quantity);
}

