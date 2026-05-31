using System;

namespace ClothingShop.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }

        public Customer? Customer { get; set; }
        public bool IsValid()
        {
            return !IsUsed && DateTime.Now < ExpiresAt;
        }
    }
}
