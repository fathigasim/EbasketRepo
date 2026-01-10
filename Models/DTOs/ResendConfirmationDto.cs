using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models.DTOs
{
    public class ResendConfirmationDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }
}
