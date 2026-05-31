using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingShop.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; } // ✅ Thay đổi 1: Sử dụng Id làm Khóa chính (Primary Key) riêng biệt

        [Required]
        public string UserId { get; set; } = string.Empty; // ✅ Định danh người sở hữu giỏ hàng

        public int VariantId { get; set; } // ✅ Khóa ngoại liên kết tới biến thể sản phẩm cụ thể (Size, Color, Stock)

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 1")]
        public int Quantity { get; set; } // ✅ Số lượng sản phẩm khách chọn mua

        public DateTime AddedAt { get; set; } = DateTime.UtcNow; // ✅ Sử dụng UtcNow để đồng bộ thời gian hệ thống

        // ── Navigation Properties (Thuộc tính điều hướng để truy vấn với LinQ/Include) ───

        [ForeignKey(nameof(UserId))]
        public Customer? Customer { get; set; }

        [ForeignKey(nameof(VariantId))]
        public ProductVariant? Variant { get; set; } // ✅ Giúp lấy thông tin Size, Color, Stock của Variant từ Database
    }
}