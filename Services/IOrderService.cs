using Microsoft.AspNetCore.Mvc;
using SecureApi.Models;
using SecureApi.Models.DTOs;

namespace SecureApi.Services
{
    public interface IOrderService
    {
        public  Task<PagedResult<OrderDto>> GetAsync(int page = 1, int pageSize = 3);

        public Task<List<OrderDto>> GetAllAsync();

        public Task<PagedResult<OrderDto>> GetAsync(DateTime date, int page = 1, int pageSize = 3);

      //  public Task<PagedResult<OrderSumVm>> GetOrderSumAsync(int page = 1, int pageSize = 3);
    }
}
