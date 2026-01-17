using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;

namespace SecureApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public OrderService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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

        public async Task<List<OrderDto>> GetAllAsync()
        {
            var orders = await _context.Order
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // If OrderItemDto needs ProductName
                .OrderByDescending(o => o.CreatedAt) // ✅ Add ordering
                .AsNoTracking() // ✅ Performance boost for read-only queries
                .ToListAsync();

            return _mapper.Map<List<OrderDto>>(orders);
        }

        public async Task<PagedResult<OrderDto>> GetAsync(int page = 1, int pageSize = 3)
        {
            var query = _context.Order.Include(p => p.OrderItems).AsQueryable();

            var totalItems = query.Count();
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var model= _mapper.Map<List<OrderDto>>(orders);

            var result = new PagedResult<OrderDto>
            {
                Items = model.ToList(),
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalItems
            };
            return result;
        }

        public async Task<PagedResult<OrderDto>> GetAsync(DateTime date, int page = 1, int pageSize = 3)
        {
            var query = _context.Order.Include(p => p.OrderItems).AsQueryable();
            if (!string.IsNullOrEmpty(date.ToString()))
            {
                query = query.Where(p => p.CreatedAt.Date == date.Date);
            }
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var model = _mapper.Map<List<OrderDto>>(orders);
            var totalItems = query.Count();
            var result = new PagedResult<OrderDto>
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
