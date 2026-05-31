using System;
using System.Collections.Generic;

namespace ClothingShop.Models
{
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public string? ShippingMethod { get; set; }
        public string? PaymentMethod { get; set; }
        public string Status { get; set; } = "Chờ xác nhận";

        public Customer? Customer { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<OrderTracking> Trackings { get; set; } = new List<OrderTracking>();
        public Payment? Payment { get; set; }
    }
}
