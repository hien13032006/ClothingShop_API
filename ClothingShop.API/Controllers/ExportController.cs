using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;
        public ExportController(IExportService es) => _exportService = es;

        /// <summary>GET /api/export/orders?status=Hoàn thành&fromDate=2024-01-01&toDate=2024-12-31</summary>
        [HttpGet("orders")]
        public async Task<IActionResult> ExportOrders([FromQuery] OrderFilterDto filter)
        {
            var csv = await _exportService.ExportOrdersToCsvAsync(filter);
            return File(csv, "text/csv; charset=utf-8",
                $"don-hang-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        }

        /// <summary>GET /api/export/revenue?from=2024-01-01&to=2024-12-31</summary>
        [HttpGet("revenue")]
        public async Task<IActionResult> ExportRevenue(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var fromDate = from ?? DateTime.Now.AddMonths(-1);
            var toDate   = to   ?? DateTime.Now;
            var csv = await _exportService.ExportRevenueToCsvAsync(fromDate, toDate);
            return File(csv, "text/csv; charset=utf-8",
                $"doanh-thu-{fromDate:yyyyMMdd}-den-{toDate:yyyyMMdd}.csv");
        }

        /// <summary>GET /api/export/customers</summary>
        [HttpGet("customers")]
        public async Task<IActionResult> ExportCustomers()
        {
            var csv = await _exportService.ExportCustomersToCsvAsync();
            return File(csv, "text/csv; charset=utf-8",
                $"khach-hang-{DateTime.Now:yyyyMMdd}.csv");
        }

        /// <summary>GET /api/export/inventory</summary>
        [HttpGet("inventory")]
        public async Task<IActionResult> ExportInventory()
        {
            var csv = await _exportService.ExportInventoryToCsvAsync();
            return File(csv, "text/csv; charset=utf-8",
                $"ton-kho-{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
