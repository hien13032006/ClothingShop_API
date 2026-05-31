using System;

namespace ClothingShop.Models
{
    public class PointHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public int Points { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Customer? Customer { get; set; }
    }
}
