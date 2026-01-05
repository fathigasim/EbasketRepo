using SecureApi.Models.DTOs;

namespace SecureApi.Services
{
    public interface ICategoryService
    {
        Task<List<string>> GetCategory();
        Task PostCategory(CategoryDto categoryDto);
    }
}
