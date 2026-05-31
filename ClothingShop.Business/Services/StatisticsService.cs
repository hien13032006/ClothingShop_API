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

            var now = DateTime.Now;
            return ApiResponse<DashboardDto>.Ok(new DashboardDto
            {
                TotalCustomers  = await _context.Customers.CountAsync(c => !c.UserId.StartsWith("AD")),
                TotalOrders     = await _context.Orders.CountAsync(),
                PendingOrders   = await _context.Orders.CountAsync(o => o.Status == "Chờ xác nhận"),
                TotalRevenue    = await _context.Orders
                    .Where(o => o.Status == "Hoàn thành")
                    .SumAsync(o => (decimal?)o.FinalPrice) ?? 0,
                RevenueThisMonth = await _context.Orders
                    .Where(o => o.Status == "Hoàn thành" &&
                                o.OrderDate.Month == now.Month &&
                                o.OrderDate.Year  == now.Year)
                    .SumAsync(o => (decimal?)o.FinalPrice) ?? 0,
                TopProducts          = topProducts,
                RevenueByMonth       = revenueByMonth,
                OrderStatusBreakdown = statusBreakdown
            });
        }

        public async Task<ApiResponse<RevenueReportDto>> GetRevenueReportAsync(DateTime from, DateTime to)
        {
            if (from > to)
                return ApiResponse<RevenueReportDto>.Fail("Ngày bắt đầu phải nhỏ hơn ngày kết thúc");

            var orders = await _context.Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate >= from && o.OrderDate <= to)
                .ToListAsync();

            var byDay = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new RevenueByDayDto
                {
                    Date       = g.Key,
                    Revenue    = g.Sum(o => o.FinalPrice),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            var totalSold = await _context.OrderDetails
                .Where(od => od.Order!.Status == "Hoàn thành" &&
                             od.Order.OrderDate >= from &&
                             od.Order.OrderDate <= to)
                .SumAsync(od => od.Quantity);

            return ApiResponse<RevenueReportDto>.Ok(new RevenueReportDto
            {
                FromDate          = from,
                ToDate            = to,
                TotalRevenue      = orders.Sum(o => o.FinalPrice),
                TotalOrders       = orders.Count,
                TotalProductsSold = totalSold,
                ByDay             = byDay
            });
        }
    }
}
