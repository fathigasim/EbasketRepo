using Microsoft.AspNetCore.Mvc;
using SecureApi.Models;
using SecureApi.Models.DTOs;

namespace SecureApi.Services.Interfaces
{
    public interface IProductService 
    {
        Task<PagedResult<ProductDto>> Get(string? q = "",string?category="", string? sort = "", int page = 1, int pageSize = 5);

        Task PostProudct(ProductDto productdto);
    }
}
