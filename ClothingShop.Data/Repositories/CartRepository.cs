using Microsoft.EntityFrameworkCore;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using System.Collections.Generic;

namespace ClothingShop.Data.Repositories
{
    public class CartRepository : BaseRepository<CartItem>, ICartRepository
    {
        public CartRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<CartItem>> GetCartByUserAsync(string userId)
            => await _context.Cart.Where(c => c.UserId == userId).Include(c => c.Variant).ThenInclude(v => v.Product).ToListAsync();

        public async Task<CartItem?> GetCartItemAsync(string userId, int variantId)
            => await _context.Cart.FirstOrDefaultAsync(c => c.UserId == userId && c.VariantId == variantId);

        public async Task ClearCartAsync(string userId)
        {
            var cartItems = _context.Cart.Where(c => c.UserId == userId);
            _context.Cart.RemoveRange(cartItems);
            await _context.SaveChangesAsync(); // Cần dòng này!
        }
    }
}
