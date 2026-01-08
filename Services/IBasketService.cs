using SecureApi.Models;
using SecureApi.Models.DTOs;

namespace SecureApi.Services
{
    public interface IBasketService
    {
        Task<Basket> GetBasket(HttpContext httpContext, bool createIfNull = false);
        Task AddToBasket(HttpContext httpContext, string productId, int quantity = 1);
        Task RemoveFromBasket(HttpContext httpContext, string productId, int quantity = 1);
        Task<List<BasketItemViewModel>> GetBasketItems(HttpContext httpContext);
        Task ClearBasket(HttpContext httpContext);
        Task<BasketSummeryViewModel> GetBasketSummery(HttpContext httpContext);

    }
}
