using SecureApi.Models.DTOs;

namespace SecureApi.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);
    //Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync();
    Task<ApiResponse<bool>> RevokeTokenAsync(string email);
    Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token);
    Task<ApiResponse<string>> ResendConfirmationEmailAsync(string email);

    // Password Reset
    Task<ApiResponse<bool>> ForgotPasswordAsync(string email);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto dto);
    Task<ApiResponse<bool>> ValidateResetTokenAsync(string userId, string token);
}
