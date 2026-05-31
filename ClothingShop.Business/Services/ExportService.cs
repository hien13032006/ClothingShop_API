using System.Text;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportOrdersToCsvAsync(OrderFilterDto filter);
        Task<byte[]> ExportRevenueToCsvAsync(DateTime from, DateTime to);
        Task<byte[]> ExportCustomersToCsvAsync();
        Task<byte[]> ExportInventoryToCsvAsync();
    }

    public class ExportService : IExportService
    {
        private readonly AppDbContext _context;

        public ExportService(AppDbContext context) => _context = context;

        public async Task<byte[]> ExportOrdersToCsvAsync(OrderFilterDto filter)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(o => o.Status == filter.Status);
            if (filter.FromDate.HasValue)
                query = query.Where(o => o.OrderDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(o => o.OrderDate <= filter.ToDate.Value);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Mã đơn hàng,Khách hàng,Ngày đặt,Tổng tiền,Giảm giá,Thực thanh toán,Phương thức vận chuyển,Phương thức thanh toán,Trạng thái,Trạng thái thanh toán");

            foreach (var o in orders)
            {
                sb.AppendLine(string.Join(",",
                    o.OrderId,
                    $"\"{o.Customer?.FullName ?? ""}\"",
                    o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                    o.TotalPrice,
                    o.DiscountAmount,
                    o.FinalPrice,
                    $"\"{o.ShippingMethod}\"",
                    $"\"{o.PaymentMethod}\"",
                    $"\"{o.Status}\"",
                    $"\"{o.Payment?.PaymentStatus ?? ""}\""
                ));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public async Task<byte[]> ExportRevenueToCsvAsync(DateTime from, DateTime to)
        {
            var orders = await _context.Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate >= from && o.OrderDate <= to)
                .ToListAsync();

            var byDay = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.FinalPrice), Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Ngày,Số đơn hoàn thành,Doanh thu (đồng)");
            foreach (var d in byDay)
                sb.AppendLine($"{d.Date:dd/MM/yyyy},{d.Count},{d.Revenue}");

            sb.AppendLine($"TỔNG,,{orders.Sum(o => o.FinalPrice)}");
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public async Task<byte[]> ExportCustomersToCsvAsync()
        {
            var customers = await _context.Customers
                .Where(c => !c.UserId.StartsWith("AD"))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Mã KH,Họ tên,Email,Số điện thoại,Hạng thành viên,Điểm,Ngày đăng ký");
            foreach (var c in customers)
            {
                sb.AppendLine(string.Join(",",
                    c.UserId,
                    $"\"{c.FullName ?? ""}\"",
                    c.Email,
                    c.Phone ?? "",
                    c.MembershipLevel,
                    c.TotalPoints,
                    c.CreatedAt.ToString("dd/MM/yyyy")
                ));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public async Task<byte[]> ExportInventoryToCsvAsync()
        {
            // ĐÃ SỬA: Loại bỏ điều kiện .Where lọc theo trường IsActive cũ
                var variants = await _context.ProductVariants
                    .Include(v => v.Product)
                    .OrderBy(v => v.StockQuantity)
                    .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Mã biến thể,Tên sản phẩm,Danh mục,Màu sắc,Kích thước,Tồn kho,Giá,Trạng thái");
            foreach (var v in variants)
            {
                var status = v.StockQuantity == 0 ? "Hết hàng"
                           : v.StockQuantity < 5 ? "Sắp hết"
                           : "Còn hàng";

                // ĐÃ SỬA: Lấy giá sản phẩm trực tiếp từ v.Product.Price thay vì v.ActualPrice cũ
                decimal productPrice = v.Product?.Price ?? 0;

                sb.AppendLine(string.Join(",",
                    v.VariantId,
                    $"\"{v.Product?.Name ?? ""}\"",
                    $"\"{v.Product?.Category ?? ""}\"",
                    v.Color ?? "",
                    v.Size ?? "",
                    v.StockQuantity,
                    productPrice,
                    status
                ));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }
    }
}