namespace ClothingShop.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public string? ReplacedByToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation — Customer cùng namespace
        public Customer? Customer { get; set; }

        public bool IsActive => !IsRevoked && DateTime.Now < ExpiresAt;
    }
}
