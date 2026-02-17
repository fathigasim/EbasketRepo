using Microsoft.EntityFrameworkCore;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using Stripe;

namespace SecureApi.Services
{
    public class BasketService : IBasketService
    {

        private readonly ApplicationDbContext context;
        public const string BasketSessionName = "ShoppingCommerceBasket";
        private readonly TimeSpan _cookieExpiry = TimeSpan.FromDays(7);
        private readonly StockService _stockService;
        private readonly ILogger<BasketService> _logger;

        public BasketService(ApplicationDbContext _context, StockService stockService,
            ILogger<BasketService> logger)
        {
            context = _context;
            _stockService = stockService;
            _logger = logger;
            ;
        }



        public async Task<Basket> GetBasket(HttpContext httpContext, bool createIfNull = false)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            var cookie = httpContext.Request.Cookies[BasketSessionName];
            Basket basket = null;

            if (!string.IsNullOrWhiteSpace(cookie))
            {
                basket = await context.Basket
                    .Include(b => b.BasketItems)
                    .FirstOrDefaultAsync(b => b.BasketId == cookie);
            }

            if (basket == null && createIfNull)
            {
                basket = new Basket();
                await context.Basket.AddAsync(basket);
                await context.SaveChangesAsync();

                var options = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.Add(_cookieExpiry),
                    HttpOnly = true,
                    SameSite = SameSiteMode.None, // Better default, use None only if needed
                    Secure = true,
                    Path = "/"
                };

                httpContext.Response.Cookies.Append(BasketSessionName, basket.BasketId, options);

                _logger.LogInformation($"Cookie set: {BasketSessionName}={basket.BasketId}");
            }

            return basket;
        }
        public async Task AddToBasket(HttpContext httpContext, string productId, int quantity = 1)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be > 0", nameof(quantity));

            var basket = await GetBasket(httpContext, true);
            if (basket == null)
                throw new InvalidOperationException("Failed to get or create basket");

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Load item within transaction
                var item = await context.BasketItems
                    .FirstOrDefaultAsync(i => i.BasketId == basket.BasketId && i.ProductId == productId);

                // Get and update product stock
                var product = await context.Product.FindAsync(productId);
                if (product == null)
                    throw new InvalidOperationException($"Product {productId} not found");

                if (product.Stock < quantity)
                    throw new InvalidOperationException($"Insufficient stock. Available: {product.Stock}");

                // Decrease stock
                product.Stock -= quantity;

                if (item == null)
                {
                    item = new BasketItems
                    {
                        BasketId = basket.BasketId,
                        ProductId = productId,
                        Quantity = quantity
                    };
                    await context.BasketItems.AddAsync(item);
                }
                else
                {
                    item.Quantity += quantity;
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Added {quantity}x {productId} to basket {basket.BasketId}");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task RemoveFromBasket(HttpContext httpContext, string productId, int quantity = 1)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be > 0", nameof(quantity));

            var basket = await GetBasket(httpContext, false);
            if (basket == null) return;

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var item = basket.BasketItems
                    .FirstOrDefault(i => i.ProductId == productId);

                if (item == null) return;

              
                var product = await context.Product
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null) return;

                if (item.Quantity <= quantity)
                {
                  
                    product.Stock += item.Quantity;  
                    context.BasketItems.Remove(item);

                  
                }
                else
                {
                 
                    item.Quantity -= quantity;
                    product.Stock += quantity;
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

  
                if (item.Quantity <= quantity)
                {
                    var hasItems = await context.BasketItems.AnyAsync(i => i.BasketId == basket.BasketId);
                    if (!hasItems)
                    {
                        httpContext.Response.Cookies.Delete(BasketSessionName);
                    }
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        public async Task<List<BasketItemViewModel>> GetBasketItems(HttpContext httpContext)
        {
            var basket = await GetBasket(httpContext, false);
            if (basket == null) return new List<BasketItemViewModel>();

            var items = await context.BasketItems
                .Include(bi => bi.Product)
                .Where(bi => bi.BasketId == basket.BasketId)
                .Select(bi => new BasketItemViewModel
                {
                    Id = bi.BasketitemId,
                    ProductId = bi.ProductId,
                    Quantity = bi.Quantity,
                    ProductName = bi.Product.Name,
                    Price = bi.Product.Price,
                    Image = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/StaticImages/{bi.Product.ImagePath}"
                })
                .ToListAsync();

            return items;
        }
        public async Task<BasketSummeryViewModel> GetBasketSummery(HttpContext httpContext)
        {
            var basket = await GetBasket(httpContext, false);
            if (basket == null)
                return new BasketSummeryViewModel(0, 0);

            var items = await context.BasketItems
                .Include(bi => bi.Product)
                .Where(bi => bi.BasketId == basket.BasketId)
                .ToListAsync();

            var total = items.Sum(bi => bi.Quantity * bi.Product.Price);
            var count = items.Count;

            return new BasketSummeryViewModel(count,total);
        }
        //public async Task<BasketSummeryViewModel> GetBasketSummery(HttpContext httpContext)
        //{
        //    var basket = await GetBasket(httpContext, false);
        //    if (basket == null)
        //        return new BasketSummeryViewModel(0, 0);

        //    // Single query with both calculations
        //    var summary = await context.BasketItems
        //        .Include(bi => bi.Product)
        //        .Where(bi => bi.BasketId == basket.BasketId)
        //        .GroupBy(bi => 1) // Dummy grouping for aggregate
        //        .Select(g => new
        //        {
        //            Total = g.Sum(bi => bi.Quantity * bi.Product.Price),
        //            Count = g.Count()
        //        })
        //        .FirstOrDefaultAsync();

        //    if (summary == null)
        //        return new BasketSummeryViewModel(0, 0);

        //    return new BasketSummeryViewModel(summary.Total, summary.Count);
        //}

        public async Task ClearBasket(HttpContext httpContext)
        {
            var basket = await GetBasket(httpContext, false);
            if (basket == null) return;

            await using var transaction = await context.Database.BeginTransactionAsync(); // Fixed typo
            try
            {
                var basketItems = await context.BasketItems
                    .Where(i => i.BasketId == basket.BasketId)
                    .ToListAsync();

                if (!basketItems.Any())
                {
                    await transaction.CommitAsync();
                    return;
                }

                var productIds = basketItems.Select(i => i.ProductId).Distinct();
                var products = await context.Product
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in products)
                {
                    product.Stock += basketItems
                        .Where(p => p.ProductId == product.Id)
                        .Sum(p => p.Quantity);
                }

                context.BasketItems.RemoveRange(basketItems);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ Delete cookie after clearing
                httpContext.Response.Cookies.Delete(BasketSessionName);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RemoveBasket(HttpContext httpContext)
        {
            var basket = await GetBasket(httpContext, false);
            if (basket == null) return;

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Get product IDs before removing basket
                var productIds = basket.BasketItems
                    .Select(bi => bi.ProductId)
                    .Distinct()
                    .ToList();

                // Load products
                var products = await context.Product
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                // Return stock
                foreach (var product in products)
                {
                    var totalQuantity = basket.BasketItems
                        .Where(bi => bi.ProductId == product.Id)
                        .Sum(bi => bi.Quantity);

                    product.Stock += totalQuantity;
                }

                // Remove basket (should cascade delete items if configured)
                context.Basket.Remove(basket);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Delete cookie AFTER successful transaction
                httpContext.Response.Cookies.Delete(BasketSessionName);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

}
