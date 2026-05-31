using ClothingShop.Data;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ClothingShop.Business.Services
{
    public interface IReviewService
    {
        Task<ApiResponse<ReviewDto>> AddReviewAsync(string userId, int productId, CreateReviewDto dto);
        Task<ApiResponse<ProductRatingSummaryDto>> GetReviewsAsync(int productId, int page, int pageSize);
        Task<ApiResponse<string>> DeleteReviewAsync(string userId, int reviewId, bool isAdmin);
    }

    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepo;
        private readonly AppDbContext _context;

        public ReviewService(IReviewRepository reviewRepo, AppDbContext context)
        {
            _reviewRepo = reviewRepo;
            _context = context;
        }

        public async Task<ApiResponse<ReviewDto>> AddReviewAsync(string userId, int productId, CreateReviewDto dto)
        {
            // 1. Kiểm tra đơn hàng hợp lệ
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant)
                .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId && o.UserId == userId);

            if (order == null)
                return ApiResponse<ReviewDto>.Fail("Không tìm thấy đơn hàng hoặc đơn hàng không thuộc về bạn");

            if (order.Status != "Hoàn thành")
                return ApiResponse<ReviewDto>.Fail("Chỉ được đánh giá sau khi đơn hàng hoàn thành");

            bool hasBought = order.OrderDetails.Any(od => od.Variant?.ProductId == productId);
            if (!hasBought)
                return ApiResponse<ReviewDto>.Fail("Bạn chưa mua sản phẩm này trong đơn hàng đó");

            // 2. Kiểm tra đã review chưa
            var existing = await _reviewRepo.GetUserReviewAsync(productId, userId, dto.OrderId);
            if (existing != null)
                return ApiResponse<ReviewDto>.Fail("Bạn đã đánh giá sản phẩm này trong đơn hàng này rồi");

            // Lấy tên khách hàng từ bảng tài khoản Customers để lưu trực tiếp vào bản ghi Review
            var customerAccount = await _context.Customers.FindAsync(userId);
            string resolvedCustomerName = customerAccount?.FullName ?? "Ẩn danh";

            // 3. Tạo review (ĐÃ XÓA IsVerifiedPurchase vì Model đã gỡ bỏ thuộc tính này)
            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                OrderId = dto.OrderId,
                CustomerName = resolvedCustomerName, // Lưu trực tiếp chuỗi tên vào database
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                CreatedAt = DateTime.UtcNow // Khuyến khích dùng UtcNow cho đồng bộ hệ thống
            };

            await _reviewRepo.AddAsync(review);
            await _reviewRepo.SaveChangesAsync();

            return ApiResponse<ReviewDto>.Ok(new ReviewDto
            {
                ReviewId = review.ReviewId,
                ProductId = review.ProductId,
                UserId = review.UserId,
                OrderId = review.OrderId,
                CustomerName = resolvedCustomerName!,
                Rating = review.Rating,
                Comment = review.Comment,
                IsVerifiedPurchase = true, // Gán cứng true ở DTO trả về vì qua được bước kiểm tra đơn hàng phía trên
                CreatedAt = review.CreatedAt
            }, "Đánh giá thành công");
        }

        public async Task<ApiResponse<ProductRatingSummaryDto>> GetReviewsAsync(int productId, int page, int pageSize)
        {
            var reviews = await _reviewRepo.GetByProductAsync(productId, page, pageSize);
            var total = await _reviewRepo.CountByProductAsync(productId);
            var avg = await _reviewRepo.GetAverageRatingAsync(productId);
            var dist = await _reviewRepo.GetRatingDistributionAsync(productId);

            return ApiResponse<ProductRatingSummaryDto>.Ok(new ProductRatingSummaryDto
            {
                AverageRating = Math.Round(avg, 1),
                TotalReviews = total,
                RatingDistribution = dist,
                Reviews = reviews.Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    UserId = r.UserId,
                    OrderId = r.OrderId,
                    CustomerName = r.CustomerName, // Thay thế r.Customer?.FullName bằng r.CustomerName lưu sẵn ở bảng
                    Rating = r.Rating,
                    Comment = r.Comment,
                    IsVerifiedPurchase = true, // Luôn luôn hiển thị Đã xác thực mua hàng
                    CreatedAt = r.CreatedAt
                }).ToList()
            });
        }

        public async Task<ApiResponse<string>> DeleteReviewAsync(string userId, int reviewId, bool isAdmin)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);
            if (review == null) return ApiResponse<string>.Fail("Không tìm thấy đánh giá");
            if (!isAdmin && review.UserId != userId)
                return ApiResponse<string>.Fail("Không có quyền xóa đánh giá này");

            await _reviewRepo.DeleteAsync(review);
            await _reviewRepo.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đã xóa đánh giá");
        }
    }
}