using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using SecureApi.Services.Interfaces;

namespace SecureApi.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        #region User CRUD Operations

        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var userDto = await MapToUserDto(user);
                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User retrieved successfully.",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", userId);
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the user.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid email address." }
                    };
                }

                var userDto = await MapToUserDto(user);
                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User retrieved successfully.",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email {Email}", email);
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the user.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<PagedResult<UserDto>>> GetAllUsersAsync(UserQueryParameters parameters)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
                {
                    query = query.Where(u =>
                        u.UserName!.Contains(parameters.SearchTerm) ||
                        u.Email!.Contains(parameters.SearchTerm));
                }

                // Apply lockout filter
                if (parameters.IsLockedOut.HasValue)
                {
                    if (parameters.IsLockedOut.Value)
                    {
                        query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow);
                    }
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting
                query = parameters.SortBy.ToLower() switch
                {
                    "username" => parameters.SortDescending
                        ? query.OrderByDescending(u => u.UserName)
                        : query.OrderBy(u => u.UserName),
                    "email" => parameters.SortDescending
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email),
                    "createdat" => parameters.SortDescending
                        ? query.OrderByDescending(u => u.CreatedAt)
                        : query.OrderBy(u => u.CreatedAt),
                    _ => query.OrderByDescending(u => u.CreatedAt)
                };

                // Apply pagination
                var users = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                var userDtos = new List<UserDto>();
                foreach (var user in users)
                {
                    userDtos.Add(await MapToUserDto(user));
                }

                // Apply role filter after mapping (since roles are not in the main query)
                if (!string.IsNullOrWhiteSpace(parameters.Role))
                {
                    userDtos = userDtos.Where(u => u.Roles.Contains(parameters.Role)).ToList();
                }

                var pagedResult = new PagedResult<UserDto>
                {
                    Items = userDtos,
                    TotalCount = totalCount,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize
                };

                return new ApiResponse<PagedResult<UserDto>>
                {
                    Success = true,
                    Message = "Users retrieved successfully.",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return new ApiResponse<PagedResult<UserDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving users.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
                if (existingUser != null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User with this email already exists.",
                        Errors = new List<string> { "Email already registered." }
                    };
                }

                var user = new ApplicationUser
                {
                    UserName = createUserDto.UserName,
                    Email = createUserDto.Email,
                    PhoneNumber = createUserDto.PhoneNumber,
                    EmailConfirmed = true, // Admin-created users have confirmed emails
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, createUserDto.Password);
                if (!result.Succeeded)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User creation failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                // Assign roles
                if (createUserDto.Roles.Any())
                {
                    await _userManager.AddToRolesAsync(user, createUserDto.Roles);
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                var userDto = await MapToUserDto(user);
                _logger.LogInformation("User {Email} created by admin", createUserDto.Email);

                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User created successfully.",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Email}", createUserDto.Email);
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the user.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateUserAsync(string userId, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                user.UserName = updateUserDto.UserName;
                user.PhoneNumber = updateUserDto.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User update failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                var userDto = await MapToUserDto(user);
                _logger.LogInformation("User {UserId} updated", userId);

                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User updated successfully.",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the user.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User deletion failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("User {UserId} deleted", userId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "User deleted successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the user.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion

        #region Password Management

        public async Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Password change failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("Password changed for user {UserId}", userId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Password changed successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while changing the password.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> AdminChangePasswordAsync(AdminChangePasswordDto adminChangePasswordDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(adminChangePasswordDto.UserId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, adminChangePasswordDto.NewPassword);

                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Password reset failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("Password reset by admin for user {UserId}", adminChangePasswordDto.UserId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Password reset successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", adminChangePasswordDto.UserId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while resetting the password.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // Don't reveal that the user doesn't exist
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "If the email exists, a password reset link has been sent.",
                        Data = true
                    };
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // TODO: Send email with reset token
                // await _emailService.SendPasswordResetEmailAsync(user.Email, token);

                _logger.LogInformation("Password reset requested for user {Email}", email);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "If the email exists, a password reset link has been sent.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for {Email}", email);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while processing the request.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion

        #region Role Management

        public async Task<ApiResponse<List<string>>> GetUserRolesAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<List<string>>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var roles = await _userManager.GetRolesAsync(user);

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "User roles retrieved successfully.",
                    Data = roles.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user {UserId}", userId);
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user roles.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> AssignRolesToUserAsync(AssignRolesDto assignRolesDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(assignRolesDto.UserId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                // Remove existing roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                // Add new roles
                var result = await _userManager.AddToRolesAsync(user, assignRolesDto.Roles);
                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Role assignment failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("Roles assigned to user {UserId}", assignRolesDto.UserId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Roles assigned successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning roles to user {UserId}", assignRolesDto.UserId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while assigning roles.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> RemoveRoleFromUserAsync(string userId, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var result = await _userManager.RemoveFromRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Role removal failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("Role {Role} removed from user {UserId}", role, userId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Role removed successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while removing the role.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<List<string>>> GetAllRolesAsync()
        {
            try
            {
                var roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "Roles retrieved successfully.",
                    Data = roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all roles");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving roles.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion

        #region Account Management

        public async Task<ApiResponse<bool>> LockUserAsync(LockoutUserDto lockoutUserDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(lockoutUserDto.UserId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var lockoutEnd = lockoutUserDto.LockoutEnd ?? DateTimeOffset.MaxValue;
                var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User lockout failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("User {UserId} locked out until {LockoutEnd}", lockoutUserDto.UserId, lockoutEnd);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "User locked successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user {UserId}", lockoutUserDto.UserId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while locking the user.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> UnlockUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var result = await _userManager.SetLockoutEndDateAsync(user, null);
                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User unlock failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                // Reset access failed count
                await _userManager.ResetAccessFailedCountAsync(user);

                _logger.LogInformation("User {UserId} unlocked", userId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "User unlocked successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while unlocking the user.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> ConfirmEmailAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Email confirmation failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("Email confirmed for user {UserId}", userId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Email confirmed successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while confirming the email.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> EnableTwoFactorAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Two-factor authentication enable failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("Two-factor authentication enabled for user {UserId}", userId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Two-factor authentication enabled successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling two-factor authentication for user {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while enabling two-factor authentication.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> DisableTwoFactorAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "Invalid user ID." }
                    };
                }

                var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
                if (!result.Succeeded)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Two-factor authentication disable failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("Two-factor authentication disabled for user {UserId}", userId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Two-factor authentication disabled successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling two-factor authentication for user {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while disabling two-factor authentication.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion

        #region Statistics

        public async Task<ApiResponse<UserStatisticsDto>> GetUserStatisticsAsync()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var lockedOutUsers = await _context.Users
                    .Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow)
                    .CountAsync();
                var usersWithTwoFactor = await _context.Users
                    .Where(u => u.TwoFactorEnabled)
                    .CountAsync();

                var usersByRole = new Dictionary<string, int>();
                var roles = await _roleManager.Roles.ToListAsync();

                foreach (var role in roles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                    usersByRole[role.Name!] = usersInRole.Count;
                }

                var statistics = new UserStatisticsDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = totalUsers - lockedOutUsers,
                    LockedOutUsers = lockedOutUsers,
                    UsersWithTwoFactor = usersWithTwoFactor,
                    UsersByRole = usersByRole
                };

                return new ApiResponse<UserStatisticsDto>
                {
                    Success = true,
                    Message = "Statistics retrieved successfully.",
                    Data = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user statistics");
                return new ApiResponse<UserStatisticsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving statistics.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion

        #region Helper Methods

        private async Task<UserDto> MapToUserDto(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt
            };
        }

    }
    #endregion
}

