namespace ClothingShop.Models
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public Order? Order { get; set; }
        public ProductVariant? Variant { get; set; }
    }
}
