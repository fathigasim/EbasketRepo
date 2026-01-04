namespace SecureApi.Models.DTOs
{
    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedOutUsers { get; set; }
        public int UsersWithTwoFactor { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new();

    }
}
