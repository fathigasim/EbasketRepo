using Microsoft.EntityFrameworkCore;
using SecureApi.Data;
using SecureApi.Models;

namespace SecureApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<PagedResult<Order>> GetAllAsync(int page = 1, int pageSize = 3)
        {
            var query =  _context.Order.Include(p => p.OrderItems).AsQueryable();

            var totalItems = query.Count();
            var model = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = new PagedResult<Order>
            {
                Items = model,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalItems,
            };
            return result;
        }

        public async Task GetAllAsync()
        {

            var query = _context.Order.Include(p => p.OrderItems).AsQueryable();

            var totalItems = query.Count();
            var model = await query.ToListAsync();

        }

        public Task<PagedResult<Order>> GetAsync(int page = 1, int pageSize = 3)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResult<Order>> GetAsync(DateTime date, int page = 1, int pageSize = 3)
        {
            var query = _context.Order.Include(p => p.OrderItems).AsQueryable();
            if (!string.IsNullOrEmpty(date.ToString()))
            {
                query = query.Where(p => p.CreatedAt.Date == date.Date);
            }
            var model = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalItems = query.Count();
            var result = new PagedResult<Order>
            {
                Items = model,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalItems,
            };

            return result;
        }

        //public Task<PagedResult<OrderSumVm>> GetOrderSumAsync(int page = 1, int pageSize = 3)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<PagedResult<OrderSumVm>> GetOrderSumAsync(int page = 1, int pageSize = 3)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
