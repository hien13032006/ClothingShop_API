using System;

namespace ClothingShop.Models
{
    public class Address
    {
        public int AddressId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string AddressType { get; set; } = "Nhà riêng";
        public bool IsDefault { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
    }
}
