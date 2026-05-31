using ClothingShop.Models;

namespace ClothingShop.Data.Interfaces
{
    public interface IReviewRepository : IRepository<ProductReview>
    {
        Task<IEnumerable<ProductReview>> GetByProductAsync(int productId, int page, int pageSize);
        Task<int> CountByProductAsync(int productId);
        Task<double> GetAverageRatingAsync(int productId);
        Task<Dictionary<int, int>> GetRatingDistributionAsync(int productId);
        Task<ProductReview?> GetUserReviewAsync(int productId, string userId, string orderId);
        Task<bool> HasPurchasedAsync(string userId, int productId);
        Task DeleteAsync(ProductReview review);
        Task UpdateAsync(ProductReview review);
        Task SaveChangesAsync();
    }
}
