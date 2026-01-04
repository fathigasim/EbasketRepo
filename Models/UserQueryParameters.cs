namespace SecureApi.Models
{
    public class UserQueryParameters
    {
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public bool? IsLockedOut { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;

    }
}
