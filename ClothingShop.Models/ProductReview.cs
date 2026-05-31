using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingShop.Models
{
    public class ProductReview
    {
        [Key]
        public int ReviewId { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        // ── THÊM LẠI USERID ĐỂ BIẾT AI ĐÁNH GIÁ ──
        [Required]
        public string UserId { get; set; } = string.Empty;
        // Nếu hệ thống của bạn có thực thể User/Customer, bạn có thể cấu hình thuộc tính điều hướng tại đây, 
        // còn nếu không thì lưu UserId dạng string dưới đây là đủ chặt chẽ rồi.

        // ── THÊM LẠI ORDERID ĐỂ XÁC THỰC MUA HÀNG (VERIFIED PURCHASE) ──
        [Required]
        public string OrderId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; } // Số sao (1 -> 5)

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}