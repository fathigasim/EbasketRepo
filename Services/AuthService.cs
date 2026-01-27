//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.WebUtilities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;
//using SecureApi.Configuration;
//using SecureApi.Models;
//using SecureApi.Models.DTOs;
//using SecureApi.Services.Interfaces;
//using System.Security.Claims;
//using System.Text;

//namespace SecureApi.Services;

//public class AuthService : IAuthService
//{
//    private readonly UserManager<ApplicationUser> _userManager;
//    private readonly ITokenService _tokenService;
//    private readonly JwtSettings _jwtSettings;
//    private readonly ILogger<AuthService> _logger;
//    private readonly IHttpContextAccessor _contextAccessor;
//    private readonly IEmailSender _emailSender;
//    private readonly IConfiguration _configuration;
//    public AuthService(
//        UserManager<ApplicationUser> userManager,
//        ITokenService tokenService,
//        IOptions<JwtSettings> jwtSettings,
//        ILogger<AuthService> logger,
//        IHttpContextAccessor contextAccessor,
//        IEmailSender emailSender,
//        IConfiguration configuration
//        )

//    {
//        _contextAccessor = contextAccessor;
//        _userManager = userManager;
//        _tokenService = tokenService;
//        _jwtSettings = jwtSettings.Value;
//        _logger = logger;
//        _emailSender = emailSender;
//        _configuration = configuration;
//    }

//    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
//    {
//        try
//        {
//            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
//            if (existingUser != null)
//            {
//                return new ApiResponse<AuthResponseDto>
//                {
//                    Success = false,
//                    Message = "User with this email already exists.",
//                    Errors = new List<string> { "Email already registered." }
//                };
//            }

//            var user = new ApplicationUser
//            {
//                UserName = registerDto.UserName,
//                Email = registerDto.Email,
//                CreatedAt = DateTime.UtcNow,
//                EmailConfirmed=false,
//            };

//            var result = await _userManager.CreateAsync(user, registerDto.Password);

//            if (!result.Succeeded)
//            {
//                return new ApiResponse<AuthResponseDto>
//                {
//                    Success = false,
//                    Message = "User registration failed.",
//                    Errors = result.Errors.Select(e => e.Description).ToList()
//                };
//            }

//            // Assign default role
//            await _userManager.AddToRoleAsync(user, "User");

//            var roles = await _userManager.GetRolesAsync(user);
//            var authResponse = await GenerateTokensAsync(user, roles);

//            // 👇 NEW: Send Confirmation Email Logic
//            await SendConfirmationEmailInternalAsync(user);
//            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);

//            return new ApiResponse<AuthResponseDto>
//            {
//                Success = true,
//                Message = "User registered successfully.",
//                Data = null
//            };
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error during user registration for {Email}", registerDto.Email);
//            return new ApiResponse<AuthResponseDto>
//            {
//                Success = false,
//                Message = "An error occurred during registration.",
//                Errors = new List<string> { ex.Message }
//            };
//        }
//    }

//    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
//    {
//        try
//        {

//            var user = await _userManager.FindByEmailAsync(loginDto.Email);
//            var adminUser = await _userManager.IsInRoleAsync(user, "Admin");
//            if(adminUser )
//            {
//                user.EmailConfirmed = true;
//               await _userManager.UpdateAsync(user);
//            }
//            if (user == null) return InvalidLogin();
//            //{
//            //    return new ApiResponse<AuthResponseDto>
//            //    {
//            //        Success = false,
//            //        Message = "Invalid email or password.",
//            //        Errors = new List<string> { "Authentication failed." }
//            //    };
//            //}
//            // 👇 NEW: Check if Email is Confirmed
//            if (!await _userManager.IsEmailConfirmedAsync(user))
//            {
//                return new ApiResponse<AuthResponseDto>
//                {
//                    Success = false,
//                    Message = "Email not confirmed.",
//                    Errors = new List<string> { "Please confirm your email before logging in." }
//                };
//            }
//            //var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
//            //if (!isPasswordValid)
//            //{
//            //    return new ApiResponse<AuthResponseDto>
//            //    {
//            //        Success = false,
//            //        Message = "Invalid email or password.",
//            //        Errors = new List<string> { "Authentication failed." }
//            //    };
//            //}
//            // 👇 NEW: Check if Email is Confirmed
//            if (!await _userManager.IsEmailConfirmedAsync(user))
//            {
//                return new ApiResponse<AuthResponseDto>
//                {
//                    Success = false,
//                    Message = "Email not confirmed.",
//                    Errors = new List<string> { "Please confirm your email before logging in." }
//                };
//            }
//            var roles = await _userManager.GetRolesAsync(user);
//            var authResponse = await GenerateTokensAsync(user, roles);

//            _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

//            return new ApiResponse<AuthResponseDto>
//            {
//                Success = true,
//                Message = "Login successful.",
//                Data = authResponse
//            };
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
//            return new ApiResponse<AuthResponseDto>
//            {
//                Success = false,
//                Message = "An error occurred during login.",
//                Errors = new List<string> { ex.Message }
//            };
//        }
//    }
//    private ApiResponse<AuthResponseDto> InvalidLogin() =>
//       new() { Success = false, Message = "Invalid email or password." };

//    public async Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token)
//    {
//        try
//        {
//            var user = await _userManager.FindByIdAsync(userId);
//            if (user == null)
//                return new ApiResponse<string> { Success = false, Message = "User not found." };

//            // Decode token (It comes URL encoded from frontend usually, but Identity needs raw or sometimes base64)
//            // Usually, standard Identity tokens just work, but if you base64 encoded it in the email, decode here.
//            // Assuming standard string passing:

//            // Fix for spaces becoming '+' in URLs if not handled correctly by frontend
//            token = token.Replace(" ", "+");

//            var result = await _userManager.ConfirmEmailAsync(user, token);

//            if (!result.Succeeded)
//            {
//                return new ApiResponse<string>
//                {
//                    Success = false,
//                    Message = "Email confirmation failed.",
//                    Errors = result.Errors.Select(e => e.Description).ToList()
//                };
//            }

//            return new ApiResponse<string> { Success = true, Message = "Email confirmed successfully." };
//        }
//        catch (Exception ex)
//        {
//            return new ApiResponse<string> { Success = false, Message = "Error confirming email." };
//        }
//    }
//    public async Task<ApiResponse<string>> ResendConfirmationEmailAsync(string email)
//    {
//        var user = await _userManager.FindByEmailAsync(email);
//        if (user == null)
//            return new ApiResponse<string> { Success = false, Message = "User not found." };

//        if (await _userManager.IsEmailConfirmedAsync(user))
//            return new ApiResponse<string> { Success = false, Message = "Email already confirmed." };

//        await SendConfirmationEmailInternalAsync(user);

//        return new ApiResponse<string> { Success = true, Message = "Confirmation email sent." };
//    }

//    // HELPER: Generates Token & Sends Email
//    // ---------------------------------------------------------
//    private async Task SendConfirmationEmailInternalAsync(ApplicationUser user)
//    {
//        // 1. Generate Token
//        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

//        // 2. Encode Token for URL safety
//        // Important: Pass this to frontend as query param
//        var encodedToken = System.Net.WebUtility.UrlEncode(token);

//        // 3. Construct Frontend URL
//        // Define "ClientUrl" in your appsettings.json (e.g., http://localhost:5173)
//        var clientUrl = _configuration["ClientUrl"] ?? "http://localhost:5173";
//        var callbackUrl = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

//        // 4. Send Email
//        var subject = "Confirm your email";
//        var body = $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>Confirm Email</a>";

//        await _emailSender.SendEmailAsync(user.Email, subject, body,CancellationToken.None);
//    }
//    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
//    {
//        try
//        {
//            // 1. Get the Refresh Token from the Cookie, NOT the DTO
//            var refreshToken = _contextAccessor.HttpContext?.Request.Cookies["refreshToken"];

//            if (string.IsNullOrEmpty(refreshToken))
//            {
//                return new ApiResponse<AuthResponseDto>
//                {
//                    Success = false,
//                    Message = "No refresh token found in cookies.",
//                    Errors = new List<string> { "Cookie missing" }
//                };
//            }

//            // 2. Validate Access Token (We still need the expired access token from the body to get claims)
//            var principal = _tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
//            if (principal == null)
//            {
//                return new ApiResponse<AuthResponseDto>
//                {
//                    Success = false,
//                    Message = "Invalid access token.",
//                };
//            }

//            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
//            var user = await _userManager.FindByEmailAsync(email);

//            // 3. Validate against DB
//            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
//            {
//                return new ApiResponse<AuthResponseDto>
//                {
//                    Success = false,
//                    Message = "Invalid or expired refresh token."
//                };
//            }

//            // 4. Generate New Tokens (This rotates the tokens and sets a NEW cookie)
//            var roles = await _userManager.GetRolesAsync(user);
//            var authResponse = await GenerateTokensAsync(user, roles);

//            _logger.LogInformation("Token refreshed successfully for user {Email}", email);

//            return new ApiResponse<AuthResponseDto>
//            {
//                Success = true,
//                Message = "Token refreshed successfully.",
//                Data = authResponse
//            };
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error during token refresh");
//            return new ApiResponse<AuthResponseDto>
//            {
//                Success = false,
//                Message = "An error occurred during token refresh.",
//                Errors = new List<string> { ex.Message }
//            };
//        }
//    }
//    public async Task<ApiResponse<bool>> RevokeTokenAsync(string email)
//    {
//        try
//        {
//            var user = await _userManager.FindByEmailAsync(email);
//            if (user == null)
//            {
//                return new ApiResponse<bool>
//                {
//                    Success = false,
//                    Message = "User not found.",
//                    Errors = new List<string> { "Invalid email." }
//                };
//            }

//            user.RefreshToken = null;
//            user.RefreshTokenExpiryTime = null;
//            await _userManager.UpdateAsync(user);

//            _logger.LogInformation("Token revoked for user {Email}", email);

//            return new ApiResponse<bool>
//            {
//                Success = true,
//                Message = "Token revoked successfully.",
//                Data = true
//            };
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error revoking token for {Email}", email);
//            return new ApiResponse<bool>
//            {
//                Success = false,
//                Message = "An error occurred while revoking token.",
//                Errors = new List<string> { ex.Message }
//            };
//        }
//    }

//    //private async Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, IList<string> roles)
//    //{
//    //    var accessToken = _tokenService.GenerateAccessToken(user, roles);
//    //    var refreshToken = _tokenService.GenerateRefreshToken();

//    //    user.RefreshToken = refreshToken;
//    //    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
//    //    await _userManager.UpdateAsync(user);

//    //    return new AuthResponseDto
//    //    {
//    //        ActiveUser = _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name),
//    //        AccessToken = accessToken,
//    //        RefreshToken = refreshToken,
//    //        ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
//    //    };
//    //}
//    private async Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, IList<string> roles)
//    {
//        var accessToken = _tokenService.GenerateAccessToken(user, roles);

//        // Use the method from TokenService (assuming it returns string)
//        var refreshToken = _tokenService.GenerateRefreshToken();

//        // Calculate Expiry 
//        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

//        // 1. Update Database
//        user.RefreshToken = refreshToken.Token;
//        user.RefreshTokenExpiryTime = refreshTokenExpiry;
//        await _userManager.UpdateAsync(user);

//        // 2. Set HttpOnly Cookie
//        SetRefreshTokenCookie(refreshToken.Token, refreshTokenExpiry);

//        return new AuthResponseDto
//        {
//            ActiveUser = user.UserName, // Simpler to grab directly from user obj
//            AccessToken = accessToken,

//            // IMPORTANT: Return NULL or Empty string for RefreshToken in the JSON
//            // The frontend should never see the RefreshToken in the response body anymore
//            RefreshToken = null,

//            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
//        };
//    }

//    private void SetRefreshTokenCookie(string token, DateTime expires)
//    {
//        if (_contextAccessor.HttpContext == null) return;

//        var cookieOptions = new CookieOptions
//        {
//            HttpOnly = true, // JavaScript cannot read this
//            Expires = expires,
//            Secure = true, // Must be true for production (HTTPS)
//            SameSite = SameSiteMode.None, // Necessary for cross-site cookie if frontend/backend are on different ports
//            // IsEssential = true // Optional: indicates this cookie is required for the app to function
//        };

//        _contextAccessor.HttpContext.Response.Cookies.Append("refreshToken", token, cookieOptions);
//    }

//    #region Password Reset

//    public async Task<ApiResponse<bool>> ForgotPasswordAsync(string email)
//    {
//        var user = await _userManager.FindByEmailAsync(email);

//        // ⚠️ SECURITY: Don't reveal if email exists
//        if (user == null || !user.EmailConfirmed)
//        {
//            _logger.LogWarning("Password reset requested for non-existent/unconfirmed email: {Email}", email);
//            // Still return success to prevent email enumeration
//            return new ApiResponse<bool>
//            {
//                Success = true,
//                Message = "If an account with that email exists, a reset link has been sent.",
//                Data = true
//            };
//        }

//        // Generate password reset token
//        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
//        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

//        // Build reset link
//        var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:5173";
//        var resetLink = $"{frontendUrl}/reset-password?userId={user.Id}&token={encodedToken}";

//        // Send email
//        var emailBody = $@"
//            <h2>Reset Your Password</h2>
//            <p>You requested a password reset. Click the link below to reset your password:</p>
//            <p><a href='{resetLink}'>Reset Password</a></p>
//            <p>If the button doesn't work, copy and paste this link:</p>
//            <p>{resetLink}</p>
//            <p><strong>This link will expire in 1 hour.</strong></p>
//            <p>If you didn't request this, please ignore this email.</p>";

//        await _emailSender.SendEmailAsync(user.Email!, "Reset Your Password", emailBody,CancellationToken.None);

//        _logger.LogInformation("Password reset email sent to {Email}", email);

//        return new ApiResponse<bool>
//        {
//            Success = true,
//            Message = "Password reset link sent successfully.",
//            Data = true
//        };
//    }

//    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto dto)
//    {
//        var user = await _userManager.FindByIdAsync(dto.UserId);

//        if (user == null)
//        {
//            _logger.LogWarning("Password reset failed: User {UserId} not found", dto.UserId);
//            return new ApiResponse<bool>
//            {
//                Success = false,
//                Message = "Invalid reset link"
//            };
//        }

//        // Decode token
//        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));

//        // Reset password
//        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);

//        if (!result.Succeeded)
//        {
//            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
//            _logger.LogWarning(
//                "Password reset failed for {Email}: {Errors}",
//                user.Email,
//                errors);

//            return new ApiResponse<bool>
//            {
//                Success = false,
//                Message = "Password reset failed. The link may have expired.",
//                Errors = result.Errors.Select(e => e.Description).ToList()
//            };
//        }

//        _logger.LogInformation("Password reset successfully for {Email}", user.Email);

//        // Optional: Send confirmation email
//        await _emailSender.SendEmailAsync(
//            user.Email!,
//            "Password Changed",
//            "<h2>Your password has been changed</h2><p>If you didn't make this change, please contact support immediately.</p>",CancellationToken.None);

//        return new ApiResponse<bool>
//        {
//            Success = true,
//            Message = "Password reset successfully. You can now log in with your new password.",
//            Data = true
//        };
//    }

//    public async Task<ApiResponse<bool>> ValidateResetTokenAsync(string userId, string token)
//    {
//        var user = await _userManager.FindByIdAsync(userId);

//        if (user == null)
//        {
//            return new ApiResponse<bool>
//            {
//                Success = false,
//                Message = "Invalid reset link"
//            };
//        }

//        try
//        {
//            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

//            // Verify token is valid (doesn't actually reset password)
//            var isValid = await _userManager.VerifyUserTokenAsync(
//                user,
//                _userManager.Options.Tokens.PasswordResetTokenProvider,
//                "ResetPassword",
//                decodedToken);

//            if (!isValid)
//            {
//                return new ApiResponse<bool>
//                {
//                    Success = false,
//                    Message = "Reset link has expired or is invalid"
//                };
//            }

//            return new ApiResponse<bool>
//            {
//                Success = true,
//                Message = "Reset link is valid",
//                Data = true
//            };
//        }
//        catch
//        {
//            return new ApiResponse<bool>
//            {
//                Success = false,
//                Message = "Invalid reset link format"
//            };
//        }
//    }

//    #endregion
//}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecureApi.Configuration;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;
using System.Net;
using System.Security.Claims;
using System.Text;

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
        IConfiguration configuration)
    {
        _contextAccessor = contextAccessor;
        _userManager = userManager;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    #region Registration

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", registerDto.Email);
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "User with this email already exists.",
                    Errors = new List<string> { "Email already registered." }
                };
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false,
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("User registration failed for {Email}: {Errors}",
                    registerDto.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));

                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "User registration failed.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            // Send confirmation email
            await SendConfirmationEmailInternalAsync(user);

            _logger.LogInformation("User {Email} registered successfully. Confirmation email sent.", registerDto.Email);

            return new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "Registration successful. Please check your email to confirm your account.",
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
                Errors = new List<string> { "Registration failed. Please try again later." }
            };
        }
    }

    #endregion

    #region Login

    //public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    //{
    //    try
    //    {
    //        var user = await _userManager.FindByEmailAsync(loginDto.Email);

    //        // ✅ SECURITY: Don't reveal if email exists
    //        if (user == null)
    //        {
    //            _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
    //            return InvalidLogin();
    //        }

    //        // ✅ ADMIN BYPASS: Allow admins without email confirmation
    //        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
    //        if (isAdmin && !user.EmailConfirmed)
    //        {
    //            user.EmailConfirmed = true;
    //            await _userManager.UpdateAsync(user);
    //            _logger.LogInformation("Admin user {Email} email auto-confirmed", user.Email);
    //        }

    //        // Check password BEFORE email confirmation check
    //        var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
    //        if (!isPasswordValid)
    //        {
    //            _logger.LogWarning("Failed login attempt for {Email}: Invalid password", loginDto.Email);
    //            return InvalidLogin();
    //        }

    //        // Check if email is confirmed
    //        if (!user.EmailConfirmed)
    //        {
    //            _logger.LogWarning("Login attempt with unconfirmed email: {Email}", loginDto.Email);
    //            return new ApiResponse<AuthResponseDto>
    //            {
    //                Success = false,
    //                Message = "Email not confirmed.",
    //                Errors = new List<string> { "Please confirm your email before logging in." }
    //            };
    //        }

    //        // ✅ Generate tokens and set HttpOnly cookie
    //        var roles = await _userManager.GetRolesAsync(user);
    //        var authResponse = await GenerateTokensAsync(user, roles);

    //        _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

    //        return new ApiResponse<AuthResponseDto>
    //        {
    //            Success = true,
    //            Message = "Login successful.",
    //            Data = authResponse
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
    //        return new ApiResponse<AuthResponseDto>
    //        {
    //            Success = false,
    //            Message = "An error occurred during login.",
    //            Errors = new List<string> { "Login failed. Please try again later." }
    //        };
    //    }
    //}

    private ApiResponse<AuthResponseDto> InvalidLogin() =>
        new()
        {
            Success = false,
            Message = "Invalid email or password.",
            Errors = new List<string> { "Authentication failed." }
        };
    //for single session enforcement 
    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
                return InvalidLogin();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Failed login attempt for {Email}", loginDto.Email);
                return InvalidLogin();
            }

            if (!user.EmailConfirmed)
            {
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Email not confirmed.",
                    Errors = new List<string> { "Please confirm your email before logging in." }
                };
            }

            // ✅ NEW: Single Session Enforcement - Revoke previous session
            var newSessionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(user.CurrentSessionId))
            {
                _logger.LogWarning(
                    "User {Email} logged in from new location. Previous session {SessionId} invalidated.",
                    user.Email,
                    user.CurrentSessionId);
            }

            user.CurrentSessionId = newSessionId;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = _contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            await _userManager.UpdateAsync(user);

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var authResponse = await GenerateTokensAsync(user, roles, newSessionId);

            _logger.LogInformation("User {Email} logged in successfully with session {SessionId}",
                loginDto.Email, newSessionId);

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
                Errors = new List<string> { "Login failed. Please try again later." }
            };
        }
    }

    #endregion

    #region Email Confirmation

    public async Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Email confirmation failed: User {UserId} not found", userId);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Invalid confirmation link."
                };
            }

            // Already confirmed?
            if (user.EmailConfirmed)
            {
                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Email already confirmed."
                };
            }

            // Fix URL encoding issues (spaces become '+')
            token = token.Replace(" ", "+");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Email confirmation failed for {Email}: {Errors}",
                    user.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));

                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Email confirmation failed. The link may have expired.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            _logger.LogInformation("Email confirmed successfully for {Email}", user.Email);

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Email confirmed successfully. You can now log in."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for userId {UserId}", userId);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Error confirming email. Please try again or request a new confirmation link."
            };
        }
    }

    public async Task<ApiResponse<string>> ResendConfirmationEmailAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);

            // ✅ SECURITY: Don't reveal if email exists
            if (user == null)
            {
                _logger.LogWarning("Confirmation email resend requested for non-existent email: {Email}", email);
                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "If an account exists with that email, a confirmation link has been sent."
                };
            }

            if (user.EmailConfirmed)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Email already confirmed."
                };
            }

            await SendConfirmationEmailInternalAsync(user);

            _logger.LogInformation("Confirmation email resent to {Email}", email);

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Confirmation email sent."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending confirmation email to {Email}", email);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Error sending confirmation email."
            };
        }
    }

    private async Task SendConfirmationEmailInternalAsync(ApplicationUser user)
    {
        // Generate token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // URL encode for safety
        var encodedToken = WebUtility.UrlEncode(token);

        // Build frontend URL
        var clientUrl = _configuration["Frontend:FrontendUrl"] ?? "http://localhost:5173";
        var callbackUrl = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

        // Send email
        var subject = "Confirm Your Email Address";
        var body = $@"
            <h2>Welcome to Our Platform!</h2>
            <p>Please confirm your email address by clicking the link below:</p>
            <p><a href='{callbackUrl}' style='padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;'>Confirm Email</a></p>
            <p>Or copy and paste this link into your browser:</p>
            <p>{callbackUrl}</p>
            <p><strong>This link will expire in 24 hours.</strong></p>
            <p>If you didn't create an account, please ignore this email.</p>";

        await _emailSender.SendEmailAsync(user.Email!, subject, body, CancellationToken.None);
    }

    #endregion

    #region Token Refresh
    // Services/AuthService.cs
    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = _contextAccessor.HttpContext?.Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Refresh token request without cookie");
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Refresh token not found."
                };
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null)
            {
                _logger.LogWarning("Invalid refresh token provided");
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid refresh token."
                };
            }

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Expired refresh token for user {Email}", user.Email);
                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Refresh token has expired. Please log in again."
                };
            }

            // ✅ NEW: Validate session is still active
            // If sessionId is null, it means the user logged in elsewhere
            if (string.IsNullOrEmpty(user.CurrentSessionId))
            {
                _logger.LogWarning(
                    "Session invalidated for user {Email} - logged in elsewhere",
                    user.Email);

                return new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Your session has been invalidated. Please log in again."
                };
            }

            // Generate NEW tokens with SAME session ID (session continues)
            var roles = await _userManager.GetRolesAsync(user);
            var authResponse = await GenerateTokensAsync(user, roles, user.CurrentSessionId);

            _logger.LogInformation("Token refreshed for user {Email} session {SessionId}",
                user.Email, user.CurrentSessionId);

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
                Message = "An error occurred during token refresh."
            };
        }
    }
    //private async Task<AuthResponseDto> GenerateTokensAsync(
    //ApplicationUser user,
    //IList<string> roles,
    //string sessionId) // ✅ NEW parameter
    //{
    //    // Generate access token WITH sessionId claim
    //    var accessToken = _tokenService.GenerateAccessToken(user, roles, sessionId);
    //    var refreshTokenObj = _tokenService.GenerateRefreshToken();
    //    var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

    //    // Update database
    //    user.RefreshToken = refreshTokenObj.Token;
    //    user.RefreshTokenExpiryTime = refreshTokenExpiry;
    //    user.CurrentSessionId = sessionId; // Store session ID
    //    await _userManager.UpdateAsync(user);

    //    // Set HttpOnly cookie
    //    SetRefreshTokenCookie(refreshTokenObj.Token, refreshTokenExpiry);

    //    return new AuthResponseDto
    //    {
    //        ActiveUser = user.UserName,
    //        AccessToken = accessToken,
    //        RefreshToken = null,
    //        ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
    //    };
    //}
    //public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync()
    //{
    //    try
    //    {
    //        // ✅ CRITICAL FIX: Get refresh token from HttpOnly cookie ONLY
    //        var refreshToken = _contextAccessor.HttpContext?.Request.Cookies["refreshToken"];

    //        if (string.IsNullOrEmpty(refreshToken))
    //        {
    //            _logger.LogWarning("Refresh token request without cookie");
    //            return new ApiResponse<AuthResponseDto>
    //            {
    //                Success = false,
    //                Message = "Refresh token not found.",
    //                Errors = new List<string> { "No refresh token provided." }
    //            };
    //        }

    //        // ✅ SECURITY FIX: Validate refresh token from database FIRST
    //        // Don't trust the expired access token until we verify the refresh token
    //        var user = await _userManager.Users
    //            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

    //        if (user == null)
    //        {
    //            _logger.LogWarning("Invalid refresh token provided");
    //            return new ApiResponse<AuthResponseDto>
    //            {
    //                Success = false,
    //                Message = "Invalid refresh token."
    //            };
    //        }

    //        // Check expiry
    //        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
    //        {
    //            _logger.LogWarning("Expired refresh token for user {Email}", user.Email);
    //            return new ApiResponse<AuthResponseDto>
    //            {
    //                Success = false,
    //                Message = "Refresh token has expired. Please log in again."
    //            };
    //        }

    //        // ✅ OPTIONAL: Validate the expired access token matches the user
    //        // This adds an extra layer of security
    //        if (!string.IsNullOrEmpty(refreshTokenDto.AccessToken))
    //        {
    //            var principal = _tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
    //            var emailFromToken = principal?.FindFirst(ClaimTypes.Email)?.Value;

    //            if (emailFromToken != user.Email)
    //            {
    //                _logger.LogWarning("Token mismatch: Access token email doesn't match refresh token user");
    //                return new ApiResponse<AuthResponseDto>
    //                {
    //                    Success = false,
    //                    Message = "Token validation failed."
    //                };
    //            }
    //        }

    //        // ✅ Generate NEW tokens (this rotates the refresh token)
    //        var roles = await _userManager.GetRolesAsync(user);
    //        var authResponse = await GenerateTokensAsync(user, roles);

    //        _logger.LogInformation("Token refreshed successfully for user {Email}", user.Email);

    //        return new ApiResponse<AuthResponseDto>
    //        {
    //            Success = true,
    //            Message = "Token refreshed successfully.",
    //            Data = authResponse
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error during token refresh");
    //        return new ApiResponse<AuthResponseDto>
    //        {
    //            Success = false,
    //            Message = "An error occurred during token refresh.",
    //            Errors = new List<string> { "Token refresh failed." }
    //        };
    //    }
    //}

    #endregion

    #region Token Revocation

    public async Task<ApiResponse<bool>> RevokeTokenAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Token revocation failed: User {Email} not found", email);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            // Clear refresh token AND session
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.CurrentSessionId = null; // ✅ NEW: Clear session ID

            await _userManager.UpdateAsync(user);

            ClearRefreshTokenCookie();

            _logger.LogInformation("Token revoked and session cleared for user {Email}", email);

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
                Message = "Token revocation failed."
            };
        }
    }

    #endregion

    #region Password Reset

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);

            // ✅ SECURITY: Don't reveal if email exists
            if (user == null || !user.EmailConfirmed)
            {
                _logger.LogWarning("Password reset requested for non-existent/unconfirmed email: {Email}", email);

                // Still return success to prevent email enumeration
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "If an account with that email exists, a reset link has been sent.",
                    Data = true
                };
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Build reset link
            var frontendUrl = _configuration["Frontend:FrontendUrl"] ?? "http://localhost:5173";
            var resetLink = $"{frontendUrl}/reset-password?userId={user.Id}&token={encodedToken}";

            // Send email
            var emailBody = $@"
                <h2>Reset Your Password</h2>
                <p>You requested a password reset. Click the link below to reset your password:</p>
                <p><a href='{resetLink}' style='padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>Or copy and paste this link:</p>
                <p>{resetLink}</p>
                <p><strong>This link will expire in 1 hour.</strong></p>
                <p>If you didn't request this, please ignore this email and your password will remain unchanged.</p>";

            await _emailSender.SendEmailAsync(user.Email!, "Reset Your Password", emailBody, CancellationToken.None);

            _logger.LogInformation("Password reset email sent to {Email}", email);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "If an account with that email exists, a reset link has been sent.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", email);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Error processing password reset request.",
                Errors = new List<string> { "Please try again later." }
            };
        }
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null)
            {
                _logger.LogWarning("Password reset failed: User {UserId} not found", dto.UserId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Invalid reset link."
                };
            }

            // Decode token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));

            // Reset password
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Password reset failed for {Email}: {Errors}", user.Email, errors);

                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Password reset failed. The link may have expired.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            _logger.LogInformation("Password reset successfully for {Email}", user.Email);

            // ✅ SECURITY: Revoke all refresh tokens after password reset
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);

            // Send confirmation email
            await _emailSender.SendEmailAsync(
                user.Email!,
                "Password Changed Successfully",
                @"<h2>Your password has been changed</h2>
                  <p>Your password was recently changed. If you didn't make this change, please contact support immediately.</p>
                  <p>For security reasons, you'll need to log in again with your new password.</p>",
                CancellationToken.None);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Password reset successfully. You can now log in with your new password.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for userId {UserId}", dto.UserId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Error resetting password.",
                Errors = new List<string> { "Please try again or request a new reset link." }
            };
        }
    }

    public async Task<ApiResponse<bool>> ValidateResetTokenAsync(string userId, string token)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Invalid reset link."
                };
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            // Verify token is valid
            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                "ResetPassword",
                decodedToken);

            if (!isValid)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Reset link has expired or is invalid."
                };
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Reset link is valid.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset token for userId {UserId}", userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Invalid reset link format."
            };
        }
    }

    #endregion

    #region Token Generation & Cookie Helpers

    private async Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, IList<string> roles,
            string sessionId // ✅ NEW parameter
        )
    {
        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user, roles,sessionId);
        var refreshTokenObj = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        // ✅ Update database (token rotation)
        user.RefreshToken = refreshTokenObj.Token;
        user.RefreshTokenExpiryTime = refreshTokenExpiry;
        await _userManager.UpdateAsync(user);

        // ✅ Set HttpOnly cookie
        SetRefreshTokenCookie(refreshTokenObj.Token, refreshTokenExpiry);

        return new AuthResponseDto
        {
            ActiveUser = user.UserName,
            AccessToken = accessToken,
            RefreshToken = null, // ✅ NEVER return refresh token in response body
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
        };
    }

    private void SetRefreshTokenCookie(string token, DateTime expires)
    {
        if (_contextAccessor.HttpContext == null)
        {
            _logger.LogWarning("Cannot set refresh token cookie: HttpContext is null");
            return;
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,      // ✅ JavaScript cannot access
            Expires = expires,
            Secure = true,        // ✅ HTTPS only in production
            SameSite = SameSiteMode.None, // ✅ Required for cross-origin (adjust based on your setup)
            Path = "/",           // ✅ Available to entire app
            // ✅ PRODUCTION: Consider adding Domain if needed
            // Domain = ".yourdomain.com"
        };

        _contextAccessor.HttpContext.Response.Cookies.Append("refreshToken", token, cookieOptions);

        _logger.LogDebug("Refresh token cookie set (expires: {Expiry})", expires);
    }

    private void ClearRefreshTokenCookie()
    {
        if (_contextAccessor.HttpContext == null) return;

        _contextAccessor.HttpContext.Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });

        _logger.LogDebug("Refresh token cookie cleared");
    }

    #endregion
}