using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SecureApi.Data;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        ApplicationDbContext dbContext;
        public ReportsController(ApplicationDbContext _dbContext)
        {
            dbContext= _dbContext;
        }
        // GET: api/<ReportsController>
        [HttpGet("TotalByProduct")]
        public async Task<IActionResult> Get()
        {
          var productTotal=   await dbContext.Product.GroupBy(p => p.Name)
                .Select(g => new
                {
                    ProductName = g.Key,
                    TotalSales = g.Sum(p => p.Price)
                }).ToListAsync();
            return Ok(productTotal);
        }

        // GET api/<ReportsController>/5
        [HttpGet("TotalByOrderProductOrderItem")]
        public async Task<IActionResult> GetProductOrdered()
        {
            var model = await dbContext.OrderItems.GroupBy(o => o.ProductId)
                .Select(g => new
                {

                    OrderId=g.Select(o=>o.OrderId).FirstOrDefault(),
                    ProductId = g.Key,
                    user=g.Select(o=>o.Order.User.UserName).FirstOrDefault(),
                    product =g.Select(o=>o.Product.Name).FirstOrDefault(),
                    TotalAmount = g.Sum(o => o.Quantity * o.Product.Price)
                }).AsNoTracking().OrderByDescending(p=>p.TotalAmount).Take(10)
                .ToListAsync();

            return Ok(model);
        }

        // POST api/<ReportsController>
        [HttpGet("ProductCategory")]
        public async Task<IActionResult> GetProductGategory()
        {
           var model=await   dbContext.Category.GroupJoin(dbContext.Product, c => c.Id, p => p.CategoryId, 
               (c, p) => new {c,p }).SelectMany(p => p.p.DefaultIfEmpty(), (c, p) =>new { 
                   category= c.c.Name,
                   product= p != null ? p.Name : "No Product"
               }).AsNoTracking().ToListAsync();
             
            return Ok(model);
        }

        // PUT api/<ReportsController>/5
        [HttpGet("QuarterTotals")]
        public async Task<IActionResult> GetQuarterTotals()
        {
            var quarterlySales = await dbContext.Order
    .GroupBy(o => new
    {
        Year = o.CreatedAt.Year,
        Quarter = ((o.CreatedAt.Month - 1) / 3) + 1
    })
    .Select(g => new
    {
        Year = g.Key.Year,
        Quarter = g.Key.Quarter,
        TotalSales = g.Select(x => x.OrderItems.Sum(p=>p.Price * p.Quantity))
    })
    .OrderBy(x => x.Year)
    .ThenBy(x => x.Quarter)
    .ToListAsync();
            var grandTotal = quarterlySales.Select(x => x.TotalSales.Sum());
            return Ok(new { quarterlySales,grandTotal });
        }

        // DELETE api/<ReportsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
