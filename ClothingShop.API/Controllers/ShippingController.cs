// ═══════════════════════════════════════════════════════════════
// ShippingController
// ═══════════════════════════════════════════════════════════════
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingService _shippingService;
        public ShippingController(IShippingService ss) => _shippingService = ss;

        /// <summary>GET /api/shipping/options?orderAmount=500000 — Xem phí vận chuyển + miễn phí nếu đủ điều kiện</summary>
        [HttpGet("options")]
        public async Task<IActionResult> GetOptions([FromQuery] decimal orderAmount = 0)
            => Ok(await _shippingService.GetShippingOptionsAsync(orderAmount));

        /// <summary>GET /api/shipping/fee?method=Tiêu chuẩn&orderAmount=300000</summary>
        [HttpGet("fee")]
        public async Task<IActionResult> GetFee([FromQuery] string method, [FromQuery] decimal orderAmount = 0)
        {
            var r = await _shippingService.GetFeeAsync(method, orderAmount);
            return r.Success ? Ok(r) : NotFound(r);
        }

        /// <summary>GET /api/shipping/admin/all — [Admin] Xem tất cả cấu hình phí</summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllFees()
            => Ok(await _shippingService.GetAllFeesAsync());

        /// <summary>PUT /api/shipping/admin — [Admin] Tạo hoặc cập nhật phí vận chuyển</summary>
        [HttpPut("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upsert([FromBody] UpsertShippingFeeDto dto)
        {
            var r = await _shippingService.UpsertShippingFeeAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
