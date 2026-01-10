using SecureApi.Models.DTOs;

namespace SecureApi.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetCategory();
        Task PostCategory(CategoryDto categoryDto);
    }
}
