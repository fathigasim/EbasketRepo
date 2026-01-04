using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models.DTOs
{
    public class CreateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public List<string> Roles { get; set; } = new();

    }
}
