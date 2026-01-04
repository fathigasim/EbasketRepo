using SecureApi.Models;
using SecureApi.Models.DTOs;

namespace SecureApi.Services.Interfaces
{
    public interface IUserManagementService
    {
        // User CRUD operations
        Task<ApiResponse<UserDto>> GetUserByIdAsync(string userId);
        Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email);
        Task<ApiResponse<PagedResult<UserDto>>> GetAllUsersAsync(UserQueryParameters parameters);
        Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
        Task<ApiResponse<UserDto>> UpdateUserAsync(string userId, UpdateUserDto updateUserDto);
        Task<ApiResponse<bool>> DeleteUserAsync(string userId);

        // Password management
        Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<ApiResponse<bool>> AdminChangePasswordAsync(AdminChangePasswordDto adminChangePasswordDto);
        Task<ApiResponse<bool>> ResetPasswordAsync(string email);

        // Role management
        Task<ApiResponse<List<string>>> GetUserRolesAsync(string userId);
        Task<ApiResponse<bool>> AssignRolesToUserAsync(AssignRolesDto assignRolesDto);
        Task<ApiResponse<bool>> RemoveRoleFromUserAsync(string userId, string role);
        Task<ApiResponse<List<string>>> GetAllRolesAsync();

        // Account management
        Task<ApiResponse<bool>> LockUserAsync(LockoutUserDto lockoutUserDto);
        Task<ApiResponse<bool>> UnlockUserAsync(string userId);
        Task<ApiResponse<bool>> ConfirmEmailAsync(string userId);
        Task<ApiResponse<bool>> EnableTwoFactorAsync(string userId);
        Task<ApiResponse<bool>> DisableTwoFactorAsync(string userId);

        // Statistics
        Task<ApiResponse<UserStatisticsDto>> GetUserStatisticsAsync();
    }
}
