using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models.DTOs
{
    public class UpdateUserDto
    {
        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

    }
}
