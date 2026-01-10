using SecureApi.Models;
using SecureApi.Models.DTOs;
using System.Security.Claims;

namespace SecureApi.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles,string sessionId);
    //string GenerateRefreshToken();
    RefreshTokenResult GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
