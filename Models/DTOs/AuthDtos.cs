using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage ="EmailRequired")]
    [EmailAddress(ErrorMessage ="NotValidEmail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage ="PassRequired")]
    [StringLength(100, MinimumLength = 6,ErrorMessage = "NotValidPassword")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password", ErrorMessage = "NotIdenticalPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required (ErrorMessage = "UserRequired")]
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenDto
{
    //[Required]
    //public string AccessToken { get; set; } = string.Empty;

    //[Required]
    //public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string? ActiveUser { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}
