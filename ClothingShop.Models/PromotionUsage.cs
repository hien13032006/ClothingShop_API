using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingShop.Models
{
    public class PromotionUsage
    {
        [Key]
        public int UsageId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;// ID người dùng

        [Required]
        public int PromotionId { get; set; } // ID khuyến mãi

        [Required]
        public string OrderId { get; set; } = string.Empty;// ID đơn hàng liên quan

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (Tùy chọn để truy vấn dữ liệu dễ hơn)
        public virtual Promotion? Promotion { get; set; }
    }
}