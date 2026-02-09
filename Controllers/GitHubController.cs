using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureApi.Services;

namespace SecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GitHubController : ControllerBase
    {
        GitHubService hubService;
        public GitHubController(GitHubService _hubService)
        {
            hubService = _hubService;
        }

        [HttpGet("{name}")]
        public async Task <IActionResult> Get(string name) {
          var result=  await hubService.GetRepoAsync(name);
            return Ok(result);
        }

        [HttpGet("Info/{name}")]
        public async Task<IActionResult> GetInfo(string name)
        {
            var result = await hubService.GetRepoResAsync(name);
            return Ok(result);
        }
    }
}
