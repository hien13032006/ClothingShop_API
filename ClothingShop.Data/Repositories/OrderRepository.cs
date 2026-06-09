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
            // Lấy mã đơn hàng lớn nhất hiện có trong bảng
            var lastOrder = await _context.Orders
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefaultAsync();

            if (lastOrder == null)
            {
                return "ORD000001"; // Nếu chưa có đơn nào, bắt đầu từ 000001
            }

            // Tách phần số từ mã (VD: ORD000005 -> 5)
            string lastId = lastOrder.OrderId; // ORD000005
            string numericPart = lastId.Replace("ORD", "");
            int nextNumber = int.Parse(numericPart) + 1;

            // Trả về mã mới (VD: ORD000006)
            return $"ORD{nextNumber:D6}";
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
            return await _context.Orders
                .Include(o => o.OrderDetails)           // Nạp chi tiết đơn hàng
                    .ThenInclude(od => od.Variant)      // Nạp biến thể (Màu, Size)
                        .ThenInclude(v => v.Product)    // Nạp thông tin sản phẩm
                .Include(o => o.Trackings)              // Nạp thông tin vận chuyển
                .Include(o => o.Payment)                // Nạp thông tin thanh toán
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)    // Nên có thêm sắp xếp để đơn mới nhất hiện lên trên
                .ToListAsync();
        }
    }
}
