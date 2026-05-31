using System;
using System.Collections.Generic;

namespace ClothingShop.Models
{
    public class Customer
    {
        public string UserId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Provider { get; set; } = "Local";
        public string MembershipLevel { get; set; } = "Bạc";
        public int TotalPoints { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
