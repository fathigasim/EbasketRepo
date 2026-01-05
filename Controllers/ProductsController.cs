using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using System.Globalization;
using System.Reflection;

namespace SecureApi.Controllers
{
    public class ProductsController : ControllerBase
    {
        ApplicationDbContext dbContext;
        IMapper mapper;
        public ProductsController(ApplicationDbContext _dbContext, IStringLocalizerFactory factory, IMapper _mapper
          )
        {
            dbContext = _dbContext;
            mapper = _mapper;
           
        }
        //[Authorize(Roles ="User,Admin")]
        //[Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResult<Product>>> Get(string? q = "", string? sort = "", int page = 1, int pageSize = 5)
        {
            var query = dbContext.Product.AsQueryable();
            if (!string.IsNullOrEmpty(q))
                query = query.Where(p => p.Name.Contains(q));

            if (sort == "lowToHigh")
                query = query.OrderBy(p => p.Price);
            else if (sort == "highToLow")
                query = query.OrderByDescending(p => p.Price);

            var totalItems = query.Count();

            var products = await query
                //Select(p=> new ProductDto { Name=p.Name,Price=p.Price,ImageUrl= $"{Request.Scheme}://{Request.Host}/StaticImages/{p.ImagePath}"})
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            // Map Product entities to ProductDto
            var productDtos = mapper.Map<List<ProductDto>>(products);

            // Set ImageUrl for each DTO
            foreach (var dto in productDtos)
            {
                dto.ImageUrl = $"{Request.Scheme}://{Request.Host}/StaticImages/{dto.ImagePath}";
            }
            //var model=  mapper.Map<ProductDto>(products);
            var result = new PagedResult<ProductDto>
            {
                Items = productDtos,
                PageNumber = page,
                PageSize = pageSize,
               // TotalItems = totalItems
            };
            return Ok(result);
        }

       
        [Authorize(Roles = "Admin")]
        [HttpGet("AdminProduct")]
        public async Task<ActionResult<PagedResult<Product>>> AdminProduct(string? q = "", string? sort = "", int page = 1, int pageSize = 5)
        {
            var query = dbContext.Product.AsQueryable();
            if (!string.IsNullOrEmpty(q))
                query = query.Where(p => p.Name.Contains(q));

            if (sort == "lowToHigh")
                query = query.OrderBy(p => p.Price);
            else if (sort == "highToLow")
                query = query.OrderByDescending(p => p.Price);

            var totalItems = query.Count();

            var products = await query
                //Select(p=> new ProductDto { Name=p.Name,Price=p.Price,ImageUrl= $"{Request.Scheme}://{Request.Host}/StaticImages/{p.ImagePath}"})
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            // Map Product entities to ProductDto
            var productDtos = mapper.Map<List<ProductDto>>(products);

            // Set ImageUrl for each DTO
            foreach (var dto in productDtos)
            {
                dto.ImageUrl = $"{Request.Scheme}://{Request.Host}/StaticImages/{dto.ImagePath}";
            }
            //var model=  mapper.Map<ProductDto>(products);
            var result = new PagedResult<ProductDto>
            {
                Items = productDtos,
                PageNumber = page,
                PageSize = pageSize,
                //TotalItems = totalItems
            };
            return Ok(result);
        }
        [HttpGet("Images")]
        public async Task<ActionResult> GetImages()
        {
            var images = await dbContext.Product.
                Select(p => new ProductDto
                {
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = $"{Request.Scheme}://{Request.Host}/StaticImages/{p.ImagePath}"
                }).ToListAsync();

            return Ok(images);
        }
        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<string>());

            var suggestions = await dbContext.Product
                .Where(p => p.Name.Contains(query))
                .OrderBy(p => p.Name)
                .Take(5)
                .Select(p => p.Name)
                .ToListAsync();

            return Ok(suggestions);
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromForm][Bind("Name,Price,Image")] ProductDto productdto)
        {
            try
            {
                // Check model validation errors first
                // if (!ModelState.IsValid)
                // {
                //var errors = ModelState
                //    .Where(kvp => kvp.Value.Errors.Any())
                //    .ToDictionary(
                //        kvp => kvp.Key,
                //        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                //    );

                //return BadRequest(new { errors });
                //   }

                // Ensure productdto is not null
                //if (productdto == null)
                //{
                //    return BadRequest(new { message = _localizer["ProductNull"].Value });
                //}
                if (!Directory.Exists("Img"))
                {
                    Directory.CreateDirectory("Img");
                }
                var filename = Guid.NewGuid().ToString().Substring(1, 8) + productdto.Image.FileName;
                var uploadfolder = Path.Combine(Directory.GetCurrentDirectory(), "Img");

                var filepath = Path.Combine(uploadfolder, filename);
                using (var filestream = new FileStream(filepath, FileMode.Create))
                {
                    productdto.Image.CopyTo(filestream);
                }
                productdto.ImagePath = filename;

                var product = new Product
                {
                    Name = productdto.Name,
                    Price = productdto.Price,
                    ImagePath = productdto.ImagePath,
                };

                await dbContext.Product.AddAsync(product);
                await dbContext.SaveChangesAsync();

               // var successMsg = _localizer["ProductAdd"].Value;
                return Ok(new { product, message = "message" 
                    //successMsg 
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "UnexpectedError" }
                   // _localizer["UnexpectedError"].Value }
                );
            }
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, ProductDto productdto)
        {
            //Console.WriteLine($"Raw JSON: {await new StreamReader(Request.Body).ReadToEndAsync()}");
            if (id == null)
                return BadRequest("Product not found");

            var product = new Product
            {
                Name = productdto.Name,
                Price = productdto.Price
            };
            var producttoUpdate = await dbContext.Product.Where(p => p.Id == id).FirstOrDefaultAsync();
            if (producttoUpdate != null)
            {
                producttoUpdate.Price = productdto.Price;
                producttoUpdate.Name = productdto.Name;
            }
            dbContext.Product.Update(producttoUpdate);
            await dbContext.SaveChangesAsync();

            return Ok(producttoUpdate); // ✅ return the updated product
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Product not found");
            var model = await dbContext.Product.Where(p => p.Id.Equals(id)).FirstOrDefaultAsync();

            dbContext.Product.Remove(model);
            await dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("price")]
        public IActionResult GetPrice()
        {
            var message = "";
               // _localizer["Price_Label"]; // auto-picks language
            return Ok(new { message });
        }

        [HttpGet("details/{id}")]
        public IActionResult GetProduct(int id)
        {
            var price = 123.45m;
            var formattedPrice = price.ToString("C", CultureInfo.CurrentCulture);
            var formattedDate = DateTime.Now.ToString("f", CultureInfo.CurrentCulture);

            return Ok(new
            {
                Price = formattedPrice,
                Date = formattedDate,
                Culture = CultureInfo.CurrentCulture.Name
            });
        }
    }
}
