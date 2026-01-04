using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models.DTOs
{
    public class AdminChangePasswordDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;

    }
}
