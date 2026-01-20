using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;

namespace SecureApi.Services
{
    public class ProductService : IProductService 
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        public ProductService(ApplicationDbContext context, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<PagedResult<ProductDto>> Get(string? q, string? category,string? sort, int page, int pageSize)
        {
            var Request = _contextAccessor?.HttpContext?.Request;
            var query = _context.Product.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(q))
                query = query.Where(p => p.Name.Contains(q));
            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category.Name.Contains(category));
            if (sort == "lowToHigh")
                query = query.OrderBy(p => p.Price);
            else if (sort == "highToLow")
                query = query.OrderByDescending(p => p.Price);

            var totalItems = query.Count();

            var products = await query
                //Select(p=> new ProductDto { Name=p.Name,Price=p.Price,ImageUrl= $"{Request.Scheme}://{Request.Host}/StaticImages/{p.ImagePath}"})
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            // Map Product entities to ProductDto
            var productDtos = _mapper.Map<List<ProductDto>>(products);

            // Set ImageUrl for each DTO
            foreach (var dto in productDtos)
            {
                dto.ImageUrl = $"{Request?.Scheme}://{Request?.Host}/StaticImages/{dto.ImagePath}";
            }
            //var model=  mapper.Map<ProductDto>(products);
            var result = new PagedResult<ProductDto>
            {
                Items = productDtos,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalItems
            };
            return result;
        }

        
        public async Task PostProudct( ProductDto productdto)
        {
            
             
                if (!Directory.Exists("Img") && productdto.Image !=null)
                {
                    Directory.CreateDirectory("Img");
                }
            var filename = Guid.NewGuid().ToString().Substring(1, 8) + productdto?.Image?.FileName;
                
                var uploadfolder = Path.Combine(Directory.GetCurrentDirectory(), "Img");

                var filepath = Path.Combine(uploadfolder, filename);
                using (var filestream = new FileStream(filepath, FileMode.Create))
                {
                    productdto.Image?.CopyTo(filestream);
                }
                productdto.ImagePath = filename ;

                var product = new Product
                {
                    Name = productdto.Name,
                    Price = productdto.Price,
                    ImagePath = productdto.ImagePath,
                    Stock=productdto.Stock,
                    CategoryId=productdto.CategoryId
                };

                await _context.Product.AddAsync(product);
                await _context.SaveChangesAsync();

                // var successMsg = _localizer["ProductAdd"].Value;
               
            
          
        }

        public async Task UpdateProductAsync(string id, ProductDto productDto)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid product id.", nameof(id));

            var productToUpdate = await _context.Product.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (productToUpdate == null)
                throw new ArgumentNullException("Invalid product id.", nameof(productToUpdate));


            var sourcefolder = Path.Combine(Directory.GetCurrentDirectory(), "Img");
            Directory.CreateDirectory(sourcefolder); // Ensure folder exists

            var oldImagePath = Path.Combine(sourcefolder, productToUpdate.ImagePath);

            if (productDto.Image != null && productDto.Image.Length > 0)
            {
                // Delete old image if exists
                if (File.Exists(oldImagePath))
                {
                    File.Delete(oldImagePath);
                }

                // Generate new name
                var extension = Path.GetExtension(productDto.Image.FileName);
                var filename = Guid.NewGuid().ToString("N").Substring(0, 8) + extension;
                var newImagePath = Path.Combine(sourcefolder, filename);

                // Save new image
                using (var filestream = new FileStream(newImagePath, FileMode.Create))
                {
                    await productDto.Image.CopyToAsync(filestream);
                }

                productToUpdate.ImagePath = filename;
            }

            // Update other fields
            productToUpdate.Name = productDto.Name;
            productToUpdate.Price = productDto.Price;
            productToUpdate.Stock = productDto.Stock;
            // Save changes
            _context.Product.Update(productToUpdate); // Ensure EF is tracking
            await _context.SaveChangesAsync();

           
        }
    }
}
