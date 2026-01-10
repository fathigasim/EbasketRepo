using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecureApi.Configuration;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SecureApi.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }
    // Services/TokenService.cs
    public string GenerateAccessToken(
        ApplicationUser user,
        IList<string> roles,
        string sessionId) // ✅ NEW parameter
    {
        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.UserName!),
        new(ClaimTypes.Email, user.Email!),
        new("sessionId", sessionId), // ✅ NEW: Session tracking
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    //public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    //{
    //    var claims = new List<Claim>
    //    {
    //        new(ClaimTypes.NameIdentifier, user.Id),
    //        new(ClaimTypes.Email, user.Email!),
    //        new(ClaimTypes.Name, user.UserName!),
    //        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    //    };

    //    // Add roles to claims
    //    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    //    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    //    var token = new JwtSecurityToken(
    //        issuer: _jwtSettings.Issuer,
    //        audience: _jwtSettings.Audience,
    //        claims: claims,
    //        expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
    //        signingCredentials: credentials
    //    );

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}

    //public string GenerateRefreshToken()
    //{
    //    var randomNumber = new byte[64];
    //    using var rng = RandomNumberGenerator.Create();
    //    rng.GetBytes(randomNumber);
    //    return Convert.ToBase64String(randomNumber);
    //}
    public RefreshTokenResult GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return new RefreshTokenResult
        {
            Token = Convert.ToBase64String(randomNumber),
            Expires = DateTime.UtcNow.AddDays(7) // Set your refresh token lifetime here
        };
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Don't validate lifetime for expired tokens
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    
}
