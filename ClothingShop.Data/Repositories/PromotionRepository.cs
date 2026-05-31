using Microsoft.EntityFrameworkCore;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;

namespace ClothingShop.Data.Repositories
{
    public class PromotionRepository : BaseRepository<Promotion>, IPromotionRepository
    {
        public PromotionRepository(AppDbContext context) : base(context) { }

        public async Task<Promotion?> GetByCodeAsync(string code)
            => await _context.Promotions.FirstOrDefaultAsync(p => p.Code == code);
    }
}
