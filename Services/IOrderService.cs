using Microsoft.AspNetCore.Mvc;
using SecureApi.Models;

namespace SecureApi.Services
{
    public interface IOrderService
    {
        public  Task<PagedResult<Order>> GetAsync(int page = 1, int pageSize = 3);

        public  Task GetAllAsync();

        public Task<PagedResult<Order>> GetAsync(DateTime date, int page = 1, int pageSize = 3);

      //  public Task<PagedResult<OrderSumVm>> GetOrderSumAsync(int page = 1, int pageSize = 3);
    }
}
