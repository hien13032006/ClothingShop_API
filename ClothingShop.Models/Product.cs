using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingShop.Models
{
    public class Product
    {

        [Key]
        public int ProductId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? Discount { get; set; }
        public string MainImage { get; set; } = string.Empty;
        public List<string> Thumbnails { get; set; } = new List<string>();
        public string Gender { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Policy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int SoldCount { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

        [NotMapped]
        public double AverageRating => Reviews.Any() ? Math.Round(Reviews.Average(r => r.Rating), 1) : 5.0;

        [NotMapped]
        public int ReviewCount => Reviews.Count;
    }
}
