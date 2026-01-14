using Microsoft.AspNetCore.Identity;

namespace SecureApi.Models;

public class ApplicationUser : IdentityUser
{
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    //  NEW: Session tracking
    public string? CurrentSessionId { get; set; } // New login = new GUID
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    //public DateTime? DateOfBirth { get; set; }
    // Navigation to orders
    //public ICollection<Order> Orders { get; set; } = new List<Order>();
}
