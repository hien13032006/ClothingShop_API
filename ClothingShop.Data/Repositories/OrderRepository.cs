using Microsoft.EntityFrameworkCore;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using System.Collections.Generic;

namespace ClothingShop.Data.Repositories
{
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context) { }

        public async Task<string> GenerateNextOrderIdAsync()
        {
            // Simple order id: ORD000001
            var count = await _context.Orders.CountAsync();
            return $"ORD{(count + 1):D6}";
        }

        public async Task AddTrackingAsync(OrderTracking tracking)
        {
            await _context.OrderTracking.AddAsync(tracking);
        }

        public async Task<Order?> GetWithDetailsAsync(string orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant).ThenInclude(v => v.Product)
                .Include(o => o.Trackings)
                .Include(o => o.Payment)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<Order>> GetByCustomerAsync(string userId)
        {
            return await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
        }
    }
}
