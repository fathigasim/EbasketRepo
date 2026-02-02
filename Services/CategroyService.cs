using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Distributed;
using SecureApi.Data;
using SecureApi.Helpers;
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
        private readonly IDistributedCache _cache;
        public CategroyService(ApplicationDbContext db,IMapper mapper, 
            ILogger<CategroyService> logger,
            IDistributedCache cache)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
        }

        public async Task<List<CategoryDto>> GetCategory()
        {
            string cacheKey = "categories_all";

            // try cache first
            var cached = await _cache.GetRecordAsync<List<CategoryDto>>(cacheKey);

            if (cached != null)
                return cached;

            var categories = await _db.Category.ToListAsync();
            // store in Redis
            await _cache.SetRecordAsync(
              cacheKey,
              categories,
              TimeSpan.FromMinutes(30));
            var model = _mapper.Map<List<CategoryDto>>(categories);
            return model;
           
          

        }
        public async Task PostCategory(CategoryDto categoryDto)
        {
          
                var category = _mapper.Map<Category>(categoryDto);
                await _db.Category.AddAsync(category);
                await _db.SaveChangesAsync();
                await _cache.RemoveAsync("categories_all");

        }
    }
}
