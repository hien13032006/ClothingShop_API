using System.ComponentModel.DataAnnotations;

namespace ClothingShop.Models.DTOs
{
    // ═══════════════════════════════════════════════════════
    // AUTH
    // ═══════════════════════════════════════════════════════
    public class RegisterDto
    {
        [Required][EmailAddress][MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required][MinLength(8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&]).{8,}$",
            ErrorMessage = "Mật khẩu phải có chữ, số và ký tự đặc biệt")]
        public string Password { get; set; } = string.Empty;

        [Required][Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "SĐT phải 10 số, bắt đầu bằng 0")]
        public string? Phone { get; set; }

        [MaxLength(100)] public string? FullName { get; set; }
    }

    public class LoginDto
    {
        [Required] public string EmailOrPhone { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginDto
    {
        [Required] public string IdToken { get; set; } = string.Empty;
    }

    public class LoginResultDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MembershipLevel { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;   // NEW
        public DateTime TokenExpiry { get; set; }                   // NEW
    }

    public class RefreshTokenDto
    {
        [Required] public string RefreshToken { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════
    // FORGOT PASSWORD
    // ═══════════════════════════════════════════════════════
    public class ForgotPasswordDto
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpDto
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required][StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải đúng 6 chữ số")]
        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required] public string ResetToken { get; set; } = string.Empty;

        [Required][MinLength(8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&]).{8,}$",
            ErrorMessage = "Mật khẩu phải có chữ, số và ký tự đặc biệt")]
        public string NewPassword { get; set; } = string.Empty;

        [Required][Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════
    // CUSTOMER / PROFILE
    // ═══════════════════════════════════════════════════════
    public class CustomerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string MembershipLevel { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [MaxLength(100)] public string? FullName { get; set; }
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "SĐT không đúng định dạng")]
        public string? Phone { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required] public string CurrentPassword { get; set; } = string.Empty;
        [Required][MinLength(8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&]).{8,}$",
            ErrorMessage = "Mật khẩu phải có chữ, số và ký tự đặc biệt")]
        public string NewPassword { get; set; } = string.Empty;
        [Required][Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════
    // ADDRESS
    // ═══════════════════════════════════════════════════════
    public class AddressDto
    {
        public int AddressId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string AddressType { get; set; } = "Nhà riêng";
        public bool IsDefault { get; set; }
    }

    public class CreateAddressDto
    {
        [Required][MaxLength(255)] public string ReceiverName { get; set; } = string.Empty;
        [Required][RegularExpression(@"^0\d{9}$", ErrorMessage = "SĐT không đúng định dạng")]
        public string ReceiverPhone { get; set; } = string.Empty;
        [Required] public string AddressDetail { get; set; } = string.Empty;
        [RegularExpression("^(Nhà riêng|Cơ quan)$")] public string AddressType { get; set; } = "Nhà riêng";
        public bool IsDefault { get; set; } = false;
    }

    // ═══════════════════════════════════════════════════════
    // PRODUCT
    // ═══════════════════════════════════════════════════════
    public class ProductSummaryDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;

        // ĐỔI TỪ BasePrice THÀNH Price
        public decimal Price { get; set; }
        public int? Discount { get; set; }

        // ĐỔI TỪ ImageUrl THÀNH MainImage
        public string? MainImage { get; set; }
        public string? Category { get; set; }
        public string? Gender { get; set; }
        public bool IsActive { get; set; }
        public int SoldCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsInWishlist { get; set; }
    }

    public class ProductDetailDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Policy { get; set; }

        // ĐỔI TỪ BasePrice THÀNH Price
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int? Discount { get; set; }

        // ĐỔI TỪ ImageUrl THÀNH MainImage
        public string? MainImage { get; set; }
        public List<string> Thumbnails { get; set; } = new List<string>();
        public string? Category { get; set; }
        public string? Gender { get; set; }
        public int SoldCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsInWishlist { get; set; }
        public List<VariantDto> Variants { get; set; } = new();
        public List<ReviewDto> RecentReviews { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    public class ProductImageDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public int SortOrder { get; set; }
    }

    public class VariantDto
    {
        public int VariantId { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int StockQuantity { get; set; }

    }

    public class CreateProductDto
    {
        [Required][MaxLength(255)] public string Name { get; set; } = string.Empty;
        [Required] public string Category { get; set; } = string.Empty; // Lưu trực tiếp dạng chuỗi, bỏ CategoryId
        [Required] public string Gender { get; set; } = string.Empty;   // Thêm trường bắt buộc
        public int? Discount { get; set; }                             // Thêm trường giảm giá
        public string? Description { get; set; }
        public string? Policy { get; set; }
        [Required][Range(0.01, double.MaxValue)] public decimal BasePrice { get; set; }
        public decimal? OldPrice { get; set; }
        [MaxLength(500)] public string? ImageUrl { get; set; }
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 biến thể")]
        public List<CreateVariantDto> Variants { get; set; } = new();
    }

    public class CreateVariantDto
    {
        [MaxLength(50)] public string? Color { get; set; }
        [MaxLength(20)] public string? Size { get; set; }
        [Range(0, int.MaxValue)] public int StockQuantity { get; set; }
    }

    public class UpdateVariantDto
    {
        [MaxLength(50)] public string? Color { get; set; }
        [MaxLength(20)] public string? Size { get; set; }
        [Range(0, int.MaxValue)] public int? StockQuantity { get; set; }
        [Range(0, double.MaxValue)] public decimal? PriceAdjustment { get; set; }
    }

    public class UpdateProductDto
    {
        [MaxLength(255)] public string? Name { get; set; }
        public string? Description { get; set; }
        [Range(0.01, double.MaxValue)] public decimal? BasePrice { get; set; }
        public decimal? OldPrice { get; set; }
        public int? Discount { get; set; }
        [MaxLength(500)] public string? ImageUrl { get; set; }
        public string? Category { get; set; } // Đổi kiểu dữ liệu từ int? sang string?
        public string? Gender { get; set; }   // Thêm trường cập nhật giới tính
        public bool? IsActive { get; set; }
    }

    public class ProductFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? Category { get; set; } // Đổi từ int? CategoryId sang string? Category
        public string? Gender { get; set; }   // Thêm bộ lọc theo Giới tính
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string SortBy { get; set; } = "newest";
    }

    // ═══════════════════════════════════════════════════════
    // REVIEW
    // ═══════════════════════════════════════════════════════
    public class CreateReviewDto
    {
        [Required] public int ProductId { get; set; }
        [Required] public string OrderId { get; set; } = string.Empty; // Front-end truyền từ chi tiết đơn hàng sang
        [Required][Range(1, 5)] public int Rating { get; set; }
        [MaxLength(1000)] public string? Comment { get; set; }
    }

    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsVerifiedPurchase { get; set; } = true; // Luôn trả về true vì bắt buộc mua mới được đánh giá
        public DateTime CreatedAt { get; set; }
    }

    public class ProductRatingSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════
    // CART
    // ═══════════════════════════════════════════════════════
    public class AddToCartDto
    {
        [Required][Range(1, int.MaxValue)] public int VariantId { get; set; }
        [Required][Range(1, 99)] public int Quantity { get; set; }
    }

    public class UpdateCartDto
    {
        [Required] public int CartId { get; set; }
        [Required][Range(1, 99)] public int Quantity { get; set; }
    }

    public class CartItemDto
    {
        public int CartId { get; set; }
        public int VariantId { get; set; }
        public string? ProductName { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public int StockQuantity { get; set; }
    }

    public class CartSummaryDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
    }

    // ═══════════════════════════════════════════════════════
    // ORDER
    // ═══════════════════════════════════════════════════════
    public class CreateOrderDto
    {
        // Cho phép AddressId là null nếu muốn nhập tay
        public int? AddressId { get; set; }

        // Thông tin địa chỉ mới (nếu không chọn AddressId)
        public string? NewReceiverName { get; set; }
        public string? NewReceiverPhone { get; set; }
        public string? NewAddressDetail { get; set; }

        [Required]
        [RegularExpression("^(Giao hàng nhanh|Tiêu chuẩn)$")]
        public string ShippingMethod { get; set; } = "Tiêu chuẩn";

        [Required]
        [RegularExpression("^(Thanh toán khi nhận hàng|Thanh toán online)$")]
        public string PaymentMethod { get; set; } = "Thanh toán khi nhận hàng";

        [MaxLength(50)] public string? PromotionCode { get; set; }
        [Required][MinLength(1)] public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        [Range(1, int.MaxValue)] public int VariantId { get; set; }
        [Range(1, 99)] public int Quantity { get; set; }
    }

    public class OrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public string? ShippingMethod { get; set; }
        public string? PaymentMethod { get; set; }
        public string Status { get; set; } = string.Empty;
        public PaymentInfoDto? Payment { get; set; }
        public List<OrderDetailDto> Details { get; set; } = new();
        public List<TrackingDto> Trackings { get; set; } = new();
    }

    public class PaymentInfoDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public decimal AmountPaid { get; set; }
    }

    public class OrderDetailDto
    {
        public int OrderDetailId { get; set; }
        public int VariantId { get; set; }
        public string? ProductName { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool CanReview { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        [RegularExpression("^(Chờ xác nhận|Đang chuẩn bị|Đang giao|Hoàn thành|Hủy)$")]
        public string Status { get; set; } = string.Empty;
        [MaxLength(255)] public string? StatusNote { get; set; }
        [MaxLength(100)] public string? LocationLatLong { get; set; }
    }

    public class TrackingDto
    {
        public string? StatusUpdate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? LocationLatLong { get; set; }
    }

    public class OrderFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Status { get; set; }
        public string? Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class OrderCalculationDto
    {
        public List<CalculatedItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; } // Bạn có thể thêm sau
        public decimal DiscountAmount { get; set; }
        public decimal FinalTotal { get; set; }
    }

    public class CalculatedItemDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? ImageUrl { get; set; }
    }

    // ═══════════════════════════════════════════════════════
    // PROMOTION
    // ═══════════════════════════════════════════════════════
    public class ApplyPromoDto
    {
        [Required][MaxLength(50)] public string Code { get; set; } = string.Empty;
        [Range(0.01, double.MaxValue)] public decimal OrderAmount { get; set; }
    }

    public class PromoResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public class CreatePromotionDto
    {
        [Required][MaxLength(50)][RegularExpression(@"^[A-Za-z0-9]+$")]
        public string Code { get; set; } = string.Empty;
        [Required][RegularExpression("^(Percent|Fixed)$")]
        public string DiscountType { get; set; } = "Percent";
        [Range(0.01, double.MaxValue)] public decimal DiscountValue { get; set; }
        [Range(0, double.MaxValue)] public decimal MinOrderAmount { get; set; } = 0;
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    // ═══════════════════════════════════════════════════════
    // PAYMENT
    // ═══════════════════════════════════════════════════════
    public class PaymentCallbackDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════
    // SHIPPING
    // ═══════════════════════════════════════════════════════
    public class ShippingOptionDto
    {
        public string Method { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public decimal OriginalFee { get; set; }
        public decimal FreeShippingFrom { get; set; }
        public string EstimatedDays { get; set; } = string.Empty;
        public bool IsFree { get; set; }
    }

    public class UpsertShippingFeeDto
    {
        [Required] public string Method { get; set; } = string.Empty;
        [Range(0, double.MaxValue)] public decimal Fee { get; set; }
        [Range(0, double.MaxValue)] public decimal FreeShippingFrom { get; set; } = 0;
        [Required][MaxLength(50)] public string EstimatedDays { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════
    // UPLOAD
    // ═══════════════════════════════════════════════════════
    public class UploadResultDto
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }

    // ═══════════════════════════════════════════════════════
    // MEMBERSHIP & POINTS
    // ═══════════════════════════════════════════════════════
    public class MembershipInfoDto
    {
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string MembershipLevel { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public decimal DiscountRate { get; set; }
        public string? NextLevel { get; set; }
        public int PointsToNextLevel { get; set; }
        public List<string> Benefits { get; set; } = new();
    }

    public class PointHistoryDto
    {
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MembershipBenefitsDto
    {
        public List<LevelBenefitDto> Levels { get; set; } = new();
        public int PointsPerAmount { get; set; }
        public int AmountPerPoint { get; set; }
    }

    public class LevelBenefitDto
    {
        public string Level { get; set; } = string.Empty;
        public int MinPoints { get; set; }
        public int MaxPoints { get; set; }
        public decimal DiscountRate { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════
    // NOTIFICATIONS
    // ═══════════════════════════════════════════════════════
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? RelatedId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationSummaryDto
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int UnreadCount { get; set; }
    }

    // ═══════════════════════════════════════════════════════
    // STATS / DASHBOARD
    // ═══════════════════════════════════════════════════════
    public class DashboardDto
    {
        public int TotalCustomers { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<RevenueByMonthDto> RevenueByMonth { get; set; } = new();
        public List<OrderStatusCountDto> OrderStatusBreakdown { get; set; } = new();
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueByMonthDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class RevenueByDayDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class OrderStatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RevenueReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProductsSold { get; set; }
        public List<RevenueByDayDto> ByDay { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════
    // CATEGORY / INVENTORY
    // ═══════════════════════════════════════════════════════
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }

    public class InventoryItemDto
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int StockQuantity { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }

    public class UpdateStockBatchDto
    {
        [Required][MinLength(1)] public List<UpdateStockItemDto> Items { get; set; } = new();
    }

    public class UpdateStockItemDto
    {
        [Range(1, int.MaxValue)] public int VariantId { get; set; }
        [Range(0, int.MaxValue)] public int NewStock { get; set; }
    }

    // ═══════════════════════════════════════════════════════
    // COMMON
    // ═══════════════════════════════════════════════════════
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Thành công")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message)
            => new() { Success = false, Message = message };
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
