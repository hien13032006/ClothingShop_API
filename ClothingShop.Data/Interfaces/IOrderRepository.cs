using System.Threading.Tasks;
using ClothingShop.Models;
using System.Collections.Generic;

namespace ClothingShop.Data.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<string> GenerateNextOrderIdAsync();
        Task AddTrackingAsync(OrderTracking tracking);
        Task<Order?> GetWithDetailsAsync(string orderId);
        Task<List<Order>> GetByCustomerAsync(string userId);
        Task UpdateAsync(Order order);
        Task SaveChangesAsync();
    }
}
