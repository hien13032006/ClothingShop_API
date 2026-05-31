using System;

namespace ClothingShop.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Thanh toán khi nhận hàng";
        public string PaymentStatus { get; set; } = "Chưa thanh toán";
        public string? TransactionId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal AmountPaid { get; set; }

        public Order? Order { get; set; }
    }
}
