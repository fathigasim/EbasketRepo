using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;
using System.Security.Claims;

namespace SecureApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<AuthResponseDto> { Success = false, Message = "Invalid input." });

        // AuthService handles generating the Token AND the HttpOnly Cookie
        var result = await _authService.RegisterAsync(registerDto);

        if (!result.Success) return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<AuthResponseDto> { Success = false, Message = "Invalid input." });

        // AuthService handles generating the Token AND the HttpOnly Cookie
        var result = await _authService.LoginAsync(loginDto);

        if (!result.Success) return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        // NOTE: Ensure your RefreshTokenDto does NOT require the "RefreshToken" string property.
        // The React Frontend only sends { accessToken: "..." } in the body.
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<AuthResponseDto>
            { Success = false, Message = "Invalid input." });

        // AuthService will ignore the DTO's refresh token and read the HTTP Cookie instead
        var result = await _authService.RefreshTokenAsync(refreshTokenDto);

        if (!result.Success) return Unauthorized(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("revoke-token")] // Used for Logout
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken()
    {
        // 1. Get user identity from the Access Token
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
            return Unauthorized(new ApiResponse<bool> { Success = false, Message = "Invalid token." });

        // 2. Call Service to clear DB Refresh Token AND delete the Browser Cookie
        var result = await _authService.RevokeTokenAsync(email);

        if (!result.Success) return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "User information retrieved successfully.",
            Data = new
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                UserName = User.FindFirst(ClaimTypes.Name)?.Value,
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            }
        });
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> DeleteUser([FromBody] DeleteDto deleteDto)
    {
        // Optional: Ensure the user deleting the account is the one logged in
        // var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        // if(currentEmail != deleteDto.Email) return Forbid();

        var user = await _userManager.FindByEmailAsync(deleteDto.Email);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
            return Ok(new ApiResponse<bool> { Success = true, Message = "User deleted" });
        }
        return BadRequest(new ApiResponse<bool> { Success = false, Message = "User not found" });
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous] // Use this if checking via browser directly or frontend
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return BadRequest("Invalid parameters");

        var result = await _authService.ConfirmEmailAsync(userId, token);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation([FromBody] string email)
    {
        var result = await _authService.ResendConfirmationEmailAsync(email);
        return Ok(result);
    }
}