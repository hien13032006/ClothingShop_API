namespace ClothingShop.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string UserId { get; set; } = string.Empty;

        // order_update | promotion | system
        public string Type { get; set; } = "system";

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? RelatedId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation — Customer nằm cùng namespace ClothingShop.Models
        public Customer? Customer { get; set; }
    }
}
