using Microsoft.EntityFrameworkCore;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;

namespace ClothingShop.Data.Repositories
{
    public class AddressRepository : BaseRepository<Address>, IAddressRepository
    {
        public AddressRepository(AppDbContext context) : base(context) { }

        // BaseRepository provides UpdateAsync/DeleteAsync/SaveChangesAsync implementations

        public async Task<IEnumerable<Address>> GetByUserAsync(string userId)
            => await _dbSet
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

        public async Task<Address?> GetDefaultAsync(string userId)
            => await _dbSet.FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

        public async Task ClearDefaultAsync(string userId)
        {
            var defaults = await _dbSet
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();
            foreach (var a in defaults)
                a.IsDefault = false;
        }
    }
}
