namespace ClothingShop.API.DTOs.Auth
{
    /// <summary>
    /// DTO for Google OAuth login request
    /// </summary>
    public class GoogleLoginDto
    {
        /// <summary>
        /// Google ID Token received from frontend
        /// </summary>
        public required string IdToken { get; set; }
    }

    /// <summary>
    /// Response DTO for Google login
    /// </summary>
    public class GoogleLoginResponseDto
    {
        public required string Token { get; set; }
        public int UserId { get; set; }
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsNewUser { get; set; }
    }
}
