using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;

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

        public async Task<List<string>> GetCategory()
        {
            var model = await _db.Category.Select(p =>  p.Name ).ToListAsync();
            
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
