using Microsoft.EntityFrameworkCore;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;

namespace ClothingShop.Data.Repositories
{
    public class ReviewRepository : BaseRepository<ProductReview>, IReviewRepository
    {
        public ReviewRepository(AppDbContext context) : base(context) { }

        // inherit UpdateAsync/DeleteAsync/SaveChangesAsync from BaseRepository

        public async Task<IEnumerable<ProductReview>> GetByProductAsync(int productId, int page, int pageSize)
            => await _dbSet
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task<int> CountByProductAsync(int productId)
            => await _dbSet.CountAsync(r => r.ProductId == productId);

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            if (!await _dbSet.AnyAsync(r => r.ProductId == productId)) return 0;
            return await _dbSet
                .Where(r => r.ProductId == productId)
                .AverageAsync(r => (double)r.Rating);
        }

        public async Task<Dictionary<int, int>> GetRatingDistributionAsync(int productId)
        {
            var dist = await _dbSet
                .Where(r => r.ProductId == productId)
                .GroupBy(r => r.Rating)
                .Select(g => new { Star = g.Key, Count = g.Count() })
                .ToListAsync();

            // Đảm bảo trả về đủ 5 mức dù không có đánh giá
            var result = new Dictionary<int, int> { {5,0},{4,0},{3,0},{2,0},{1,0} };
            foreach (var d in dist) result[d.Star] = d.Count;
            return result;
        }

        public async Task<ProductReview?> GetUserReviewAsync(int productId, string userId, string orderId)
            => await _dbSet.FirstOrDefaultAsync(r =>
                r.ProductId == productId && r.UserId == userId && r.OrderId == orderId);

        /// <summary>Kiểm tra khách đã thực sự mua sản phẩm này chưa (đơn Hoàn thành)</summary>
        public async Task<bool> HasPurchasedAsync(string userId, int productId)
            => await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Variant)
                .AnyAsync(od =>
                    od.Order!.UserId == userId &&
                    od.Order.Status == "Hoàn thành" &&
                    od.Variant!.ProductId == productId);
    }
}
