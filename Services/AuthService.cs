using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecureApi.Configuration;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;
using System.Security.Claims;

namespace SecureApi.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IHttpContextAccessor contextAccessor,
        IEmailSender emailSender,
        IConfiguration configuration
        )
 
    {
        _contextAccessor = contextAccessor;
        _userManager = userManager;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _emailSender = emailSender;
        _configuration = configuration;
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
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed=false,
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

            // 👇 NEW: Send Confirmation Email Logic
            await SendConfirmationEmailInternalAsync(user);
            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);

            return new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "User registered successfully.",
                Data = null
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
            var adminUser = await _userManager.IsInRoleAsync(user, "Admin");
            if(adminUser )
            {
                user.EmailConfirmed = true;
               await _userManager.UpdateAsync(user);
            }
            if (user == null) return InvalidLogin();
            //{
            //    return new ApiResponse<AuthResponseDto>
            //    {
            //        Success = false,
            //        Message = "Invalid email or password.",
            //        Errors = new List<string> { "Authentication failed." }
            //    };
            //}
            // 👇 NEW: Check if Email is Confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Email not confirmed.",
                    Errors = new List<string> { "Please confirm your email before logging in." }
                };
            }
            //var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            //if (!isPasswordValid)
            //{
            //    return new ApiResponse<AuthResponseDto>
            //    {
            //        Success = false,
            //        Message = "Invalid email or password.",
            //        Errors = new List<string> { "Authentication failed." }
            //    };
            //}
            // 👇 NEW: Check if Email is Confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Email not confirmed.",
                    Errors = new List<string> { "Please confirm your email before logging in." }
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
    private ApiResponse<AuthResponseDto> InvalidLogin() =>
       new() { Success = false, Message = "Invalid email or password." };

    public async Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new ApiResponse<string> { Success = false, Message = "User not found." };

            // Decode token (It comes URL encoded from frontend usually, but Identity needs raw or sometimes base64)
            // Usually, standard Identity tokens just work, but if you base64 encoded it in the email, decode here.
            // Assuming standard string passing:

            // Fix for spaces becoming '+' in URLs if not handled correctly by frontend
            token = token.Replace(" ", "+");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Email confirmation failed.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            return new ApiResponse<string> { Success = true, Message = "Email confirmed successfully." };
        }
        catch (Exception ex)
        {
            return new ApiResponse<string> { Success = false, Message = "Error confirming email." };
        }
    }
    public async Task<ApiResponse<string>> ResendConfirmationEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return new ApiResponse<string> { Success = false, Message = "User not found." };

        if (await _userManager.IsEmailConfirmedAsync(user))
            return new ApiResponse<string> { Success = false, Message = "Email already confirmed." };

        await SendConfirmationEmailInternalAsync(user);

        return new ApiResponse<string> { Success = true, Message = "Confirmation email sent." };
    }

    // HELPER: Generates Token & Sends Email
    // ---------------------------------------------------------
    private async Task SendConfirmationEmailInternalAsync(ApplicationUser user)
    {
        // 1. Generate Token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // 2. Encode Token for URL safety
        // Important: Pass this to frontend as query param
        var encodedToken = System.Net.WebUtility.UrlEncode(token);

        // 3. Construct Frontend URL
        // Define "ClientUrl" in your appsettings.json (e.g., http://localhost:5173)
        var clientUrl = _configuration["ClientUrl"] ?? "http://localhost:5173";
        var callbackUrl = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

        // 4. Send Email
        var subject = "Confirm your email";
        var body = $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>Confirm Email</a>";

        await _emailSender.SendEmailAsync(user.Email, subject, body,CancellationToken.None);
    }
    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            // 1. Get the Refresh Token from the Cookie, NOT the DTO
            var refreshToken = _contextAccessor.HttpContext?.Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "No refresh token found in cookies.",
                    Errors = new List<string> { "Cookie missing" }
                };
            }

            // 2. Validate Access Token (We still need the expired access token from the body to get claims)
            var principal = _tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
            if (principal == null)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid access token.",
                };
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);

            // 3. Validate against DB
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid or expired refresh token."
                };
            }

            // 4. Generate New Tokens (This rotates the tokens and sets a NEW cookie)
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

    //private async Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, IList<string> roles)
    //{
    //    var accessToken = _tokenService.GenerateAccessToken(user, roles);
    //    var refreshToken = _tokenService.GenerateRefreshToken();

    //    user.RefreshToken = refreshToken;
    //    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
    //    await _userManager.UpdateAsync(user);

    //    return new AuthResponseDto
    //    {
    //        ActiveUser = _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name),
    //        AccessToken = accessToken,
    //        RefreshToken = refreshToken,
    //        ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
    //    };
    //}
    private async Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, IList<string> roles)
    {
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        // Use the method from TokenService (assuming it returns string)
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Calculate Expiry 
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        // 1. Update Database
        user.RefreshToken = refreshToken.Token;
        user.RefreshTokenExpiryTime = refreshTokenExpiry;
        await _userManager.UpdateAsync(user);

        // 2. Set HttpOnly Cookie
        SetRefreshTokenCookie(refreshToken.Token, refreshTokenExpiry);

        return new AuthResponseDto
        {
            ActiveUser = user.UserName, // Simpler to grab directly from user obj
            AccessToken = accessToken,

            // IMPORTANT: Return NULL or Empty string for RefreshToken in the JSON
            // The frontend should never see the RefreshToken in the response body anymore
            RefreshToken = null,

            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
        };
    }

    private void SetRefreshTokenCookie(string token, DateTime expires)
    {
        if (_contextAccessor.HttpContext == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // JavaScript cannot read this
            Expires = expires,
            Secure = true, // Must be true for production (HTTPS)
            SameSite = SameSiteMode.None, // Necessary for cross-site cookie if frontend/backend are on different ports
            // IsEssential = true // Optional: indicates this cookie is required for the app to function
        };

        _contextAccessor.HttpContext.Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
