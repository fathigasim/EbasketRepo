using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models.DTOs
{
    public class LockoutUserDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTimeOffset? LockoutEnd { get; set; }

    }
}
