using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecureApi.Configuration;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;

namespace SecureApi.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "User with this email already exists.",
                    Errors = new List<string> { "Email already registered." }
                };
            }

            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "User registration failed.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            var roles = await _userManager.GetRolesAsync(user);
            var authResponse = await GenerateTokensAsync(user, roles);

            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);

            return new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "User registered successfully.",
                Data = authResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {Email}", registerDto.Email);
            return new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "An error occurred during registration.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid email or password.",
                    Errors = new List<string> { "Authentication failed." }
                };
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid email or password.",
                    Errors = new List<string> { "Authentication failed." }
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var authResponse = await GenerateTokensAsync(user, roles);

            _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

            return new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "Login successful.",
                Data = authResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
            return new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "An error occurred during login.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
            if (principal == null)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid access token.",
                    Errors = new List<string> { "Token validation failed." }
                };
            }

            var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid token claims.",
                    Errors = new List<string> { "Email claim not found." }
                };
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.RefreshToken != refreshTokenDto.RefreshToken || 
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid or expired refresh token.",
                    Errors = new List<string> { "Refresh token validation failed." }
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var authResponse = await GenerateTokensAsync(user, roles);

            _logger.LogInformation("Token refreshed successfully for user {Email}", email);

            return new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "Token refreshed successfully.",
                Data = authResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "An error occurred during token refresh.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<bool>> RevokeTokenAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "User not found.",
                    Errors = new List<string> { "Invalid email." }
                };
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Token revoked for user {Email}", email);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Token revoked successfully.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token for {Email}", email);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while revoking token.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, IList<string> roles)
    {
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
        };
    }
}
