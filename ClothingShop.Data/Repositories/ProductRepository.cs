using Microsoft.EntityFrameworkCore;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using System.Threading.Tasks;

namespace ClothingShop.Data.Repositories
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context) { }

        public async Task<ProductVariant?> GetVariantAsync(int variantId)
            => await _context.ProductVariants
                             .Include(v => v.Product)
                             .FirstOrDefaultAsync(v => v.VariantId == variantId);

        public async Task<Product?> GetWithVariantsAsync(int productId)
            => await _context.Products
                             .Include(p => p.Variants)
                             .FirstOrDefaultAsync(p => p.ProductId == productId);

        public async Task UpdateStockAsync(int variantId, int delta)
        {
            var v = await _context.ProductVariants.FindAsync(variantId);
            if (v == null) return;
            if (delta < 0 && v.StockQuantity + delta < 0)
            {
                throw new InvalidOperationException($"Số lượng tồn kho không đủ để thực hiện giao dịch (Hiện có: {v.StockQuantity}).");
            }

            v.StockQuantity += delta;

            await SaveChangesAsync();
        }
     
    }
}