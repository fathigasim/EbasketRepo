using Microsoft.EntityFrameworkCore;
using SecureApi.Data;

namespace SecureApi.Services
{
    public class StockService
    {
        ApplicationDbContext context;
        public StockService(ApplicationDbContext _context)
        {
            context = _context;
        }

        public async Task CheckStock(string productId, int productQnt)
        {
            if (productId == null)
            {
                throw new ArgumentNullException("Product is null");
            }
            var productStock = await context.Product.Where(p => p.Id == productId).FirstOrDefaultAsync();
            if (productStock.Stock <= 0)
            {
                throw new ArgumentException("no proudct in the stock");
            }
            if (productStock.Stock < productQnt)
            {
                throw new ArgumentException("Quantity requested exceeds stock");

            }
            productStock.Stock -= productQnt;
            context.Update(productStock);
            await context.SaveChangesAsync();
        }

        public async Task UpdateStock(string productId, int productQnt)
        {
            if (productId == null)
            {
                throw new ArgumentNullException("Product is null");
            }
            var productStock = await context.Product.Where(p => p.Id == productId).Select(p => p.Stock).FirstOrDefaultAsync();
            if (productStock <= 0)
            {
                throw new ArgumentException("no proudct in the stock");
            }

        }
    }

}
