using System;

namespace ClothingShop.Models
{
    public class OrderTracking
    {
        public int TrackingId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string? StatusUpdate { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string? LocationLatLong { get; set; }

        public Order? Order { get; set; }
    }
}
