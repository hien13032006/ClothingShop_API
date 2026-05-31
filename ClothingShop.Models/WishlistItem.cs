using System;

namespace ClothingShop.Models
{
    public class WishlistItem
    {
        public int WishlistId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;

        public Customer? Customer { get; set; }
        public Product? Product { get; set; }
    }
}
