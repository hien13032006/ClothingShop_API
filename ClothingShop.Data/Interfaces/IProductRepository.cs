using System.Threading.Tasks;
using ClothingShop.Models;
using System.Collections.Generic;

namespace ClothingShop.Data.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<ProductVariant?> GetVariantAsync(int variantId);
        Task<Product?> GetWithVariantsAsync(int productId);
        Task UpdateAsync(Product product);
        Task UpdateStockAsync(int variantId, int delta);
        Task SaveChangesAsync();

    }
}
