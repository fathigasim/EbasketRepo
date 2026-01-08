using Microsoft.EntityFrameworkCore;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;

namespace SecureApi.Services
{
    public class BasketService : IBasketService
    {

        private readonly ApplicationDbContext context;
        public const string BasketSessionName = "ShoppingCommerceBasket";
        private readonly TimeSpan _cookieExpiry = TimeSpan.FromDays(7);
        private readonly StockService _stockService;
        public BasketService(ApplicationDbContext _context, StockService stockService)
        {
            context = _context;
            _stockService = stockService;
        }



        public async Task<Basket> GetBasket(HttpContext httpContext, bool createIfNull = false)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            var cookie = httpContext.Request.Cookies[BasketSessionName];
            Basket basket = null;

            if (!string.IsNullOrWhiteSpace(cookie))
            {
                basket = await context.Basket
                    .Include(b => b.BasketItems).AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BasketId == cookie);


            }

            if (basket == null && createIfNull)
            {
                try
                {
                    basket = new Basket(); // BasketId generated via constructor default
                    await context.Basket.AddAsync(basket);
                    await context.SaveChangesAsync(); // ensure BasketId persisted


                    var options = new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.Add(_cookieExpiry),
                        HttpOnly = true,
                        SameSite = SameSiteMode.None, // IMPORTANT for cross-site cookies
                        Secure = true,
                        Path = "/"
                    };
                    httpContext.Response.Cookies.Append(BasketSessionName, basket.BasketId, options);


                }

                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }

            return basket;
        }

        public async Task AddToBasket(HttpContext httpContext, string productId, int quantity = 1)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be > 0", nameof(quantity));
            var basket = GetBasket(httpContext, true);

            var item = await context.BasketItems
                .FirstOrDefaultAsync(i => i.BasketId == basket.Result.BasketId && i.ProductId == productId);

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    if (item == null)
                    {
                        item = new BasketItems
                        {
                            BasketId = basket.Result.BasketId,
                            ProductId = productId,
                            Quantity = quantity
                        };

                        await _stockService.CheckStock(productId, quantity);
                        await context.BasketItems.AddAsync(item);
                        await context.SaveChangesAsync();



                    }



                    else
                    {

                        await _stockService.CheckStock(productId, quantity);
                        item.Quantity += quantity;
                        context.BasketItems.Update(item);
                        await context.SaveChangesAsync();
                    }
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception(ex.Message);
                }
            }
        }
        public async Task RemoveFromBasket(HttpContext httpContext, string productId, int quantity = 1)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be > 0", nameof(quantity));
            var basket = await GetBasket(httpContext, false);
            if (basket == null) return;

            var item = await context.BasketItems
                .FirstOrDefaultAsync(i => i.BasketId == basket.BasketId && i.ProductId == productId);

            if (item == null) return;

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                context.BasketItems.Remove(item);
            }
            else
            {
                context.BasketItems.Update(item);
            }

            await context.SaveChangesAsync();
        }

        public async Task<List<BasketItemViewModel>> GetBasketItems(HttpContext httpContext)
        {
            var Request = httpContext.Request;
            var basket = await GetBasket(httpContext, false);
            if (basket == null) return new List<BasketItemViewModel>();

            // Join with products if product data is in same DB. Otherwise return basic data.
            var query = from bi in context.BasketItems.Include(p => p.Product)
                        where bi.BasketId == basket.BasketId
                        select new BasketItemViewModel
                        {
                            Id = bi.BasketitemId,
                            ProductId = bi.ProductId,
                            Quantity = bi.Quantity,
                            ProductName = bi.Product.Name,
                            Price = bi.Product.Price,
                            Image = $"{Request.Scheme}://{Request.Host}/StaticImages/{bi.Product.ImagePath}"
                        };

            return await query.ToListAsync();
        }

        public async Task<BasketSummeryViewModel> GetBasketSummery(HttpContext httpContext)
        {
            var model = new BasketSummeryViewModel(0, 0);
            var basket = await GetBasket(httpContext, false);
            if (basket == null) return model;
            model.BasketTotal = context.BasketItems.Include(p => p.Product).Where(p => p.BasketId == basket.BasketId).Sum(p => p.Quantity * p.Product.Price);
            model.BasketCount = context.BasketItems.Include(p => p.Product).Where(p => p.BasketId == basket.BasketId).Select(p => p.ProductId).Count();
            return model;

        }
        public async Task ClearBasket(HttpContext httpContext)
        {
            var basket = await GetBasket(httpContext, false);
            if (basket == null) return;

            var items = context.BasketItems.Where(i => i.BasketId == basket.BasketId).ExecuteDeleteAsync();


            await context.SaveChangesAsync();
        }
    }

}
