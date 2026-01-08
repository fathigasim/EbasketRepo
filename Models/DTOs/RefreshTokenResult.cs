namespace SecureApi.Models.DTOs
{
    public class RefreshTokenResult
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
    }
}
