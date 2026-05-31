using ClothingShop.Models;

namespace ClothingShop.Data.Interfaces
{
    public interface IAddressRepository : IRepository<Address>
    {
        Task<IEnumerable<Address>> GetByUserAsync(string userId);
        Task<Address?> GetDefaultAsync(string userId);
        Task ClearDefaultAsync(string userId);
        Task UpdateAsync(Address address);
        Task DeleteAsync(Address address);
        Task SaveChangesAsync();
    }
}
