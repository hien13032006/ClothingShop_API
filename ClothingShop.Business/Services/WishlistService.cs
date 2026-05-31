using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IWishlistService
    {
        Task<ApiResponse<List<ProductSummaryDto>>> GetWishlistAsync(string userId);
        Task<ApiResponse<string>> ToggleWishlistAsync(string userId, int productId);
        Task<ApiResponse<bool>> IsInWishlistAsync(string userId, int productId);
    }

    public class WishlistService : IWishlistService
    {
        private readonly AppDbContext _context;

        public WishlistService(AppDbContext context) => _context = context;

        public async Task<ApiResponse<List<ProductSummaryDto>>> GetWishlistAsync(string userId)
        {
            // ĐÃ SỬA: Loại bỏ .ThenInclude(p => p!.Category) vì Category bây giờ là thuộc tính kiểu string trực tiếp
            var items = await _context.Wishlist
                .Where(w => w.UserId == userId)
                .Include(w => w.Product).ThenInclude(p => p!.Reviews)
                .Include(w => w.Product).ThenInclude(p => p!.Variants)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            var dtos = items
                .Where(w => w.Product != null) // ĐÃ SỬA: Loại bỏ check IsActive
                .Select(w => {
                    var p = w.Product!;
                    var avg = p.Reviews.Any() ? p.Reviews.Average(r => (double)r.Rating) : 0;
                    return new ProductSummaryDto
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        BasePrice = p.Price,       // ĐÃ SỬA: Đổi BasePrice -> Price
                        ImageUrl = p.MainImage,   // ĐÃ SỬA: Đổi ImageUrl -> MainImage
                        Category = p.Category,    // ĐÃ SỬA: Lấy trực tiếp chuỗi Category thay vì Category.Name
                        IsActive = true,          // Đồng bộ với cấu trúc DTO (luôn luôn active)
                        SoldCount = p.SoldCount,
                        AverageRating = Math.Round(avg, 1),
                        ReviewCount = p.Reviews.Count
                    };
                })
                .ToList();

            return ApiResponse<List<ProductSummaryDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<string>> ToggleWishlistAsync(string userId, int productId)
        {
            var existing = await _context.Wishlist
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existing != null)
            {
                _context.Wishlist.Remove(existing);
                await _context.SaveChangesAsync();
                return ApiResponse<string>.Ok("REMOVED", "Đã xóa khỏi danh sách yêu thích");
            }

            var product = await _context.Products.FindAsync(productId);
            // ĐÃ SỬA: Loại bỏ điều kiện check !product.IsActive
            if (product == null)
                return ApiResponse<string>.Fail("Sản phẩm không tồn tại");

            await _context.Wishlist.AddAsync(new WishlistItem
            {
                UserId = userId,
                ProductId = productId,
                AddedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("ADDED", "Đã thêm vào danh sách yêu thích");
        }

        public async Task<ApiResponse<bool>> IsInWishlistAsync(string userId, int productId)
        {
            var exists = await _context.Wishlist
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
            return ApiResponse<bool>.Ok(exists);
        }
    }
}