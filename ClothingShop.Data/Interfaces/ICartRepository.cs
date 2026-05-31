using System.Threading.Tasks;
using ClothingShop.Models;
using System.Collections.Generic;

namespace ClothingShop.Data.Interfaces
{
    public interface ICartRepository : IRepository<CartItem>
    {
        Task<IEnumerable<CartItem>> GetCartByUserAsync(string userId);
        Task<CartItem?> GetCartItemAsync(string userId, int variantId);
        Task ClearCartAsync(string userId);
        Task UpdateAsync(CartItem item);
        Task DeleteAsync(CartItem item);
        Task SaveChangesAsync();
    }
}
