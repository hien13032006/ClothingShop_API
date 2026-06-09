using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IStatisticsService
    {
        Task<ApiResponse<DashboardDto>> GetDashboardAsync();
        Task<ApiResponse<RevenueReportDto>> GetRevenueReportAsync(DateTime from, DateTime to);
    }

    public class StatisticsService : IStatisticsService
    {
        private readonly AppDbContext _context;
        public StatisticsService(AppDbContext context) => _context = context;

        public async Task<ApiResponse<DashboardDto>> GetDashboardAsync()
        {
            var topProducts = await _context.OrderDetails
                .Include(od => od.Variant).ThenInclude(v => v!.Product)
                .Where(od => od.Order!.Status == "Hoàn thành")
                .GroupBy(od => od.Variant!.Product!.Name)
                .Select(g => new TopProductDto
                {
                    ProductName  = g.Key,
                    TotalSold    = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            var sixMonthsAgo = DateTime.Now.AddMonths(-5);
            var revenueByMonth = await _context.Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new RevenueByMonthDto
                {
                    Year = g.Key.Year, Month = g.Key.Month,
                    Revenue = g.Sum(o => o.FinalPrice), OrderCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var statusBreakdown = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusCountDto { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var orderStats = await _context.Orders
                .GroupBy(o => 1)
                .Select(g => new {
                    TotalOrders = g.Count(),
                    PendingOrders = g.Count(x => x.Status == "Chờ xác nhận"),
                    TotalRevenue = g.Where(x => x.Status == "Hoàn thành").Sum(x => (decimal?)x.FinalPrice) ?? 0
                })
                .FirstOrDefaultAsync() ?? new { TotalOrders = 0, PendingOrders = 0, TotalRevenue = 0m };
            var totalCustomers = await _context.Customers.CountAsync(c => !c.UserId.StartsWith("AD"));
            var now = DateTime.Now;
            return ApiResponse<DashboardDto>.Ok(new DashboardDto
            {
                TotalCustomers = totalCustomers,
                TotalOrders = orderStats.TotalOrders,
                PendingOrders = orderStats.PendingOrders,
                TotalRevenue = orderStats.TotalRevenue,
                RevenueThisMonth = await _context.Orders
            .Where(o => o.Status == "Hoàn thành" && o.OrderDate.Month == now.Month && o.OrderDate.Year == now.Year)
            .SumAsync(o => (decimal?)o.FinalPrice) ?? 0,
                TopProducts = topProducts,
                RevenueByMonth = revenueByMonth,
                OrderStatusBreakdown = statusBreakdown
            });
        }

        public async Task<ApiResponse<RevenueReportDto>> GetRevenueReportAsync(DateTime from, DateTime to)
        {
            if (from > to)
                return ApiResponse<RevenueReportDto>.Fail("Ngày bắt đầu phải nhỏ hơn ngày kết thúc");

            // 1. Lấy dữ liệu đã GroupBy từ Database thay vì lấy hết về RAM
            var byDay = await _context.Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate >= from && o.OrderDate <= to)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new RevenueByDayDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.FinalPrice),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // 2. Tính toán tổng hợp từ kết quả của byDay để tránh truy vấn lại bảng Orders nhiều lần
            var totalRevenue = byDay.Sum(x => x.Revenue);
            var totalOrders = byDay.Sum(x => x.OrderCount);

            // 3. Truy vấn chỉ số lượng sản phẩm từ OrderDetails
            var totalSold = await _context.OrderDetails
                .Where(od => od.Order!.Status == "Hoàn thành" &&
                             od.Order.OrderDate >= from &&
                             od.Order.OrderDate <= to)
                .SumAsync(od => (int?)od.Quantity) ?? 0;

            return ApiResponse<RevenueReportDto>.Ok(new RevenueReportDto
            {
                FromDate = from,
                ToDate = to,
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalProductsSold = totalSold,
                ByDay = byDay
            });
        }
    }
}
