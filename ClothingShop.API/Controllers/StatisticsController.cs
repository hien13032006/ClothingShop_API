using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _stats;
        public StatisticsController(IStatisticsService stats) => _stats = stats;

        /// <summary>
        /// GET /api/statistics/dashboard
        /// Trả về: tổng đơn, tổng doanh thu, top sản phẩm, biểu đồ 6 tháng, phân tích trạng thái
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
            => Ok(await _stats.GetDashboardAsync());

        /// <summary>
        /// GET /api/statistics/revenue?from=2024-01-01&to=2024-12-31
        /// Báo cáo doanh thu chi tiết theo ngày trong khoảng thời gian chỉ định
        /// </summary>
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var fromDate = from ?? DateTime.Now.AddMonths(-1);
            var toDate   = to   ?? DateTime.Now;
            var r = await _stats.GetRevenueReportAsync(fromDate, toDate);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
