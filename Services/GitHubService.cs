using MassTransit.Initializers;
using SecureApi.Models.DTOs;

namespace SecureApi.Services
{
    public class GitHubService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GitHubService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<GitHubResponseDto> GetRepoAsync(string repoName)
        {
            var client = _httpClientFactory.CreateClient("GitHubClient");
            var response = await client.GetAsync($"repos/fathigasim/{repoName}");

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GitHubResponseDto>();
        }

        public async Task<GitHubResponseDto> GetRepoResAsync(string repoName)
        {
            var client = _httpClientFactory.CreateClient("GitHubClient");
            var response = await client.GetFromJsonAsync<GitHubResponseDto>($"repos/fathigasim/{repoName}");
           
            //.Select(p=> new { p.Node_Id,p.Name,p.Full_Name});
            var result = new GitHubResponseDto
            {
                Node_Id = response.Node_Id,
                Name = response.Name,
                Full_Name = response.Full_Name,
            };
            //response.EnsureSuccessStatusCode();
            return  result;
        }
    }
}
