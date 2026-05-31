using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingShop.Models
{
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; } 

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = "Percent"; 

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm giá không được âm")]
        public decimal DiscountValue { get; set; } // % Giảm hoặc số tiền giảm cụ thể

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu không được âm")]
        public decimal MinOrderAmount { get; set; } // Điều kiện giá trị đơn hàng tối thiểu để áp dụng mã

        public decimal? MaxDiscountAmount { get; set; } // ✅ Thêm mới: Số tiền giảm tối đa (Rất quan trọng nếu dùng loại "Percent")

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Kiểm tra xem mã giảm giá có hợp lệ ở thời điểm hiện tại hay không
        /// </summary>
        public bool IsValid(decimal orderAmount)
        {
            if (!IsActive) return false;

            // ✅ Thay đổi 2: Sử dụng UtcNow để đồng bộ múi giờ với hệ thống lưu trữ Database đám mây hoặc Docker
            var now = DateTime.UtcNow;

            if (StartDate.HasValue && now < StartDate.Value) return false;
            if (EndDate.HasValue && now > EndDate.Value) return false;

            // ✅ Thay đổi 3: Kiểm tra luôn điều kiện giá trị đơn hàng tối thiểu ở đây
            if (orderAmount < MinOrderAmount) return false;

            return true;
        }

        /// <summary>
        /// Tính toán số tiền được giảm giá dựa trên giá trị đơn hàng
        /// </summary>
        public decimal CalcDiscount(decimal orderAmount)
        {
            if (DiscountType == "Percent")
            {
                var discount = Math.Round(orderAmount * DiscountValue / 100m, 2);

                // Nếu cấu hình số tiền giảm tối đa, tiến hành chặn trần số tiền được giảm
                if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
                {
                    return MaxDiscountAmount.Value;
                }
                return discount;
            }

            // Trường hợp giảm số tiền cố định (FixedAmount)
            return Math.Min(DiscountValue, orderAmount);
        }
    }
}