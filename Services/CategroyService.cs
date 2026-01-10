using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;

namespace SecureApi.Services
{
    public class CategroyService :ICategoryService
    {
        ApplicationDbContext _db;
        IMapper _mapper;
        ILogger<CategroyService> _logger;
        public CategroyService(ApplicationDbContext db,IMapper mapper, ILogger<CategroyService> logger)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<CategoryDto>> GetCategory()
        {
            
            var categories = await _db.Category.ToListAsync();
         var model = _mapper.Map<List<CategoryDto>>(categories);
            return model;
        }
        public async Task PostCategory(CategoryDto categoryDto)
        {
          
                var category = _mapper.Map<Category>(categoryDto);
                await _db.Category.AddAsync(category);
                await _db.SaveChangesAsync();
          
        }
    }
}
