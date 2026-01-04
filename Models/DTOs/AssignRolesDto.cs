using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models.DTOs
{
    public class AssignRolesDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public List<string> Roles { get; set; } = new();

    }
}
