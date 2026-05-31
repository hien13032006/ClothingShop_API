using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingShop.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public string Color { get; set; } = string.Empty; // Ví dụ: Trắng
        public string Size { get; set; } = string.Empty;  // Ví dụ: M
        public int StockQuantity { get; set; } // Số lượng còn lại trong kho (Ví dụ: 12)
    }
}
