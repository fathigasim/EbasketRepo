using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {

        public TestController()
        {

        }
        [HttpGet]
        public async Task<IActionResult> Data()
        {
            var data = new List<string> { "one", "two", "three" };
            return Ok(data);
        }
    }
}