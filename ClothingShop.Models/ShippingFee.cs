namespace ClothingShop.Models
{
    public class ShippingFee
    {
        public int Id { get; set; }
        public string Method { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public decimal FreeShippingFrom { get; set; }
        public string? EstimatedDays { get; set; }
    }
}
