using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;
using System.Security.Claims;

namespace SecureApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("global")]

public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _authService = authService;
        _logger = logger;
        _userManager = userManager;
    }

    #region Registration & Login

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "Invalid input.",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _authService.RegisterAsync(registerDto);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// Sets HttpOnly cookie with refresh token
    /// Returns access token in response body
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "Invalid input.",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _authService.LoginAsync(loginDto);

        if (!result.Success)
            return Unauthorized(result);

        _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

        return Ok(result);
    }

    #endregion

    #region Token Management

    /// <summary>
    /// ✅ UPDATED: Refresh access token using HttpOnly cookie
    /// NO REQUEST BODY NEEDED - Refresh token comes from cookie
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        // ✅ NO DTO PARAMETER - Refresh token comes from HttpOnly cookie
        var result = await _authService.RefreshTokenAsync();

        if (!result.Success)
        {
            _logger.LogWarning("Token refresh failed: {Message}", result.Message);
            return Unauthorized(result);
        }

        _logger.LogInformation("Token refreshed successfully");

        return Ok(result);
    }

    /// <summary>
    /// ✅ UPDATED: Revoke refresh token (Logout)
    /// Requires valid access token in Authorization header
    /// Clears HttpOnly cookie and database refresh token
    /// </summary>
    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken()
    {
        // Get user email from access token claims
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Revoke token failed: No email claim in token");
            return Unauthorized(new ApiResponse<bool>
            {
                Success = false,
                Message = "Invalid token."
            });
        }

        var result = await _authService.RevokeTokenAsync(email);

        if (!result.Success)
        {
            _logger.LogWarning("Token revocation failed for {Email}: {Message}", email, result.Message);
            return BadRequest(result);
        }

        _logger.LogInformation("Token revoked for user {Email}", email);

        return Ok(result);
    }

    #endregion

    #region User Information

    /// <summary>
    /// Get current authenticated user's information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "User information retrieved successfully.",
            Data = new
            {
                UserId = userId,
                Email = email,
                UserName = userName,
                Roles = roles
            }
        });
    }

    #endregion

    #region Account Management

    /// <summary>
    /// Delete user account (requires authorization)
    /// </summary>
    [HttpDelete("delete-account")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteDto deleteDto)
    {
        // ✅ SECURITY: Ensure user can only delete their own account
        var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(currentEmail))
        {
            return Unauthorized(new ApiResponse<bool>
            {
                Success = false,
                Message = "Invalid token."
            });
        }

        // Prevent deleting other users' accounts
        if (currentEmail != deleteDto.Email)
        {
            _logger.LogWarning(
                "User {CurrentEmail} attempted to delete account {TargetEmail}",
                currentEmail,
                deleteDto.Email);

            return Forbid();
        }

        var user = await _userManager.FindByEmailAsync(deleteDto.Email);

        if (user == null)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "User not found."
            });
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to delete account.",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        _logger.LogInformation("User account deleted: {Email}", deleteDto.Email);

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Account deleted successfully.",
            Data = true
        });
    }

    #endregion

    #region Email Confirmation

    /// <summary>
    /// Confirm user email address
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string userId,
        [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid confirmation link."
            });
        }

        var result = await _authService.ConfirmEmailAsync(userId, token);

        if (!result.Success)
        {
            _logger.LogWarning("Email confirmation failed for userId {UserId}", userId);
            return BadRequest(result);
        }

        _logger.LogInformation("Email confirmed successfully for userId {UserId}", userId);

        return Ok(result);
    }

    /// <summary>
    /// Resend email confirmation link
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Email is required."
            });
        }

        var result = await _authService.ResendConfirmationEmailAsync(dto.Email);

        // ✅ SECURITY: Always return success to prevent email enumeration
        return Ok(result);
    }

    #endregion

    #region Password Reset

    /// <summary>
    /// Step 1: User requests password reset link
    /// Sends email with reset token
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Invalid email address."
            });
        }

        var result = await _authService.ForgotPasswordAsync(dto.Email);

        // ✅ SECURITY: Always return success to prevent email enumeration
        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "If an account with that email exists, a password reset link has been sent.",
            Data = true
        });
    }

    /// <summary>
    /// Step 2: User submits new password with reset token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Invalid input.",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _authService.ResetPasswordAsync(dto);

        if (!result.Success)
        {
            _logger.LogWarning("Password reset failed for userId {UserId}", dto.UserId);
            return BadRequest(result);
        }

        _logger.LogInformation("Password reset successfully for userId {UserId}", dto.UserId);

        return Ok(result);
    }

    /// <summary>
    /// Validate password reset token before showing reset form
    /// </summary>
    [HttpGet("validate-reset-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateResetToken(
        [FromQuery] string userId,
        [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Invalid reset link."
            });
        }

        var result = await _authService.ValidateResetTokenAsync(userId, token);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    #endregion
}