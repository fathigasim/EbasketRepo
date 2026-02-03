using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;
using System.Globalization;
using System.Reflection;

namespace SecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        IProductService _productService;
        IMapper mapper;
        IStringLocalizer<ProductsController> _localizer;
        public ProductsController(ApplicationDbContext _dbContext, IMapper _mapper
            , IProductService productService, IStringLocalizer<ProductsController> localizer
          )
        {
            _productService = productService;
            mapper = _mapper;
           _localizer = localizer;  
        }
       // [Authorize(Roles = "Admin,User")]
        //[Authorize]
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Get(string? q = "",string?category="" ,string? sort = "", int page = 1, int pageSize = 5)
        {
            var model=await _productService.Get(q,category ,sort, page, pageSize);
            return Ok(model);
        }

       
        //[Authorize(Roles = "Admin")]
        //[HttpGet("AdminProduct")]
        //public async Task<ActionResult<PagedResult<Product>>> AdminProduct(string? q = "", string? sort = "", int page = 1, int pageSize = 5)
        //{
        //    var query = dbContext.Product.AsQueryable();
        //    if (!string.IsNullOrEmpty(q))
        //        query = query.Where(p => p.Name.Contains(q));

        //    if (sort == "lowToHigh")
        //        query = query.OrderBy(p => p.Price);
        //    else if (sort == "highToLow")
        //        query = query.OrderByDescending(p => p.Price);

        //    var totalItems = query.Count();

        //    var products = await query
        //        //Select(p=> new ProductDto { Name=p.Name,Price=p.Price,ImageUrl= $"{Request.Scheme}://{Request.Host}/StaticImages/{p.ImagePath}"})
        //        .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        //    // Map Product entities to ProductDto
        //    var productDtos = mapper.Map<List<ProductDto>>(products);

        //    // Set ImageUrl for each DTO
        //    foreach (var dto in productDtos)
        //    {
        //        dto.ImageUrl = $"{Request.Scheme}://{Request.Host}/StaticImages/{dto.ImagePath}";
        //    }
        //    //var model=  mapper.Map<ProductDto>(products);
        //    var result = new PagedResult<ProductDto>
        //    {
        //        Items = productDtos,
        //        PageNumber = page,
        //        PageSize = pageSize,
        //        //TotalItems = totalItems
        //    };
        //    return Ok(result);
        //}
        //[HttpGet("Images")]
        //public async Task<ActionResult> GetImages()
        //{
        //    var images = await dbContext.Product.
        //        Select(p => new ProductDto
        //        {
        //            Name = p.Name,
        //            Price = p.Price,
        //            ImageUrl = $"{Request.Scheme}://{Request.Host}/StaticImages/{p.ImagePath}"
        //        }).ToListAsync();

        //    return Ok(images);
        //}
        //[HttpGet("suggest")]
        //public async Task<IActionResult> Suggest(string query)
        //{
        //    if (string.IsNullOrWhiteSpace(query))
        //        return Ok(new List<string>());

        //    var suggestions = await dbContext.Product
        //        .Where(p => p.Name.Contains(query))
        //        .OrderBy(p => p.Name)
        //        .Take(5)
        //        .Select(p => p.Name)
        //        .ToListAsync();

        //    return Ok(suggestions);
        //}


        [HttpPost]
        public async Task<IActionResult> Post([FromForm][Bind("Name,Price,Image,Stock,CategoryId")] ProductDto productdto)
        {
            try
            {

              await  _productService.PostProudct(productdto);
              var successMsg = _localizer["ProductAdd"].Value;
                return Ok(new { message =  $"{productdto.Name} "+ $"{successMsg}" });
            }
            catch (Exception)
            {
                return BadRequest( new { message = "Some went wrong" }
                   // _localizer["UnexpectedError"].Value }
                );
            }
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, ProductDto productdto)
        {
            try
            {
                await _productService.UpdateProductAsync(id, productdto);
                return Ok(new {message=$"{productdto.Name} Updated Successfully"});
            }
            catch { 
              return BadRequest(new { message = "Some went wrong" });
            }
            
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
           var result= await _productService.DeleteAsync(id);
            return Ok(result);
        }

        //[HttpGet("price")]
        //public IActionResult GetPrice()
        //{
        //    var message = "";
        //       // _localizer["Price_Label"]; // auto-picks language
        //    return Ok(new { message });
        //}

        //[HttpGet("details/{id}")]
        //public IActionResult GetProduct(int id)
        //{
        //    var price = 123.45m;
        //    var formattedPrice = price.ToString("C", CultureInfo.CurrentCulture);
        //    var formattedDate = DateTime.Now.ToString("f", CultureInfo.CurrentCulture);

        //    return Ok(new
        //    {
        //        Price = formattedPrice,
        //        Date = formattedDate,
        //        Culture = CultureInfo.CurrentCulture.Name
        //    });
        //}
    }
}
