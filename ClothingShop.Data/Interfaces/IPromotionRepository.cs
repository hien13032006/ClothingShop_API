using System.Threading.Tasks;
using ClothingShop.Models;

namespace ClothingShop.Data.Interfaces
{
    public interface IPromotionRepository : IRepository<Promotion>
    {
        Task<Promotion?> GetByCodeAsync(string code);
        Task UpdateAsync(Promotion promo);
        Task SaveChangesAsync();
    }
}
